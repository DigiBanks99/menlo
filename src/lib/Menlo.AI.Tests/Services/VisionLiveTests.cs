using Menlo.AI.Extensions;
using Menlo.AI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Menlo.AI.Tests.Services;

/// <summary>
/// Calls a real llama-server, which needs the vision model's image projector on (<c>mmproj-auto</c>). Opt-in: set
/// <c>MENLO_AI_LIVE_ENDPOINT</c> to its base URL including <c>/v1</c>, for example the Aspire <c>llama</c> resource's
/// URL. Skipped otherwise, so CI never needs a running model. On CPU this can take minutes.
/// </summary>
[SkipUnlessLiveModel]
public sealed class VisionLiveTests
{
    [Test]
    [Timeout(11 * 60 * 1000)]
    public async Task AHandwrittenShoppingNoteIsRead(CancellationToken cancellationToken)
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Menlo:AI:Endpoint"] = SkipUnlessLiveModelAttribute.Endpoint
        });
        builder.AddMenloAi();
        using IHost host = builder.Build();
        using IServiceScope scope = host.Services.CreateScope();
        byte[] note = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Samples", "shopping-note.png"), cancellationToken);

        string text = await scope.ServiceProvider.GetRequiredService<IVisionService>().ExtractTextAsync(note, "image/png", cancellationToken);

        ItShouldReadTheNotesKeyWords();

        // Shouldly's string ShouldContain ignores case.
        void ItShouldReadTheNotesKeyWords() => text.ShouldSatisfyAllConditions(
            () => text.ShouldContain("milk"),
            () => text.ShouldContain("bread"),
            () => text.ShouldContain("eggs"));
    }
}

internal sealed class SkipUnlessLiveModelAttribute() : SkipAttribute("Set MENLO_AI_LIVE_ENDPOINT to run against a real llama-server")
{
    public static string? Endpoint => Environment.GetEnvironmentVariable("MENLO_AI_LIVE_ENDPOINT");

    public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
        Task.FromResult(string.IsNullOrWhiteSpace(Endpoint));
}
