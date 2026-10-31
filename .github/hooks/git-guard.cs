#!/usr/bin/env dotnet
// Agent PreToolUse guard for git commands. Used by Claude Code (.claude/settings.json)
// and Copilot (.github/hooks/git-guard.json); see .github/hooks/README.md.
//
// Reads the hook payload from stdin and inspects tool_input.command. It blocks:
//   - commit, cherry-pick, revert and am while the branch is main (or master);
//   - git checkout / git switch forms that discard work (paths, --, -f, -B, ...).
// A block writes the reason to stderr and exits 2; everything else exits 0 silently.
// Written to stay AOT-safe (no reflection serialization).
#:property PublishAot=true
#:property RestorePackagesWithLockFile=false

using System.Diagnostics;
using System.Text;
using System.Text.Json;

string input;
try
{
    input = Console.In.ReadToEnd();
}
catch (IOException)
{
    return 0;
}

if (!HookPayload.TryParse(input, out string command, out string cwd, out bool powerShell))
{
    return 0;
}

string? reason = GitGuard.Check(command, cwd, powerShell);
if (reason is null)
{
    return 0;
}

Console.Error.WriteLine($"git-guard: {reason}");
return 2;

static class HookPayload
{
    public static bool TryParse(string input, out string command, out string cwd, out bool powerShell)
    {
        command = string.Empty;
        powerShell = false;
        cwd = Directory.GetCurrentDirectory();
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(input);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            // PowerShell uses backtick, not backslash, as its escape character.
            powerShell = root.TryGetProperty("tool_name", out JsonElement toolName)
                && toolName.ValueKind == JsonValueKind.String
                && string.Equals(toolName.GetString(), "PowerShell", StringComparison.OrdinalIgnoreCase);

            if (root.TryGetProperty("cwd", out JsonElement cwdElement) && cwdElement.ValueKind == JsonValueKind.String)
            {
                string? value = cwdElement.GetString();
                if (!string.IsNullOrWhiteSpace(value) && Directory.Exists(value))
                {
                    cwd = value;
                }
            }

            foreach (string name in new[] { "tool_input", "toolInput", "input" })
            {
                if (root.TryGetProperty(name, out JsonElement toolInput)
                    && toolInput.ValueKind == JsonValueKind.Object
                    && toolInput.TryGetProperty("command", out JsonElement commandElement)
                    && commandElement.ValueKind == JsonValueKind.String)
                {
                    command = commandElement.GetString() ?? string.Empty;
                    return command.Length > 0;
                }
            }
        }
        catch (JsonException)
        {
        }

        return false;
    }
}

static class GitGuard
{
    private static readonly HashSet<string> CommitCommands = ["commit", "cherry-pick", "revert", "am"];
    private static readonly HashSet<string> ProtectedBranches = ["main", "master"];

    // Global git options that take a separate value (git -C <path>, git -c <key=value>, ...).
    private static readonly HashSet<string> GlobalOptionsWithValue = ["-C", "-c", "--git-dir", "--work-tree", "--namespace", "--exec-path", "--config-env"];

    // Commands that run the rest of their arguments as another command (env FOO=1 git ...).
    private static readonly HashSet<string> Wrappers = ["command", "exec", "env", "time", "nohup", "nice", "sudo", "xargs"];
    private static readonly HashSet<string> Shells = ["sh", "bash", "zsh", "dash", "pwsh", "powershell", "cmd"];

    public static string? Check(string command, string cwd, bool powerShell = false)
    {
        if (!command.Contains("git", StringComparison.Ordinal))
        {
            return null;
        }

        return Check(command, cwd, powerShell, new BranchState());
    }

    private static string? Check(string command, string cwd, bool powerShell, BranchState state)
    {
        foreach (List<string> segment in CommandLine.Split(command, powerShell))
        {
            List<string> words = Unwrap(segment);

            // sh -c '...', pwsh -Command '...', cmd /c ...: check the inner command as well.
            if (words.Count > 0 && Shells.Contains(ExecutableName(words[0])))
            {
                int flag = words.FindIndex(1, w => w is "-c" or "/c" or "/C" || w.Equals("-Command", StringComparison.OrdinalIgnoreCase));
                if (flag > 0 && flag + 1 < words.Count)
                {
                    bool innerPowerShell = ExecutableName(words[0]) is "pwsh" or "powershell";
                    string? reason = Check(string.Join(' ', words.Skip(flag + 1)), cwd, innerPowerShell, state);
                    if (reason is not null)
                    {
                        return reason;
                    }
                }

                continue;
            }

            if (!TryParseGit(words, cwd, out string workDir, out string sub, out List<string> args))
            {
                continue;
            }

            switch (sub)
            {
                case "checkout":
                {
                    string? reason = CheckCheckout(args, workDir, out string? target);
                    if (reason is not null)
                    {
                        return reason;
                    }

                    state.MoveTo(target, workDir);
                    break;
                }
                case "switch":
                {
                    string? reason = CheckSwitch(args, out string? target);
                    if (reason is not null)
                    {
                        return reason;
                    }

                    state.MoveTo(target, workDir);
                    break;
                }
                default:
                    if (CommitCommands.Contains(sub) && !args.Contains("-h") && !args.Contains("--help"))
                    {
                        string? branch = state.Current(workDir);
                        if (branch is not null && ProtectedBranches.Contains(branch))
                        {
                            return $"'git {sub}' on '{branch}' is blocked. Create a feature branch first (git switch -c <type>/<issue>-<slug>); see AGENTS.md.";
                        }
                    }

                    break;
            }
        }

        return null;
    }

    private static bool TryParseGit(List<string> words, string cwd, out string workDir, out string sub, out List<string> args)
    {
        workDir = cwd;
        sub = string.Empty;
        args = [];

        if (words.Count == 0 || !IsGitExecutable(words[0]))
        {
            return false;
        }

        int i = 1;
        while (i < words.Count && words[i].StartsWith('-'))
        {
            string option = words[i];
            if (GlobalOptionsWithValue.Contains(option) && i + 1 < words.Count)
            {
                if (option == "-C")
                {
                    workDir = Path.GetFullPath(NativePath(words[i + 1]), workDir);
                }

                i += 2;
                continue;
            }

            i++;
        }

        if (i >= words.Count)
        {
            return false;
        }

        sub = words[i];
        args = words.GetRange(i + 1, words.Count - i - 1);
        return true;
    }

    private static bool IsEnvAssignment(string word)
    {
        int eq = word.IndexOf('=');
        return eq > 0 && !word.StartsWith('-') && word[..eq].All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private static bool IsGitExecutable(string word) => ExecutableName(word) == "git";

    private static string ExecutableName(string word)
    {
        string name = Path.GetFileName(word.Replace('\\', '/'));
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }

    // Git Bash paths (/c/x, /tmp/x) mean nothing to .NET on Windows; resolve them through cygpath,
    // falling back to the /c/x drive form when cygpath is not on PATH.
    private static string NativePath(string path)
    {
        if (!OperatingSystem.IsWindows() || !path.StartsWith('/'))
        {
            return path;
        }

        string? resolved = Git.RunTool("cygpath", Directory.GetCurrentDirectory(), "-m", path);
        if (!string.IsNullOrEmpty(resolved))
        {
            return resolved;
        }

        if (path.Length >= 2 && char.IsAsciiLetter(path[1]) && (path.Length == 2 || path[2] == '/'))
        {
            return $"{char.ToUpperInvariant(path[1])}:{(path.Length == 2 ? "/" : path[2..])}";
        }

        return path;
    }

    private static List<string> Unwrap(List<string> words)
    {
        int i = 0;
        while (i < words.Count)
        {
            if (IsEnvAssignment(words[i]) || Wrappers.Contains(ExecutableName(words[i])))
            {
                i++;
            }
            else if (i > 0 && words[i].StartsWith('-') && Wrappers.Contains(ExecutableName(words[i - 1])))
            {
                // A wrapper's flags (env -i, sudo -E); flags that take values are rare enough to ignore.
                i++;
            }
            else
            {
                break;
            }
        }

        return words.GetRange(i, words.Count - i);
    }

    // Follows the branch through earlier segments, so "git switch -c x && git commit" is judged on x.
    private sealed class BranchState
    {
        private bool known;
        private string? current;
        private bool previousKnown;
        private string? previous;

        public string? Current(string workDir)
        {
            if (!known)
            {
                current = Git.Run(workDir, "rev-parse", "--abbrev-ref", "HEAD");
                known = true;
            }

            return current;
        }

        // target: a branch, "-" for the previous branch, "HEAD" when detached, or null when unchanged.
        public void MoveTo(string? target, string workDir)
        {
            if (target is null)
            {
                return;
            }

            if (target == "-")
            {
                target = previousKnown ? previous : Git.Run(workDir, "rev-parse", "--abbrev-ref", "@{-1}");
            }

            previous = Current(workDir);
            previousKnown = true;
            current = target;
            known = true;
        }
    }

    private static string? CheckCheckout(List<string> args, string workDir, out string? target)
    {
        target = null;
        bool create = false;
        bool detach = false;
        var positionals = new List<string>();

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--":
                    return "'git checkout -- <path>' discards changes and is blocked; ask the user.";
                case "-f" or "--force" or "--ours" or "--theirs" or "-p" or "--patch" or "-m" or "--merge" or "--overwrite-ignore":
                    return $"'git checkout {arg}' can discard work and is blocked; ask the user.";
                case "-B":
                    return "'git checkout -B' resets an existing branch and is blocked; use 'git checkout -b' for a new branch.";
                case "-b" or "--orphan":
                    create = true;
                    if (i + 1 < args.Count)
                    {
                        target = args[++i];
                    }

                    break;
                case "--detach":
                    detach = true;
                    break;
                case "-t" or "--track" or "--no-track" or "-q" or "--quiet" or "--guess" or "--no-guess" or "--progress" or "--no-progress" or "--recurse-submodules" or "--no-recurse-submodules":
                    break;
                default:
                    if (arg.StartsWith("--track=", StringComparison.Ordinal) || arg.StartsWith("--recurse-submodules=", StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (arg.StartsWith("--pathspec-from-file", StringComparison.Ordinal))
                    {
                        return "'git checkout --pathspec-from-file' checks out paths and is blocked.";
                    }

                    if (arg.StartsWith('-') && arg != "-")
                    {
                        return $"'git checkout {arg}' is not a recognised branch-navigation option; the guard blocks it. Use 'git switch' or ask the user.";
                    }

                    positionals.Add(arg);
                    break;
            }
        }

        // Branch navigation takes at most one positional: the branch, or the start point after -b.
        if (positionals.Count > 1)
        {
            return "'git checkout <ref> <path>' overwrites files and is blocked. Only branch navigation is allowed.";
        }

        if (positionals.Count == 1)
        {
            string name = positionals[0];
            // With -b the positional is the start point; without it, it must be a commit, not a path.
            if (name != "-" && !Git.IsCommit(workDir, name) && !(Git.IsRemoteBranchName(workDir, name) && !create))
            {
                return $"'git checkout {name}' does not name a branch or commit, so it would check out paths and discard changes; it is blocked.";
            }

            if (!create)
            {
                target = name;
            }
        }

        if (detach)
        {
            target = "HEAD";
        }

        return null;
    }

    private static string? CheckSwitch(List<string> args, out string? target)
    {
        target = null;
        string? positional = null;
        bool detach = false;

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--discard-changes" or "-f" or "--force" or "-m" or "--merge":
                    return $"'git switch {arg}' can discard work and is blocked; ask the user.";
                case "-C" or "--force-create":
                    return $"'git switch {arg}' resets an existing branch and is blocked; use 'git switch -c' for a new branch.";
                case "-c" or "--create" or "--orphan":
                    if (i + 1 < args.Count)
                    {
                        target = args[++i];
                    }

                    break;
                case "-d" or "--detach":
                    detach = true;
                    break;
                default:
                    if (arg.StartsWith('-') && arg != "-")
                    {
                        break;
                    }

                    positional ??= arg;
                    break;
            }
        }

        if (detach)
        {
            target = "HEAD";
        }
        else if (target is null && positional is not null)
        {
            target = positional;
        }

        return null;
    }
}

static class Git
{
    public static bool IsCommit(string workDir, string name) =>
        Run(workDir, "rev-parse", "--verify", "--quiet", "--end-of-options", name + "^{commit}") is not null;

    // "git checkout feature" creates a tracking branch from origin/feature (DWIM), even though
    // "feature" itself does not resolve yet.
    public static bool IsRemoteBranchName(string workDir, string name)
    {
        string? remotes = Run(workDir, "for-each-ref", "--format=%(refname:lstrip=3)", "refs/remotes");
        return remotes is not null && remotes.Split('\n').Any(r => r.Trim() == name);
    }

    public static string? Run(string workDir, params string[] args) => RunTool("git", workDir, args);

    public static string? RunTool(string tool, string workDir, params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo(tool)
            {
                WorkingDirectory = workDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (string arg in args)
            {
                psi.ArgumentList.Add(arg);
            }

            using Process? process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            string output = process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
    }
}

// Splits a shell command line into simple commands (on &&, ||, ;, |, & and newlines)
// and each command into words, honouring single quotes, double quotes and escapes:
// backslash for POSIX shells, backtick for PowerShell (where C:\path is a plain word).
static class CommandLine
{
    public static List<List<string>> Split(string command, bool powerShell = false)
    {
        char escape = powerShell ? '`' : '\\';
        var segments = new List<List<string>>();
        var words = new List<string>();
        var word = new StringBuilder();
        bool inWord = false;

        void EndWord()
        {
            if (inWord)
            {
                words.Add(word.ToString());
                word.Clear();
                inWord = false;
            }
        }

        void EndSegment()
        {
            EndWord();
            if (words.Count > 0)
            {
                segments.Add(words);
                words = [];
            }
        }

        for (int i = 0; i < command.Length; i++)
        {
            char c = command[i];
            switch (c)
            {
                case '\'':
                {
                    inWord = true;
                    int end = command.IndexOf('\'', i + 1);
                    end = end < 0 ? command.Length : end;
                    word.Append(command, i + 1, end - i - 1);
                    i = end;
                    break;
                }
                case '"':
                {
                    inWord = true;
                    i++;
                    while (i < command.Length && command[i] != '"')
                    {
                        if (command[i] == escape && i + 1 < command.Length && (powerShell || command[i + 1] is '"' or '\\' or '$' or '`'))
                        {
                            i++;
                        }

                        word.Append(command[i]);
                        i++;
                    }

                    break;
                }
                case var _ when c == escape && i + 1 < command.Length:
                    if (command[i + 1] == '\n')
                    {
                        i++;
                        break;
                    }

                    inWord = true;
                    word.Append(command[++i]);
                    break;
                case ';' or '|' or '&' or '\n' or '\r' or '(' or ')' or '{' or '}':
                    // Brace groups only count as separators when they stand alone.
                    if (c is '{' or '}' && inWord)
                    {
                        word.Append(c);
                        break;
                    }

                    EndSegment();
                    break;
                case ' ' or '\t':
                    EndWord();
                    break;
                default:
                    inWord = true;
                    word.Append(c);
                    break;
            }
        }

        EndSegment();
        return segments;
    }
}
