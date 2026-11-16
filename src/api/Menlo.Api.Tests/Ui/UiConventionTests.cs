using System.Text.RegularExpressions;

namespace Menlo.Api.Tests.Ui;

/// <summary>
/// Checks the UI's source files follow the separation rules in the menlo-ui-pages and
/// menlo-ui-components skills: presentation files hold no services, no inline styles or scripts,
/// and design-system components know nothing about the rest of Menlo.
/// </summary>
public sealed partial class UiConventionTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static IEnumerable<string> RazorFiles(string relativeFolder) =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot, relativeFolder), "*.razor", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    [Test]
    public void GivenThePagesAndTheDesignSystem_WhenTheirMarkupIsRead_ThenNoneBreaksTheContentSecurityPolicy()
    {
        List<string> violations =
        [
            .. Violations(RazorFiles("src/api/Menlo.Api/Ui"), InlineStyle(), "an inline style attribute"),
            .. Violations(RazorFiles("src/ui/Menlo.Ui"), InlineStyle(), "an inline style attribute"),
            .. Violations(RazorFiles("src/api/Menlo.Api/Ui"), ScriptElement(), "a <script> element"),
            .. Violations(RazorFiles("src/api/Menlo.Api/Ui"), InlineHandler(), "an inline event handler"),
            .. Violations(RazorFiles("src/ui/Menlo.Ui"), InlineHandler(), "an inline event handler")
        ];

        violations.ShouldBeEmpty();
    }

    [Test]
    public void GivenThePages_WhenTheirMarkupIsRead_ThenNoneInjectsAService()
    {
        // Pages render a view model; loading data and applying rules belongs in the endpoint
        List<string> violations =
        [
            .. Violations(RazorFiles("src/api/Menlo.Api/Ui"), Inject(), "@inject"),
            .. Violations(RazorFiles("src/ui/Menlo.Ui"), Inject(), "@inject")
        ];

        violations.ShouldBeEmpty();
    }

    [Test]
    public void GivenTheDesignSystem_WhenItsSourceIsRead_ThenItKnowsNothingAboutTheRestOfMenlo()
    {
        IEnumerable<string> sources = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "src/ui/Menlo.Ui"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".razor", StringComparison.Ordinal) || path.EndsWith(".cs", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        List<string> violations = [.. Violations(sources, MenloReference(), "a reference to Menlo.Api, Menlo.Lib or Menlo.Application")];
        string project = File.ReadAllText(Path.Combine(RepoRoot, "src/ui/Menlo.Ui/Menlo.Ui.csproj"));

        violations.ShouldBeEmpty();
        project.ShouldNotContain("ProjectReference");
    }

    [Test]
    public void GivenTheDesignSystemStyles_WhenRead_ThenColoursComeFromTokens()
    {
        // Hex colours belong in tokens.css. The sign-in button uses Microsoft's brand colours on purpose.
        IEnumerable<string> styles = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "src/ui/Menlo.Ui"), "*.css", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(RepoRoot, "src/api/Menlo.Api/Ui"), "*.css", SearchOption.AllDirectories))
            .Where(path => !path.EndsWith("tokens.css", StringComparison.Ordinal)
                && !path.EndsWith("SignInPage.razor.css", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        Violations(styles, HexColour(), "a hex colour").ShouldBeEmpty();
    }

    private static IEnumerable<string> Violations(IEnumerable<string> files, Regex pattern, string what) =>
        files.SelectMany(file => WithoutComments(File.ReadAllText(file))
            .Split('\n')
            .Select((line, index) => (line, number: index + 1))
            .Where(entry => pattern.IsMatch(entry.line))
            .Select(entry => $"{Path.GetRelativePath(RepoRoot, file)}:{entry.number} has {what}"));

    // Blanks out Razor, C#, HTML and CSS comments, keeping the line breaks so line numbers stay right
    private static string WithoutComments(string text) =>
        Comment().Replace(text, match => new string([.. match.Value.Where(c => c == '\n')]));

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Menlo.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Menlo.slnx was not found above the test output.");
    }

    [GeneratedRegex(@"\sstyle\s*=\s*""", RegexOptions.IgnoreCase)]
    private static partial Regex InlineStyle();

    [GeneratedRegex(@"<script\b", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptElement();

    // HTML event attributes are lower case; Razor parameters such as OnBackground are not
    [GeneratedRegex(@"\s(on[a-z]+|hx-on[:\-][\w:\-]*)\s*=\s*""")]
    private static partial Regex InlineHandler();

    [GeneratedRegex(@"^\s*@inject\b")]
    private static partial Regex Inject();

    [GeneratedRegex(@"\bMenlo\.(Api|Lib|Application|AI)\b")]
    private static partial Regex MenloReference();

    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b")]
    private static partial Regex HexColour();

    // Razor, HTML and CSS comments, and C# line comments (not the // in a URL)
    [GeneratedRegex(@"@\*.*?\*@|<!--.*?-->|/\*.*?\*/|(?<![:""'])//[^\n]*", RegexOptions.Singleline)]
    private static partial Regex Comment();
}
