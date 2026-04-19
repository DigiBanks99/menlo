using System.Diagnostics;
using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace Menlo.Api.SpaHosting;

internal static class SpaHostingExtensions
{
    private const string SpaServiceName = "web-ui";

    private static readonly ForwarderRequestConfig SpaForwarderRequestConfig = new()
    {
        ActivityTimeout = TimeSpan.FromSeconds(100)
    };

    public static IHostApplicationBuilder AddSpaReverseProxy(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpForwarder();
        return builder;
    }

    public static WebApplication UseMenloSpaStaticFiles(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }

        return app;
    }

    public static IEndpointRouteBuilder MapMenloSpa(this IEndpointRouteBuilder app)
    {
        app.Map("/.well-known/{**path}", static context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });

        if (app.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            IConfiguration configuration = app.ServiceProvider.GetRequiredService<IConfiguration>();
            app.MapForwarder(
                "/{**catch-all}",
                ResolveDevelopmentServerUri(configuration),
                SpaForwarderRequestConfig,
                HttpTransformer.Default,
                CreateProxyHttpClient());

            return app;
        }

        app.MapFallbackToFile("index.html");
        return app;
    }

    private static string ResolveDevelopmentServerUri(IConfiguration configuration)
    {
        string? uri = configuration.GetConnectionString(SpaServiceName)
            ?? configuration[$"Services:{SpaServiceName}:https:0"]
            ?? configuration[$"Services:{SpaServiceName}:http:0"];

        return Uri.TryCreate(uri, UriKind.Absolute, out Uri? developmentServerUri)
            ? EnsureTrailingSlash(developmentServerUri.AbsoluteUri)
            : throw new InvalidOperationException(
                "The web-ui development server endpoint was not found in Aspire service discovery configuration.");
    }

    private static string EnsureTrailingSlash(string destinationPrefix) =>
        destinationPrefix.EndsWith("/", StringComparison.Ordinal)
            ? destinationPrefix
            : $"{destinationPrefix}/";

    private static HttpMessageInvoker CreateProxyHttpClient() =>
        new(new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,
            EnableMultipleHttp2Connections = true,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15)
        });
}
