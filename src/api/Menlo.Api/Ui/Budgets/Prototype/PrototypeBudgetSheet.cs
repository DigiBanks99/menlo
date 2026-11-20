using Menlo.Ui.Components;
using System.Globalization;

namespace Menlo.Api.Ui.Budgets.Prototype;

// PROTOTYPE (#596): the URL state, the calculations and the formatting every variant shares.

/// <summary>Everything the prototype keeps in the URL.</summary>
public sealed record ProtoQuery(
    string Variant,
    string View,
    int Month,
    IReadOnlyList<string> Open,
    string Filter,
    string Who,
    IReadOnlyList<string> Proposals,
    string? Panel = null,
    string? Commit = null)
{
    /// <summary>The side-rail sections variant D offers, in rail order.</summary>
    public static readonly (string Key, string Label)[] Panels =
    [
        ("proposals", "Proposals"), ("filters", "Filters"), ("views", "Whose budget"), ("key", "How to read the grid")
    ];

    public const int YearPeriod = -1;

    public static readonly (string Key, string Name)[] Variants =
    [
        ("D", "Grid with side rail"), ("A", "Year grid"), ("B", "Month focus"), ("C", "By line type")
    ];

    public static readonly (string Key, string Label)[] Filters =
    [
        ("all", "All"), ("over", "Over plan"), ("with", "With lines"), ("without", "Without lines")
    ];

    public bool IsYear => Month == YearPeriod;

    public string VariantName => Variants.FirstOrDefault(v => v.Key == Variant).Name ?? Variant;

    public static ProtoQuery From(HttpRequest request)
    {
        IQueryCollection q = request.Query;
        string variant = Variants.Any(v => v.Key == q["variant"]) ? q["variant"].ToString() : "D";
        string view = ProtoBudget.Views.Any(v => v.Key == q["view"]) ? q["view"].ToString() : "household";
        int month = int.TryParse(q["month"], out int m) && m is >= YearPeriod and <= 11
            ? m
            : variant == "B" ? ProtoBudget.CurrentMonth : YearPeriod;
        string filter = Filters.Any(f => f.Key == q["filter"]) ? q["filter"].ToString() : "all";
        string who = ProtoBudget.Beneficiaries.Any(b => b.Key == q["who"]) ? q["who"].ToString() : "all";
        IReadOnlyList<string> open = [.. q["open"].Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).Distinct()];
        IReadOnlyList<string> proposals = [.. q["p"].Where(s => ProtoBudget.Proposals.Any(p => p.Id == s)).Select(s => s!).Distinct()];
        string? panel = Panels.Any(x => x.Key == q["panel"]) ? q["panel"].ToString() : null;
        string? commit = ProtoBudget.Proposals.Any(x => x.Id == q["commit"]) ? q["commit"].ToString() : null;
        return new ProtoQuery(variant, view, month, open, filter, who, proposals, panel, commit);
    }

    public string Href
    {
        get
        {
            List<string> parts =
            [
                $"variant={Variant}", $"view={View}", $"month={Month}", $"filter={Filter}", $"who={Who}"
            ];
            parts.AddRange(Open.Select(o => $"open={Uri.EscapeDataString(o)}"));
            parts.AddRange(Proposals.Select(p => $"p={p}"));
            if (Panel is not null)
            {
                parts.Add($"panel={Panel}");
            }

            if (Commit is not null)
            {
                parts.Add($"commit={Commit}");
            }
            return "/budgets/prototype?" + string.Join('&', parts);
        }
    }

    public string WithVariant(string variant) =>
        (this with { Variant = variant, Month = variant == "B" ? (IsYear ? ProtoBudget.CurrentMonth : Month) : Month }).Href;

    /// <summary>The same state with the side rail closed and no commit message: for links outside the rail.</summary>
    public ProtoQuery Closed => this with { Panel = null, Commit = null };

    public string WithPanel(string panel) => (this with { Panel = panel, Commit = null }).Href;

    public string WithCommit(string proposal) => (this with { Commit = proposal }).Href;

    public string WithView(string view) => (this with { View = view }).Href;

    public string WithMonth(int month) => (this with { Month = month }).Href;

    public string WithFilter(string filter) => (this with { Filter = filter }).Href;

    public string WithWho(string who) => (this with { Who = who }).Href;

    public bool IsOpen(string key) => Open.Contains(key);

    public string ToggleOpen(string key) =>
        (this with { Open = IsOpen(key) ? [.. Open.Where(o => o != key)] : [.. Open, key] }).Href;

    public bool IsOn(string proposal) => Proposals.Contains(proposal);

    public string ToggleProposal(string proposal) =>
        (this with { Proposals = IsOn(proposal) ? [.. Proposals.Where(p => p != proposal)] : [.. Proposals, proposal], Commit = null }).Href;
}

/// <summary>A budget line as one view sees it, with proposals applied on top for the preview.</summary>
public sealed record SheetLine(
    ProtoLine Line,
    decimal[] Planned,
    decimal[] Preview,
    decimal?[] Actual,
    bool[] Changed,
    bool FromProposal)
{
    public int Sign => Line.Income ? 1 : -1;

    public bool HasOverride => Line.OverrideMonth is not null;

    public decimal YearPlanned => Planned.Sum();

    public decimal YearPreview => Preview.Sum();

    public decimal ToDatePlanned => Planned.Take(ProtoBudget.CurrentMonth).Sum();

    public decimal ToDateActual => Actual.Take(ProtoBudget.CurrentMonth).Sum(a => a ?? 0);

    public bool AnyChanged => Changed.Any(c => c);

    public bool OverPlan => Line.Income ? ToDateActual < ToDatePlanned * 0.98m : ToDateActual > ToDatePlanned * 1.02m;

    public decimal PlannedFor(int month) => month == ProtoQuery.YearPeriod ? YearPlanned : Planned[month];

    public decimal PreviewFor(int month) => month == ProtoQuery.YearPeriod ? YearPreview : Preview[month];

    public decimal? ActualFor(int month) =>
        month == ProtoQuery.YearPeriod ? Actual.Sum(a => a ?? 0) : Actual[month];
}

public sealed record SheetCategory(string Group, string Name, IReadOnlyList<SheetLine> Lines)
{
    public string Key => $"{Group}/{Name}";

    public decimal PlannedFor(int month) => Lines.Sum(l => l.PlannedFor(month));

    public decimal PreviewFor(int month) => Lines.Sum(l => l.PreviewFor(month));

    public decimal? ActualFor(int month) => Lines.All(l => l.ActualFor(month) is null) ? null : Lines.Sum(l => l.ActualFor(month) ?? 0);

    public bool AnyChanged => Lines.Any(l => l.AnyChanged || l.FromProposal);
}

public sealed record SheetGroup(string Name, IReadOnlyList<SheetCategory> Categories)
{
    public bool Income => Name == "Income";

    public IEnumerable<SheetLine> Lines => Categories.SelectMany(c => c.Lines);

    public decimal PlannedFor(int month) => Lines.Sum(l => l.PlannedFor(month));

    public decimal PreviewFor(int month) => Lines.Sum(l => l.PreviewFor(month));

    public decimal? ActualFor(int month) => Lines.All(l => l.ActualFor(month) is null) ? null : Lines.Sum(l => l.ActualFor(month) ?? 0);

    public bool AnyChanged => Categories.Any(c => c.AnyChanged);
}

/// <summary>Net for one view, for the page's period.</summary>
public sealed record ViewNet(string Key, string Label, decimal Planned, decimal Preview, decimal? Actual);

/// <summary>Everything a variant renders.</summary>
public sealed class ProtoSheet
{
    public required ProtoQuery Query { get; init; }

    public required string ViewLabel { get; init; }

    public required IReadOnlyList<SheetGroup> Groups { get; init; }

    public required IReadOnlyList<SheetLine> AllLines { get; init; }

    public required decimal[] NetPlanned { get; init; }

    public required decimal[] NetPreview { get; init; }

    public required decimal?[] NetActual { get; init; }

    public required IReadOnlyList<ProtoProposal> Active { get; init; }

    public required IReadOnlyList<(ProtoProposal Proposal, string Reason)> Blocked { get; init; }

    public required IReadOnlyList<ViewNet> Views { get; init; }

    /// <summary>What each proposal alone does to this view's net for the year.</summary>
    public required IReadOnlyDictionary<string, decimal> Effects { get; init; }

    public bool Previewing => Active.Count > 0;

    public decimal NetPlannedFor(int month) => month == ProtoQuery.YearPeriod ? NetPlanned.Sum() : NetPlanned[month];

    public decimal NetPreviewFor(int month) => month == ProtoQuery.YearPeriod ? NetPreview.Sum() : NetPreview[month];

    public decimal? NetActualFor(int month) => month == ProtoQuery.YearPeriod ? NetActual.Sum(a => a ?? 0) : NetActual[month];

    public IEnumerable<SheetLine> OfType(ProtoLineType type) => Groups.SelectMany(g => g.Lines).Where(l => l.Line.Type == type);

    public static ProtoSheet Build(ProtoQuery query)
    {
        (List<ProtoProposal> active, List<(ProtoProposal, string)> blocked) = Resolve(query.Proposals);

        List<SheetLine> lines = LinesFor(query.View, active);
        (decimal[] netPlanned, decimal[] netPreview, decimal?[] netActual) = Net(lines);

        IEnumerable<SheetLine> shown = lines.Where(l => query.Who == "all" || l.Line.Beneficiary == query.Who);
        if (query.Filter == "over")
        {
            shown = shown.Where(l => l.OverPlan);
        }

        List<SheetGroup> groups = [];
        foreach (string group in ProtoBudget.Groups)
        {
            List<SheetCategory> categories = [.. shown
                .Where(l => l.Line.Group == group)
                .GroupBy(l => l.Line.Category)
                .Select(g => new SheetCategory(group, g.Key, [.. g]))];

            if (query.Filter is "all" or "without" && query.Who == "all")
            {
                categories.AddRange(ProtoBudget.EmptyCategories
                    .Where(e => e.Group == group)
                    .Select(e => new SheetCategory(group, e.Category, [])));
            }

            categories = query.Filter switch
            {
                "with" => [.. categories.Where(c => c.Lines.Count > 0)],
                "without" => [.. categories.Where(c => c.Lines.Count == 0)],
                _ => categories
            };

            if (categories.Count > 0)
            {
                groups.Add(new SheetGroup(group, categories));
            }
        }

        List<ViewNet> views = [];
        foreach ((string key, string label) in ProtoBudget.Views)
        {
            (decimal[] p, decimal[] pv, decimal?[] a) = Net(LinesFor(key, active));
            int month = query.Month;
            views.Add(new ViewNet(
                key,
                label,
                month == ProtoQuery.YearPeriod ? p.Sum() : p[month],
                month == ProtoQuery.YearPeriod ? pv.Sum() : pv[month],
                month == ProtoQuery.YearPeriod ? a.Sum(x => x ?? 0) : a[month]));
        }

        return new ProtoSheet
        {
            Query = query,
            ViewLabel = ProtoBudget.Views.First(v => v.Key == query.View).Label,
            Groups = groups,
            AllLines = lines,
            NetPlanned = netPlanned,
            NetPreview = netPreview,
            NetActual = netActual,
            Active = active,
            Blocked = blocked,
            Views = views,
            Effects = ProtoBudget.Proposals.ToDictionary(
                p => p.Id,
                p => Net(LinesFor(query.View, [p])).Item2.Sum() - netPlanned.Sum())
        };
    }

    // Proposals are applied in the order they are listed. One that changes a line and month another switched-on
    // proposal already changes is blocked, not merged.
    private static (List<ProtoProposal>, List<(ProtoProposal, string)>) Resolve(IReadOnlyList<string> requested)
    {
        List<ProtoProposal> active = [];
        List<(ProtoProposal, string)> blocked = [];
        foreach (ProtoProposal proposal in ProtoBudget.Proposals.Where(p => requested.Contains(p.Id)))
        {
            ProtoProposal? clash = active.FirstOrDefault(a => a.Changes.Any(c => proposal.Changes.Any(o => o.LineId == c.LineId && o.Month == c.Month)));
            if (clash is null)
            {
                active.Add(proposal);
                continue;
            }

            ProtoChange shared = proposal.Changes.First(c => clash.Changes.Any(o => o.LineId == c.LineId && o.Month == c.Month));
            string line = ProtoBudget.Lines.First(l => l.Id == shared.LineId).Name;
            blocked.Add((proposal, $"Can't be on with \"{clash.Name}\": both change {line} in {ProtoBudget.MonthLong(shared.Month)}."));
        }

        return (active, blocked);
    }

    private static List<SheetLine> LinesFor(string view, IReadOnlyList<ProtoProposal> active)
    {
        IEnumerable<ProtoLine> source = ProtoBudget.Lines.Concat(active.Where(p => p.NewLine is not null).Select(p => p.NewLine!));
        List<SheetLine> result = [];
        foreach (ProtoLine line in source)
        {
            decimal factor = Factor(line, view);
            if (factor == 0)
            {
                continue;
            }

            bool fromProposal = active.Any(p => p.NewLine == line);
            decimal[] planned = [.. line.Planned.Select(v => fromProposal ? 0 : Math.Round(v * factor, 0))];
            decimal[] preview = [.. line.Planned.Select(v => Math.Round(v * factor, 0))];
            bool[] changed = new bool[12];
            foreach (ProtoChange change in active.SelectMany(p => p.Changes).Where(c => c.LineId == line.Id && c.Month >= ProtoBudget.CurrentMonth))
            {
                preview[change.Month] = Math.Round(change.Amount * factor, 0);
                changed[change.Month] = true;
            }

            if (fromProposal)
            {
                for (int m = 0; m < 12; m++)
                {
                    changed[m] = preview[m] != 0;
                }
            }

            decimal?[] actual = [.. line.Actual.Select(a => a is null ? (decimal?)null : Math.Round(a.Value * factor, 0))];
            result.Add(new SheetLine(line, planned, preview, actual, changed, fromProposal));
        }

        return result;
    }

    private static decimal Factor(ProtoLine line, string view) => view switch
    {
        "a" => line.ShareA,
        "b" => line.ShareB,
        "rental" => line.RentalIncome ? 1 : line.Income || line.PreTax ? 0 : line.RentalShare,
        _ => 1
    };

    private static (decimal[], decimal[], decimal?[]) Net(IReadOnlyList<SheetLine> lines)
    {
        decimal[] planned = new decimal[12];
        decimal[] preview = new decimal[12];
        decimal?[] actual = new decimal?[12];
        for (int m = 0; m < 12; m++)
        {
            planned[m] = lines.Sum(l => l.Sign * l.Planned[m]);
            preview[m] = lines.Sum(l => l.Sign * l.Preview[m]);
            actual[m] = m > ProtoBudget.CurrentMonth ? null : lines.Sum(l => l.Sign * (l.Actual[m] ?? 0));
        }

        return (planned, preview, actual);
    }
}

/// <summary>Formatting and tone rules shared by the variants.</summary>
public static class ProtoFormat
{
    private static readonly NumberFormatInfo Numbers = new() { NumberGroupSeparator = " ", NumberGroupSizes = [3] };

    public static string Money(decimal value) => (value < 0 ? "−" : "") + "R " + Math.Abs(value).ToString("#,0", Numbers);

    public static string Money(decimal? value) => value is null ? "" : Money(value.Value);

    public static string Delta(decimal value) => value == 0 ? "±R 0" : (value > 0 ? "+" : "−") + "R " + Math.Abs(value).ToString("#,0", Numbers);

    public static string Percent(decimal value) => value.ToString("0", CultureInfo.InvariantCulture) + "%";

    public static string MonthState(int month) =>
        month < ProtoBudget.CurrentMonth ? "frozen" : month == ProtoBudget.CurrentMonth ? "current" : "future";

    public static string PeriodLabel(int month) =>
        month == ProtoQuery.YearPeriod ? $"{ProtoBudget.Year}" : $"{ProtoBudget.MonthLong(month)} {ProtoBudget.Year}";

    public static string TypeLabel(ProtoLineType type) => type switch
    {
        ProtoLineType.Fixed => "Fixed",
        ProtoLineType.Variable => "Variable",
        ProtoLineType.Provision => "Provision",
        _ => "Event"
    };

    /// <summary>Variance is actual minus planned. Over on an expense, or under on income, is unfavourable.</summary>
    public static MnlTone VarianceTone(bool income, decimal planned, decimal? actual)
    {
        if (actual is null || (planned == 0 && actual == 0))
        {
            return MnlTone.Neutral;
        }

        decimal diff = actual.Value - planned;
        decimal relative = Math.Abs(diff) / Math.Max(planned, 1);
        if (relative <= 0.02m)
        {
            return MnlTone.Success;
        }

        bool unfavourable = income ? diff < 0 : diff > 0;
        return unfavourable ? (relative > 0.15m ? MnlTone.Error : MnlTone.Warning) : MnlTone.Info;
    }

    public static MnlTone NetTone(decimal planned, decimal? actual) =>
        actual is null ? MnlTone.Neutral : actual >= planned ? MnlTone.Success : actual >= planned - Math.Abs(planned) * 0.05m ? MnlTone.Warning : MnlTone.Error;

    public static string ToneKey(MnlTone tone) => tone.ToString().ToLowerInvariant();

    /// <summary>How the line is doing, judged the way its line type says.</summary>
    public static (string Text, MnlTone Tone) Judge(SheetLine line)
    {
        int frozen = ProtoBudget.CurrentMonth;
        switch (line.Line.Type)
        {
            case ProtoLineType.Fixed:
                {
                    int off = Enumerable.Range(0, frozen).Count(m => VarianceTone(line.Line.Income, line.Planned[m], line.Actual[m]) != MnlTone.Success
                                                                   && !(line.Planned[m] == 0 && line.Actual[m] == 0));
                    return off == 0
                        ? ("Matched the plan every month", MnlTone.Success)
                        : ($"Off plan in {off} of {frozen} months ({Delta(line.ToDateActual - line.ToDatePlanned)} to date)", MnlTone.Warning);
                }
            case ProtoLineType.Variable:
                {
                    decimal planned = line.Planned.Skip(frozen - 3).Take(3).Sum();
                    decimal actual = line.Actual.Skip(frozen - 3).Take(3).Sum(a => a ?? 0);
                    if (planned == 0)
                    {
                        return ("No allowance yet", MnlTone.Neutral);
                    }

                    decimal pct = Math.Round((actual - planned) / planned * 100, 0);
                    bool income = line.Line.Income;
                    return Math.Abs(pct) <= 5
                        ? ($"Within its allowance over the last 3 months ({(pct >= 0 ? "+" : "")}{Percent(pct)})", MnlTone.Success)
                        : (income ? pct < 0 : pct > 0)
                            ? ($"{(pct > 0 ? "+" : "")}{Percent(pct)} against its allowance over the last 3 months", MnlTone.Warning)
                            : ($"{Percent(pct)} against its allowance over the last 3 months", MnlTone.Info);
                }
            case ProtoLineType.Provision:
                {
                    decimal annual = line.YearPlanned;
                    decimal used = line.ToDateActual;
                    decimal pctUsed = annual == 0 ? 0 : Math.Round(used / annual * 100, 0);
                    decimal pctYear = Math.Round(frozen / 12m * 100, 0);
                    MnlTone tone = used > annual ? MnlTone.Error : pctUsed > pctYear + 15 ? MnlTone.Warning : MnlTone.Success;
                    return ($"{Money(used)} of {Money(annual)} used ({Percent(pctUsed)}), with {Percent(pctYear)} of the year gone", tone);
                }
            default:
                {
                    int month = Array.FindIndex(line.Preview, v => v != 0);
                    if (month < 0)
                    {
                        return ("Nothing planned", MnlTone.Neutral);
                    }

                    if (month < frozen)
                    {
                        decimal actual = line.Actual[month] ?? 0;
                        return ($"{Money(actual)} spent against {Money(line.Planned[month])} in {ProtoBudget.MonthName(month)}",
                            VarianceTone(false, line.Planned[month], actual));
                    }

                    return ($"Planned {Money(line.Preview[month])} in {ProtoBudget.MonthName(month)}", MnlTone.Neutral);
                }
        }
    }

    /// <summary>SVG polyline points for a small trend chart.</summary>
    public static string Points(IEnumerable<decimal?> values, decimal top, int width, int height, int count = 12)
    {
        List<string> points = [];
        int i = 0;
        foreach (decimal? value in values)
        {
            if (value is not null)
            {
                double x = count <= 1 ? 0 : i * (width / (double)(count - 1));
                double y = height - (double)(top == 0 ? 0 : value.Value / top) * height;
                points.Add($"{x.ToString("0.#", CultureInfo.InvariantCulture)},{y.ToString("0.#", CultureInfo.InvariantCulture)}");
            }

            i++;
        }

        return string.Join(' ', points);
    }

    public static string Coord(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
