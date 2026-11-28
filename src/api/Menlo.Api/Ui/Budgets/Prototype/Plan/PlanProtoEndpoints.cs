using Menlo.Api.Ui.Shared;
using Menlo.Ui.Components;
using Menlo.Ui.Utilities;

namespace Menlo.Api.Ui.Budgets.Prototype.Plan;

// PROTOTYPE (#658): the URL state, the page model and one POST that applies every change.

public sealed record PQuery(string Variant, int Year, string Target, string? Line, int? Month, string? Category, string? Edit,
    string View = "household", int Period = PQuery.YearPeriod, IReadOnlyList<string>? Open = null, string Filter = "all", string Who = "all",
    string? Panel = null, string Root = PQuery.PlanRoot)
{
    public const int YearPeriod = -1;

    public const string PlanRoot = "/budgets/prototype/plan";

    /// <summary>PROTOTYPE (#768): the payments prototype reuses this state on its own route.</summary>
    public const string PayRoot = "/budgets/prototype/pay";

    public static readonly (string Key, string Name)[] Variants =
    [
        ("A", "Edit in the grid"), ("B", "Edit in the rail"), ("C", "Set up by category")
    ];

    /// <summary>The side-rail sections, in rail order. "line" and "category" only show when one is open (variant B).</summary>
    public static readonly (string Key, string Label)[] Panels =
    [
        ("line", "Line"), ("category", "Category"), ("setup", "Set up the year"), ("proposals", "Proposals"), ("triggers", "Triggers"),
        ("year", "Year"), ("filters", "Filters"), ("views", "Whose budget"), ("key", "How to read the grid")
    ];

    public static readonly (string Key, string Label)[] Views =
    [
        ("household", "Household"), ("a", "Member A"), ("b", "Member B"), ("rental", "Rental unit")
    ];

    public static readonly (string Key, string Label)[] Filters =
    [
        ("all", "All"), ("over", "Over plan"), ("with", "With lines"), ("without", "Without lines")
    ];

    public static readonly (string Key, string Label)[] Beneficiaries =
    [
        ("all", "Everyone"), ("household", "Household"), ("a", "Member A"), ("b", "Member B"), ("child", "Child")
    ];

    public string VariantName => Variants.FirstOrDefault(v => v.Key == Variant).Name ?? Variant;

    public string ViewLabel => Views.First(v => v.Key == View).Label;

    public bool InProposal => Target != "plan";

    public bool IsYear => Period == YearPeriod;

    public IReadOnlyList<string> Opened => Open ?? [];

    public static PQuery From(HttpRequest request)
    {
        IQueryCollection q = request.Query;
        string variant = Variants.Any(v => v.Key == q["variant"]) ? q["variant"].ToString() : "A";
        int year = int.TryParse(q["year"], out int y) && PlanStore.Years.ContainsKey(y) ? y : PlanStore.FirstYear;
        PYear py = PlanStore.Get(year);
        string target = py.Proposals.Any(p => p.Id == q["target"]) ? q["target"].ToString() : "plan";
        string? line = py.Lines.Any(l => l.Id == q["line"]) || py.Proposals.SelectMany(p => p.Edits).Any(e => e.LineId == q["line"]) ? q["line"].ToString() : null;
        int? month = int.TryParse(q["month"], out int m) && m is >= 0 and <= 11 ? m : null;
        string? category = py.Categories.Any(c => c.Id == q["cat"]) ? q["cat"].ToString() : null;
        string? edit = string.IsNullOrEmpty(q["edit"]) ? null : q["edit"].ToString();
        string view = Views.Any(v => v.Key == q["view"]) ? q["view"].ToString() : "household";
        int period = int.TryParse(q["period"], out int pm) && pm is >= YearPeriod and <= 11 ? pm : YearPeriod;
        string filter = Filters.Any(f => f.Key == q["filter"]) ? q["filter"].ToString() : "all";
        string who = Beneficiaries.Any(b => b.Key == q["who"]) ? q["who"].ToString() : "all";
        IReadOnlyList<string> open = [.. q["open"].Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).Distinct()];
        string? panel = Panels.Any(x => x.Key == q["panel"]) ? q["panel"].ToString() : null;
        string root = request.Path.StartsWithSegments(PayRoot) ? PayRoot : PlanRoot;
        return new PQuery(variant, year, target, line, month, category, edit, view, period, open, filter, who, panel, root);
    }

    public string Href
    {
        get
        {
            List<string> parts = [$"variant={Variant}", $"year={Year}"];
            if (Target != "plan") parts.Add($"target={Target}");
            if (View != "household") parts.Add($"view={View}");
            if (Period != YearPeriod) parts.Add($"period={Period}");
            if (Filter != "all") parts.Add($"filter={Filter}");
            if (Who != "all") parts.Add($"who={Who}");
            parts.AddRange(Opened.Select(o => $"open={Uri.EscapeDataString(o)}"));
            if (Line is not null) parts.Add($"line={Line}");
            if (Month is not null) parts.Add($"month={Month}");
            if (Category is not null) parts.Add($"cat={Category}");
            if (Edit is not null) parts.Add($"edit={Uri.EscapeDataString(Edit)}");
            if (Panel is not null) parts.Add($"panel={Panel}");
            return Root + "?" + string.Join('&', parts);
        }
    }

    /// <summary>No line, cell or category open.</summary>
    public PQuery Closed => this with { Line = null, Month = null, Edit = null, Category = null, Panel = Panel is "line" or "category" ? null : Panel };

    /// <summary>The same state with the side rail shut: for links outside the rail.</summary>
    public PQuery Shut => this with { Panel = null };

    public string WithVariant(string v) => (this with { Variant = v, Line = null, Month = null, Edit = null, Category = null, Panel = null }).Href;

    public string WithTarget(string t) => (this with { Target = t }).Href;

    public string Cell(string line, int month) => (this with { Line = line, Month = month, Edit = null, Category = null, Panel = null }).Href;

    public string WithLine(string? line) => (this with { Line = line, Month = null, Edit = null }).Href;

    public string WithCategory(string category) => (this with { Category = category, Edit = null, Line = null, Month = null }).Href;

    public string Editing(string what) => (this with { Edit = what, Line = null, Month = null }).Href;

    public string WithPanel(string panel) => (this with { Panel = panel }).Href;

    public string WithView(string view) => (this with { View = view }).Href;

    public string WithPeriod(int period) => (this with { Period = period }).Href;

    public string WithFilter(string filter) => (this with { Filter = filter }).Href;

    public string WithWho(string who) => (this with { Who = who }).Href;

    public bool IsOpen(string key) => Opened.Contains(key);

    public string ToggleOpen(string key) => (this with { Open = IsOpen(key) ? [.. Opened.Where(o => o != key)] : [.. Opened, key] }).Href;
}

/// <summary>Everything the page renders: the year as committed, and the preview with proposals on.</summary>
public sealed record PSheet(PQuery Query, PYear Plan, PYear Preview, PProposal? Target, IReadOnlyDictionary<string, string?> Conflicts,
    IReadOnlyDictionary<string, decimal> Effects, HashSet<(string Line, int Month)> Changed)
{
    public static PSheet Build(PQuery q)
    {
        PYear plan = PlanStore.Get(q.Year);
        PProposal? target = plan.Proposals.FirstOrDefault(p => p.Id == q.Target);

        // The preview shows every switched-on proposal, and the one being worked on even if it's off
        List<PProposal> shown = [.. plan.Proposals.Where(p => p.On || p == target)];
        PYear preview = plan.With(shown);
        HashSet<(string, int)> changed = [.. shown.SelectMany(plan.Touches)];
        decimal year = Enumerable.Range(0, 12).Sum(plan.Net);
        return new PSheet(q, plan, preview, target,
            plan.Proposals.ToDictionary(p => p.Id, p => p.On ? null : plan.Conflict(p)),
            plan.Proposals.ToDictionary(p => p.Id, p => Enumerable.Range(0, 12).Sum(plan.With([p]).Net) - year),
            changed);
    }

    public bool Previewing => Changed.Count > 0;

    public bool IsChanged(string line, int month) => Changed.Contains((line, month));

    public PLine? Line => Query.Line is null ? null : Preview.Lines.FirstOrDefault(l => l.Id == Query.Line);

    public IReadOnlyList<MnlOption> MonthOptions(bool fromCurrent = true) =>
        [.. Enumerable.Range(fromCurrent ? Plan.Current : 0, 12 - (fromCurrent ? Plan.Current : 0))
            .Select(m => new MnlOption(m.ToString(System.Globalization.CultureInfo.InvariantCulture), PFormat.MonthLong(m)))];

    public IReadOnlyList<MnlOption> CategoryOptions =>
        [.. Plan.Tops.SelectMany(t => Plan.Children(t.Id).Select(c => new MnlOption(c.Id, $"{t.Name} › {c.Name}")))];

    public IReadOnlyList<MnlOption> TriggerOptions => [.. Plan.Triggers.Select(t => new MnlOption(t.Id, $"{t.Name} (expected {PFormat.Month(t.Expected)})"))];

    public static readonly IReadOnlyList<MnlOption> TypeOptions =
        [new("Fixed", "Fixed"), new("Variable", "Variable"), new("Provision", "Provision"), new("Event", "Event")];

    public static readonly IReadOnlyList<MnlOption> WhoOptions =
        [new("household", "Household"), new("a", "Member A"), new("b", "Member B"), new("child", "Child")];

    /// <summary>Where a change goes: straight into the plan, or into the proposal being worked on.</summary>
    public string SaveLabel => Target is null ? "Save and commit" : $"Add to \"{Target.Name}\"";

    public IEnumerable<PLine> LinesIn(string categoryId) => Preview.Lines.Where(l => l.CategoryId == categoryId);

    /// <summary>The grid as the views prototype (#596) shows it, built from this year.</summary>
    public PBoard Board => board ??= PBoard.Build(this);

    private PBoard? board;
}

internal static class PlanProtoEndpoints
{
    public static IEndpointRouteBuilder MapPlanPrototype(this IEndpointRouteBuilder app)
    {
        app.MapGet("/budgets/prototype/plan", (HttpContext context) =>
                Render.Page<PlanProtoPage>(context, PSheet.Build(PQuery.From(context.Request))))
            .AllowAnonymous();

        app.MapGet("/budgets/prototype/plan/copy", (HttpContext context) =>
                Render.Page<PlanCopyPage>(context, PSheet.Build(PQuery.From(context.Request))))
            .AllowAnonymous();

        app.MapPost("/budgets/prototype/plan/act", (HttpContext context, IFormCollection form) =>
            {
                string back = form["back"].ToString();
                back = back.StartsWith("/budgets/prototype/plan", StringComparison.Ordinal) ? back : "/budgets/prototype/plan";
                (string message, MnlTone tone, string? go) = Act(form);
                Flash.Set(context, message, tone);
                return Navigate.To(context, go ?? back);
            })
            .AllowAnonymous();

        return app;
    }

    private static (string, MnlTone, string?) Act(IFormCollection f)
    {
        string op = f["op"].ToString();
        if (op == "reset")
        {
            PlanStore.Reset();
            return ("Prototype data put back.", MnlTone.Info, "/budgets/prototype/plan?variant=" + f["variant"]);
        }

        int yearNo = int.TryParse(f["year"], out int yn) ? yn : PlanStore.FirstYear;
        PYear y = PlanStore.Get(yearNo);
        PProposal? target = y.Proposals.FirstOrDefault(p => p.Id == f["target"]);
        string lineId = f["line"].ToString();
        int month = int.TryParse(f["month"], out int mm) ? mm : y.Current;
        PKind kind = f["valueKind"] == "percent" ? PKind.Percent : PKind.Rand;
        bool hasAmount = MoneyFormat.TryParse(f["amount"], out decimal amount);

        (string, MnlTone, string?) Fail(string why) => (why, MnlTone.Error, null);

        // A change goes into the proposal being worked on, or is committed straight away
        (string, MnlTone, string?) Commit(PEdit edit)
        {
            string what = edit.Describe(y);
            PlanStore.Change(() =>
            {
                if (target is not null)
                {
                    target.Edits.Add(edit);
                }
                else
                {
                    edit.Apply(y);
                    y.Write();
                }
            });
            return target is null
                ? ($"Committed: {what}.", MnlTone.Success, null)
                : ($"Added to \"{target.Name}\": {what}. Not committed yet.", MnlTone.Info, null);
        }

        bool Frozen(int m) => m < y.Current;

        switch (op)
        {
            case "change":
                {
                    string change = f["change"].ToString();
                    if (change is not ("clear-override" or "seasonal") && !hasAmount) return Fail("Enter an amount.");
                    return change switch
                    {
                        "dated" when Frozen(month) => Fail($"{PFormat.MonthLong(month)} is frozen. Changes apply from {PFormat.MonthLong(y.Current)} onwards."),
                        "dated" => Commit(new DatedEdit(lineId, month, amount)),
                        "yearly" => Commit(new YearlyEdit(lineId, month, amount, kind)),
                        "seasonal" when !hasAmount => Fail("Enter an amount or percentage."),
                        "seasonal" when f["months"].Count == 0 => Fail("Pick the months it applies in."),
                        "seasonal" => Commit(new SeasonalEdit(lineId, [.. f["months"].Select(s => int.Parse(s!, System.Globalization.CultureInfo.InvariantCulture))], amount, kind)),
                        "conditional" when y.Triggers.All(t => t.Id != f["trigger"]) => Fail("Pick a trigger, or add one under Triggers."),
                        "conditional" => Commit(new ConditionalEdit(lineId, f["trigger"].ToString(), amount, kind)),
                        "override" or "clear-override" or "event" when Frozen(month) => Fail($"{PFormat.MonthLong(month)} is frozen and can't be changed."),
                        "override" => Commit(new OverrideEdit(lineId, month, amount)),
                        "clear-override" => Commit(new OverrideEdit(lineId, month, null)),
                        "event" => Commit(new EventEdit(lineId, month, amount)),
                        _ => Fail("Unknown change.")
                    };
                }
            case "remove-layer":
                return Commit(new RemoveLayerEdit(lineId, f["layer"].ToString(), int.Parse(f["index"]!, System.Globalization.CultureInfo.InvariantCulture)));
            case "line-add":
                {
                    string name = f["name"].ToString().Trim();
                    if (name.Length == 0) return Fail("Give the line a name.");
                    if (!hasAmount) return Fail("Enter the line's amount.");
                    if (Frozen(month)) return Fail($"{PFormat.MonthLong(month)} is frozen. A new line starts in {PFormat.MonthLong(y.Current)} or later.");
                    ProtoLineType type = Enum.TryParse(f["type"], out ProtoLineType t) ? t : ProtoLineType.Fixed;
                    string category = f["category"].ToString();
                    bool income = y.Categories.FirstOrDefault(c => c.Id == category)?.ParentId == "income";
                    string id = $"{Slug(name)}-{Guid.NewGuid().ToString("N")[..4]}";
                    return Commit(new NewLineEdit(id, name, category, type, income, amount, month));
                }
            case "line-save":
                PlanStore.Change(() =>
                {
                    PLine l = y.Line(lineId);
                    l.Name = f["name"].ToString().Trim() is { Length: > 0 } n ? n : l.Name;
                    l.CategoryId = y.Categories.Any(c => c.Id == f["category"]) ? f["category"].ToString() : l.CategoryId;
                    l.Type = Enum.TryParse(f["type"], out ProtoLineType t) ? t : l.Type;
                    l.Beneficiary = f["who"].ToString() is { Length: > 0 } w ? w : l.Beneficiary;
                    l.ShareA = int.TryParse(f["shareA"], out int a) ? Math.Clamp(a, 0, 100) : l.ShareA;
                    l.RentalShare = int.TryParse(f["rental"], out int r) ? Math.Clamp(r, 0, 100) : l.RentalShare;
                });
                return ("Line details saved.", MnlTone.Success, null);
            case "line-remove":
                PlanStore.Change(() =>
                {
                    // Ending a line is a dated change to zero from the current month; frozen months keep their plan
                    PLine l = y.Line(lineId);
                    if (l.Stored.Take(y.Current).All(v => v == 0)) y.Lines.Remove(l);
                    else { l.Dated.Add(new PDated(y.Current, 0)); l.Events = l.Events.Where(e => e.Key < y.Current).ToDictionary(); l.Overrides.Clear(); y.Write(); }
                });
                return ($"Line ended from {PFormat.MonthLong(y.Current)}. Earlier months keep their plan.", MnlTone.Success, null);
            case "cat-add":
                {
                    string name = f["name"].ToString().Trim();
                    if (name.Length == 0) return Fail("Give the category a name.");
                    string? parent = y.Categories.Any(c => c.Id == f["parent"] && c.ParentId is null) ? f["parent"].ToString() : null;
                    PlanStore.Change(() => y.Categories.Add(new PCategory { Id = $"{Slug(name)}-{Guid.NewGuid().ToString("N")[..4]}", Name = name, ParentId = parent }));
                    return ($"Category \"{name}\" added.", MnlTone.Success, null);
                }
            case "cat-rename":
                PlanStore.Change(() =>
                {
                    PCategory c = y.Categories.First(c => c.Id == f["id"]);
                    c.Name = f["name"].ToString().Trim() is { Length: > 0 } n ? n : c.Name;
                });
                return ("Category renamed.", MnlTone.Success, null);
            case "cat-move":
                {
                    PCategory c = y.Categories.First(c => c.Id == f["id"]);
                    string parent = f["parent"].ToString();
                    if (c.ParentId is null) return Fail("Only a subcategory can move. Categories nest at most two deep.");
                    PlanStore.Change(() => c.ParentId = parent);
                    return ($"\"{c.Name}\" moved to {y.Categories.First(x => x.Id == parent).Name}.", MnlTone.Success, null);
                }
            case "cat-remove":
                {
                    PCategory c = y.Categories.First(c => c.Id == f["id"]);
                    bool used = y.Lines.Any(l => l.CategoryId == c.Id || y.Children(c.Id).Any(k => k.Id == l.CategoryId));
                    if (used) return Fail($"\"{c.Name}\" still has budget lines. Move or end them first.");
                    PlanStore.Change(() => y.Categories.RemoveAll(x => x == c || x.ParentId == c.Id));
                    return ($"Category \"{c.Name}\" removed.", MnlTone.Success, null);
                }
            case "setup-save":
                {
                    // Variant C: one form per category. Every changed amount is a dated change from the chosen month
                    if (Frozen(month)) return Fail($"{PFormat.MonthLong(month)} is frozen.");
                    List<PEdit> edits = [];
                    string[] ids = [.. f["lineId"].Select(s => s!)];
                    for (int i = 0; i < ids.Length; i++)
                    {
                        PLine l = y.Line(ids[i]);
                        if (!MoneyFormat.TryParse(f["lineAmount"][i], out decimal a)) continue;
                        decimal now = l.Type == ProtoLineType.Event ? l.Events.GetValueOrDefault(month) : y.BaselineAt(l, month);
                        if (a == now) continue;
                        edits.Add(l.Type == ProtoLineType.Event ? new EventEdit(l.Id, month, a) : new DatedEdit(l.Id, month, a));
                    }

                    string cat = f["category"].ToString();
                    PlanStore.Change(() =>
                    {
                        y.Reviewed.Add(cat);
                        if (target is not null) target.Edits.AddRange(edits);
                        else { edits.ForEach(e => e.Apply(y)); y.Write(); }
                    });
                    string where = target is null ? $"committed from {PFormat.Month(month)}" : $"added to \"{target.Name}\"";
                    return (edits.Count == 0 ? $"{y.Path(cat)} marked as done. No amounts changed." : $"{y.Path(cat)}: {edits.Count} change(s) {where}.",
                        MnlTone.Success, null);
                }
            case "proposal-new":
                {
                    string name = f["name"].ToString().Trim();
                    if (name.Length == 0) return Fail("Give the proposal a name.");
                    PProposal p = new() { Id = $"{Slug(name)}-{Guid.NewGuid().ToString("N")[..4]}", Name = name, On = true };
                    PlanStore.Change(() => y.Proposals.Add(p));
                    string back = f["back"].ToString();
                    string go = back.Contains("target=", StringComparison.Ordinal)
                        ? System.Text.RegularExpressions.Regex.Replace(back, "target=[^&]*", $"target={p.Id}")
                        : back + $"&target={p.Id}";
                    return ($"Working on \"{name}\". Changes now go into it until you switch back to the plan.", MnlTone.Info, go);
                }
            case "proposal-toggle":
                {
                    PProposal p = y.Proposals.First(p => p.Id == f["id"]);
                    if (!p.On && y.Conflict(p) is string why) return Fail(why);
                    PlanStore.Change(() => p.On = !p.On);
                    return ($"\"{p.Name}\" switched {(p.On ? "on" : "off")}.", MnlTone.Info, null);
                }
            case "proposal-commit":
                {
                    PProposal p = y.Proposals.First(p => p.Id == f["id"]);
                    if (p.Edits.Count == 0) return Fail($"\"{p.Name}\" has no changes yet.");
                    PlanStore.Change(() =>
                    {
                        p.Edits.ForEach(e => e.Apply(y));
                        y.Proposals.Remove(p);
                        y.Write();
                    });
                    return ($"\"{p.Name}\" committed from {PFormat.MonthLong(y.Current)}. It's part of the plan now.", MnlTone.Success,
                        System.Text.RegularExpressions.Regex.Replace(f["back"].ToString(), "&?target=[^&]*", ""));
                }
            case "proposal-drop":
                {
                    PProposal p = y.Proposals.First(p => p.Id == f["id"]);
                    PlanStore.Change(() => y.Proposals.Remove(p));
                    return ($"\"{p.Name}\" dropped.", MnlTone.Info, System.Text.RegularExpressions.Regex.Replace(f["back"].ToString(), "&?target=[^&]*", ""));
                }
            case "proposal-remove-edit":
                {
                    PProposal p = y.Proposals.First(p => p.Id == f["id"]);
                    int i = int.Parse(f["index"]!, System.Globalization.CultureInfo.InvariantCulture);
                    PlanStore.Change(() => p.Edits.RemoveAt(i));
                    return ("Change taken out of the proposal.", MnlTone.Info, null);
                }
            case "trigger-add":
                {
                    string name = f["name"].ToString().Trim();
                    if (name.Length == 0) return Fail("Give the trigger a name.");
                    if (Frozen(month)) return Fail("A trigger's expected date is the current month or later.");
                    PlanStore.Change(() => { y.Triggers.Add(new PTrigger { Id = $"{Slug(name)}-{Guid.NewGuid().ToString("N")[..4]}", Name = name, Expected = month }); y.Write(); });
                    return ($"Trigger \"{name}\" added, expected {PFormat.MonthLong(month)}. Tie conditional changes to it from a line.", MnlTone.Success, null);
                }
            case "trigger-move":
                {
                    PTrigger t = y.Triggers.First(t => t.Id == f["id"]);
                    if (Frozen(month)) return Fail("The expected date can only move to the current month or later.");
                    if (t.Realized) return Fail("This trigger is realized: its date is when it happened.");
                    PlanStore.Change(() => { t.Expected = month; y.Write(); });
                    return ($"\"{t.Name}\" now expected in {PFormat.MonthLong(month)}. Its conditional changes moved with it.", MnlTone.Success, null);
                }
            case "trigger-realize":
                {
                    PTrigger t = y.Triggers.First(t => t.Id == f["id"]);
                    PlanStore.Change(() => { t.Realized = true; t.Expected = Math.Max(t.Expected, y.Current); y.Write(); });
                    return ($"\"{t.Name}\" is realized. It's fixed at {PFormat.MonthLong(t.Expected)}.", MnlTone.Success, null);
                }
            case "copy":
                {
                    if (PlanStore.Years.ContainsKey(y.Year + 1)) return Fail($"{y.Year + 1} already exists. Undo it first to copy again.");
                    HashSet<string> realized = [.. y.Triggers.Where(t => t.Realized || f[$"trigger-{t.Id}"] == "realized").Select(t => t.Id)];
                    HashSet<string> keep = [.. y.Lines.Where(l => l.Type == ProtoLineType.Event && f[$"event-{l.Id}"] == "keep").Select(l => l.Id)];
                    PlanStore.Change(() => PlanStore.Years[y.Year + 1] = PlanStore.Copy(y, realized, keep));
                    return ($"{y.Year + 1} budget created from {y.Year}. It's planned from January.", MnlTone.Success,
                        $"/budgets/prototype/plan?variant={f["variant"]}&year={y.Year + 1}");
                }
            case "copy-undo":
                PlanStore.Change(() => PlanStore.Years.Remove(yearNo + 1));
                return ($"{yearNo + 1} removed.", MnlTone.Info, $"/budgets/prototype/plan/copy?variant={f["variant"]}&year={yearNo}");
            default:
                return Fail("Unknown action.");
        }
    }

    private static string Slug(string name) =>
        new([.. name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')]);
}
