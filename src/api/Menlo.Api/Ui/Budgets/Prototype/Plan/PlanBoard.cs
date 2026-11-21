using Menlo.Ui.Components;

namespace Menlo.Api.Ui.Budgets.Prototype.Plan;

// PROTOTYPE (#658): the views prototype's (#596) variant D grid, built from the editable plan instead of fixed
// data. Planned is what's committed; the preview adds the proposals that are on and the one being worked on.

/// <summary>A line as one view sees it.</summary>
public sealed record BLine(PLine Line, decimal[] Planned, decimal[] Preview, decimal?[] Actual, bool New, int Current)
{
    public int Sign => Line.Income ? 1 : -1;

    public decimal YearPlanned => Planned.Sum();

    public decimal YearPreview => Preview.Sum();

    public decimal ToDatePlanned => Planned.Take(Current).Sum();

    public decimal ToDateActual => Actual.Take(Current).Sum(a => a ?? 0);

    public bool AnyChanged => New || Enumerable.Range(0, 12).Any(m => Preview[m] != Planned[m]);

    public bool OverPlan => Line.Income ? ToDateActual < ToDatePlanned * 0.98m : ToDateActual > ToDatePlanned * 1.02m;

    public decimal PlannedFor(int month) => month == PQuery.YearPeriod ? YearPlanned : Planned[month];

    public decimal PreviewFor(int month) => month == PQuery.YearPeriod ? YearPreview : Preview[month];

    public decimal? ActualFor(int month) => month == PQuery.YearPeriod
        ? Actual.All(a => a is null) ? null : Actual.Sum(a => a ?? 0)
        : Actual[month];
}

public sealed record BCategory(PCategory Category, IReadOnlyList<BLine> Lines)
{
    public decimal PlannedFor(int month) => Lines.Sum(l => l.PlannedFor(month));

    public decimal PreviewFor(int month) => Lines.Sum(l => l.PreviewFor(month));

    public decimal? ActualFor(int month) => Lines.All(l => l.ActualFor(month) is null) ? null : Lines.Sum(l => l.ActualFor(month) ?? 0);

    public bool AnyChanged => Lines.Any(l => l.AnyChanged);
}

public sealed record BGroup(PCategory Top, IReadOnlyList<BCategory> Categories)
{
    public bool Income => Top.Id == "income";

    public IEnumerable<BLine> Lines => Categories.SelectMany(c => c.Lines);

    public decimal PlannedFor(int month) => Lines.Sum(l => l.PlannedFor(month));

    public decimal PreviewFor(int month) => Lines.Sum(l => l.PreviewFor(month));

    public decimal? ActualFor(int month) => Lines.All(l => l.ActualFor(month) is null) ? null : Lines.Sum(l => l.ActualFor(month) ?? 0);

    public bool AnyChanged => Categories.Any(c => c.AnyChanged);
}

public sealed record BViewNet(string Key, string Label, decimal Planned, decimal Preview, decimal? Actual);

public sealed class PBoard
{
    public required IReadOnlyList<BGroup> Groups { get; init; }

    public required decimal[] NetPlanned { get; init; }

    public required decimal[] NetPreview { get; init; }

    public required decimal?[] NetActual { get; init; }

    public required IReadOnlyList<BViewNet> Views { get; init; }

    /// <summary>Groups and categories shown open: the ones in the URL, and the ones holding what's being edited.</summary>
    public required IReadOnlySet<string> Open { get; init; }

    public required int Current { get; init; }

    public decimal NetPlannedFor(int month) => month == PQuery.YearPeriod ? NetPlanned.Sum() : NetPlanned[month];

    public decimal NetPreviewFor(int month) => month == PQuery.YearPeriod ? NetPreview.Sum() : NetPreview[month];

    public decimal? NetActualFor(int month) => month == PQuery.YearPeriod
        ? NetActual.All(a => a is null) ? null : NetActual.Sum(a => a ?? 0)
        : NetActual[month];

    public bool IsOpen(string key) => Open.Contains(key);

    public static PBoard Build(PSheet sheet)
    {
        PQuery q = sheet.Query;
        PYear plan = sheet.Plan;
        PYear pv = sheet.Preview;

        List<BLine> all = Lines(plan, pv, q.View);
        (decimal[] netPlanned, decimal[] netPreview, decimal?[] netActual) = Net(all, plan.Current);

        IEnumerable<BLine> shown = all.Where(l => q.Who == "all" || l.Line.Beneficiary == q.Who);
        if (q.Filter == "over")
        {
            shown = shown.Where(l => l.OverPlan);
        }

        List<BLine> shownList = [.. shown];
        List<BGroup> groups = [];
        foreach (PCategory top in pv.Tops)
        {
            List<BCategory> categories = [];
            foreach (PCategory child in pv.Children(top.Id))
            {
                List<BLine> lines = [.. shownList.Where(l => l.Line.CategoryId == child.Id)];
                bool empty = pv.Lines.All(l => l.CategoryId != child.Id);
                bool keep = q.Filter switch
                {
                    "with" => lines.Count > 0,
                    "without" => empty,
                    "over" => lines.Count > 0,
                    _ => lines.Count > 0 || (empty && q.Who == "all")
                };
                if (keep)
                {
                    categories.Add(new BCategory(child, lines));
                }
            }

            if (categories.Count > 0 || (q.Filter is "all" or "without" && q.Who == "all" && !pv.Children(top.Id).Any()))
            {
                groups.Add(new BGroup(top, categories));
            }
        }

        HashSet<string> open = [.. q.Opened];
        void OpenFor(string categoryId)
        {
            PCategory? c = pv.Categories.FirstOrDefault(x => x.Id == categoryId);
            if (c is null) return;
            open.Add("g:" + (c.ParentId ?? c.Id));
            if (c.ParentId is not null) open.Add("c:" + c.Id);
        }

        if (sheet.Line is PLine selected) OpenFor(selected.CategoryId);
        if (q.Category is string cat) OpenFor(cat);
        if (q.Edit?.StartsWith("cat:", StringComparison.Ordinal) == true) OpenFor(q.Edit[4..]);

        List<BViewNet> views = [];
        foreach ((string key, string label) in PQuery.Views)
        {
            (decimal[] p, decimal[] v, decimal?[] a) = Net(Lines(plan, pv, key), plan.Current);
            int m = q.Period;
            views.Add(new BViewNet(key, label,
                m == PQuery.YearPeriod ? p.Sum() : p[m],
                m == PQuery.YearPeriod ? v.Sum() : v[m],
                m == PQuery.YearPeriod ? (a.All(x => x is null) ? null : a.Sum(x => x ?? 0)) : a[m]));
        }

        return new PBoard
        {
            Groups = groups,
            NetPlanned = netPlanned,
            NetPreview = netPreview,
            NetActual = netActual,
            Views = views,
            Open = open,
            Current = plan.Current
        };
    }

    public static decimal Factor(PYear y, PLine line, string view) => view switch
    {
        "a" => line.ShareA / 100m,
        "b" => (100 - line.ShareA) / 100m,
        "rental" => y.IsRentalIncome(line) ? 1 : line.Income || y.IsPreTax(line) ? 0 : line.RentalShare / 100m,
        _ => 1
    };

    private static List<BLine> Lines(PYear plan, PYear pv, string view)
    {
        List<BLine> result = [];
        foreach (PLine line in pv.Lines)
        {
            decimal factor = Factor(pv, line, view);
            if (factor == 0)
            {
                continue;
            }

            PLine? before = plan.Lines.FirstOrDefault(l => l.Id == line.Id);
            decimal[] planned = [.. Enumerable.Range(0, 12).Select(m => before is null ? 0 : Math.Round(plan.Planned(before, m) * factor, 0))];
            decimal[] preview = [.. Enumerable.Range(0, 12).Select(m => Math.Round(pv.Planned(line, m) * factor, 0))];
            decimal?[] actual = [.. line.Actual.Select(a => a is null ? (decimal?)null : Math.Round(a.Value * factor, 0))];
            result.Add(new BLine(line, planned, preview, actual, before is null, plan.Current));
        }

        return result;
    }

    private static (decimal[], decimal[], decimal?[]) Net(IReadOnlyList<BLine> lines, int current)
    {
        decimal[] planned = new decimal[12];
        decimal[] preview = new decimal[12];
        decimal?[] actual = new decimal?[12];
        for (int m = 0; m < 12; m++)
        {
            planned[m] = lines.Sum(l => l.Sign * l.Planned[m]);
            preview[m] = lines.Sum(l => l.Sign * l.Preview[m]);
            actual[m] = m > current || lines.All(l => l.Actual[m] is null) ? null : lines.Sum(l => l.Sign * (l.Actual[m] ?? 0));
        }

        return (planned, preview, actual);
    }

    public static string MonthState(int current, int month) =>
        month < current ? "frozen" : month == current ? "current" : "future";

    /// <summary>How the line is doing, judged the way its line type says (from the views prototype).</summary>
    public static (string Text, MnlTone Tone) Judge(BLine line)
    {
        int frozen = line.Current;
        if (frozen == 0)
        {
            return ("No actuals yet", MnlTone.Neutral);
        }

        switch (line.Line.Type)
        {
            case ProtoLineType.Fixed:
                {
                    int off = Enumerable.Range(0, frozen).Count(m => ProtoFormat.VarianceTone(line.Line.Income, line.Planned[m], line.Actual[m]) != MnlTone.Success
                                                                   && !(line.Planned[m] == 0 && (line.Actual[m] ?? 0) == 0));
                    return off == 0
                        ? ("Matched the plan every month", MnlTone.Success)
                        : ($"Off plan in {off} of {frozen} months ({ProtoFormat.Delta(line.ToDateActual - line.ToDatePlanned)} to date)", MnlTone.Warning);
                }
            case ProtoLineType.Variable:
                {
                    int from = Math.Max(0, frozen - 3);
                    decimal planned = line.Planned.Skip(from).Take(frozen - from).Sum();
                    decimal actual = line.Actual.Skip(from).Take(frozen - from).Sum(a => a ?? 0);
                    if (planned == 0)
                    {
                        return ("No allowance yet", MnlTone.Neutral);
                    }

                    decimal pct = Math.Round((actual - planned) / planned * 100, 0);
                    bool income = line.Line.Income;
                    return Math.Abs(pct) <= 5
                        ? ($"Within its allowance over the last 3 months ({(pct >= 0 ? "+" : "")}{ProtoFormat.Percent(pct)})", MnlTone.Success)
                        : (income ? pct < 0 : pct > 0)
                            ? ($"{(pct > 0 ? "+" : "")}{ProtoFormat.Percent(pct)} against its allowance over the last 3 months", MnlTone.Warning)
                            : ($"{ProtoFormat.Percent(pct)} against its allowance over the last 3 months", MnlTone.Info);
                }
            case ProtoLineType.Provision:
                {
                    decimal annual = line.YearPreview;
                    decimal used = line.ToDateActual;
                    decimal pctUsed = annual == 0 ? 0 : Math.Round(used / annual * 100, 0);
                    decimal pctYear = Math.Round(frozen / 12m * 100, 0);
                    MnlTone tone = used > annual ? MnlTone.Error : pctUsed > pctYear + 15 ? MnlTone.Warning : MnlTone.Success;
                    return ($"{ProtoFormat.Money(used)} of {ProtoFormat.Money(annual)} used ({ProtoFormat.Percent(pctUsed)}), with {ProtoFormat.Percent(pctYear)} of the year gone", tone);
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
                        return ($"{ProtoFormat.Money(actual)} spent against {ProtoFormat.Money(line.Planned[month])} in {PFormat.Month(month)}",
                            ProtoFormat.VarianceTone(false, line.Planned[month], actual));
                    }

                    return ($"Planned {ProtoFormat.Money(line.Preview[month])} in {PFormat.Month(month)}", MnlTone.Neutral);
                }
        }
    }
}
