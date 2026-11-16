using Menlo.Api.Tests.Budget;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using TUnit.Core.Interfaces;

namespace Menlo.Api.Tests.E2E;

/// <summary>
/// One browser for the whole test session. Each test gets its own <see cref="AppSession"/>: the app
/// on a real port with an empty budget database, and a fresh browser context pointed at it.
/// </summary>
/// <remarks>
/// The tests drive the installed Chrome, so there is no browser to download. Set
/// <c>MENLO_E2E_BROWSER_CHANNEL</c> to another channel (such as <c>msedge</c>), or to <c>bundled</c>
/// to use the Chromium that <c>playwright install chromium</c> downloads.
/// </remarks>
public sealed class BrowserFixture : IAsyncInitializer, IAsyncDisposable
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    public async Task InitializeAsync()
    {
        string channel = Environment.GetEnvironmentVariable("MENLO_E2E_BROWSER_CHANNEL") is { Length: > 0 } value ? value : "chrome";

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = channel == "bundled" ? null : channel,
            Headless = Environment.GetEnvironmentVariable("MENLO_E2E_HEADED") != "1"
        });
    }

    /// <summary>
    /// Starts the app on a free port and opens a page on it, signed in as the test user.
    /// </summary>
    public async Task<AppSession> StartAsync(
        BudgetTestWebApplicationFactory factory,
        ViewportSize? viewport = null,
        ColorScheme colorScheme = ColorScheme.Light)
    {
        factory.UseKestrel(0);
        factory.StartServer();

        string address = factory.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First()
            .Replace("127.0.0.1", "localhost", StringComparison.Ordinal)
            .Replace("[::1]", "localhost", StringComparison.Ordinal);

        IBrowserContext context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = address,
            ViewportSize = viewport ?? new ViewportSize { Width = 1280, Height = 900 },
            ColorScheme = colorScheme,
            // The test host listens on plain HTTP. This header, which the app trusts from loopback, makes it treat
            // requests as HTTPS, so the anti-forgery tokens and Secure cookies behave as they do in production.
            ExtraHTTPHeaders = new Dictionary<string, string> { ["X-Forwarded-Proto"] = "https" }
        });

        context.SetDefaultTimeout(10_000);
        IPage page = await context.NewPageAsync();

        return new AppSession(factory, context, page);
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
}

/// <summary>
/// The running app and a browser page on it. It records everything the browser complained about, so a
/// test can assert that a flow broke no Content Security Policy rule and logged no error.
/// </summary>
public sealed class AppSession : IAsyncDisposable
{
    private readonly BudgetTestWebApplicationFactory _factory;
    private readonly IBrowserContext _context;

    internal AppSession(BudgetTestWebApplicationFactory factory, IBrowserContext context, IPage page)
    {
        _factory = factory;
        _context = context;
        Page = page;

        page.Console += (_, message) =>
        {
            // Chrome logs every 4xx response as an error. A 422 is the app working: a form sent back with its errors.
            if (message.Type == "error" && !message.Text.Contains("status of 422", StringComparison.Ordinal))
            {
                BrowserErrors.Add(message.Text);
            }
        };
        page.PageError += (_, error) => BrowserErrors.Add(error);
    }

    /// <summary>The page.</summary>
    public IPage Page { get; }

    /// <summary>Console errors and uncaught exceptions, including Content Security Policy violations.</summary>
    public List<string> BrowserErrors { get; } = [];

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();
    }
}
