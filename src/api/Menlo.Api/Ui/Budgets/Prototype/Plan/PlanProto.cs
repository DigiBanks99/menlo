using System.Globalization;

namespace Menlo.Api.Ui.Budgets.Prototype.Plan;

// PROTOTYPE for "Prototype capturing the plan" (#658). Throwaway: an in-memory, single-user plan with
// made-up, generic data, so entering and changing the plan can be tried. Nothing here is the real model.
//
// It follows "Decide how a budget line's plan is stored" (#595): planned amounts are calculated from
// layers (baseline and dated changes, yearly changes, seasonal variations, conditional changes, overrides,
// event amounts) and written on commit from the current month on. Frozen months keep what was stored.

public enum PKind { Rand, Percent }

public sealed class PCategory
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public string? ParentId { get; set; }
    public PCategory Clone() => (PCategory)MemberwiseClone();
}

public sealed record PDated(int From, decimal Amount);

public sealed record PYearly(int Month, decimal Value, PKind Kind);

public sealed record PSeasonal(int[] Months, decimal Value, PKind Kind);

public sealed record PConditional(string TriggerId, decimal Value, PKind Kind);

public sealed class PLine
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required string CategoryId { get; set; }
    public required ProtoLineType Type { get; set; }
    public bool Income { get; set; }
    public string Beneficiary { get; set; } = "household";
    public int ShareA { get; set; } = 50;
    public int RentalShare { get; set; }
    public List<PDated> Dated { get; set; } = [];
    public List<PYearly> Yearly { get; set; } = [];
    public List<PSeasonal> Seasonal { get; set; } = [];
    public List<PConditional> Conditional { get; set; } = [];
    public Dictionary<int, decimal> Overrides { get; set; } = [];
    public Dictionary<int, decimal> Events { get; set; } = [];

    /// <summary>What was committed for each month. Frozen months show this and are never rewritten.</summary>
    public decimal[] Stored { get; set; } = new decimal[12];

    public PLine Clone() => new()
    {
        Id = Id,
        Name = Name,
        CategoryId = CategoryId,
        Type = Type,
        Income = Income,
        Beneficiary = Beneficiary,
        ShareA = ShareA,
        RentalShare = RentalShare,
        Dated = [.. Dated],
        Yearly = [.. Yearly],
        Seasonal = [.. Seasonal],
        Conditional = [.. Conditional],
        Overrides = new(Overrides),
        Events = new(Events),
        Stored = [.. Stored]
    };
}

public sealed class PTrigger
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public int Expected { get; set; }
    public bool Realized { get; set; }
    public PTrigger Clone() => (PTrigger)MemberwiseClone();
}

public sealed class PProposal
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public List<PEdit> Edits { get; set; } = [];
    public bool On { get; set; }
    public PProposal Clone() => new() { Id = Id, Name = Name, Edits = [.. Edits], On = On };
}

/// <summary>One step of one layer, for showing where a month's amount comes from.</summary>
public sealed record PStep(string Layer, string What, decimal Running, int? Starts = null);

/// <summary>A month's planned amount and how it was reached.</summary>
public sealed record PCalc(decimal Amount, decimal Calculated, bool Overridden, IReadOnlyList<PStep> Steps)
{
    /// <summary>Short marks for the layers that touch this month, shown in the cells.</summary>
    public IEnumerable<string> MarksAt(int month) => Steps.Where(s => s.Starts is null || s.Starts == month).Select(s => s.Layer).Distinct()
        .Where(l => l != "Baseline" && l != "Event")
        .Select(l => l switch { "Dated change" => "D", "Yearly change" => "Y", "Seasonal variation" => "S", "Conditional change" => "C", _ => "O" });
}

// ---------------------------------------------------------------------------------------------------
// Edits: everything the owner can change. A direct edit applies at once (a commit); in a proposal it waits.
// ---------------------------------------------------------------------------------------------------

public abstract record PEdit(string LineId)
{
    public abstract void Apply(PYear year);
    public abstract string Describe(PYear year);
    /// <summary>A carried proposal can name a line that year copy dropped; then the edit does nothing.</summary>
    protected static PLine? Find(PYear y, string id) => y.Lines.FirstOrDefault(l => l.Id == id);

    protected static string Line(PYear y, string id) => y.Lines.FirstOrDefault(l => l.Id == id)?.Name ?? id;
}

public sealed record DatedEdit(string LineId, int From, decimal Amount) : PEdit(LineId)
{
    public override void Apply(PYear y)
    {
        if (Find(y, LineId) is not PLine line) return;
        line.Dated = [.. line.Dated.Where(d => d.From != From), new PDated(From, Amount)];
        line.Dated.Sort((a, b) => a.From.CompareTo(b.From));
    }

    public override string Describe(PYear y) => $"{Line(y, LineId)}: {PFormat.Money(Amount)} a month from {PFormat.Month(From)}";
}

public sealed record YearlyEdit(string LineId, int Month, decimal Value, PKind Kind) : PEdit(LineId)
{
    public override void Apply(PYear y) => Find(y, LineId)?.Yearly.Add(new PYearly(Month, Value, Kind));
    public override string Describe(PYear y) => $"{Line(y, LineId)}: {PFormat.Change(Value, Kind)} every {PFormat.MonthLong(Month)}";
}

public sealed record SeasonalEdit(string LineId, int[] Months, decimal Value, PKind Kind) : PEdit(LineId)
{
    public override void Apply(PYear y) => Find(y, LineId)?.Seasonal.Add(new PSeasonal(Months, Value, Kind));
    public override string Describe(PYear y) => $"{Line(y, LineId)}: {PFormat.Change(Value, Kind)} in {PFormat.Months(Months)}";
}

public sealed record ConditionalEdit(string LineId, string TriggerId, decimal Value, PKind Kind) : PEdit(LineId)
{
    public override void Apply(PYear y) => Find(y, LineId)?.Conditional.Add(new PConditional(TriggerId, Value, Kind));
    public override string Describe(PYear y) =>
        $"{Line(y, LineId)}: {PFormat.Change(Value, Kind)} once \"{y.Triggers.FirstOrDefault(t => t.Id == TriggerId)?.Name}\"";
}

public sealed record OverrideEdit(string LineId, int Month, decimal? Amount) : PEdit(LineId)
{
    public override void Apply(PYear y)
    {
        if (Find(y, LineId) is not PLine l) return;
        if (Amount is null) l.Overrides.Remove(Month); else l.Overrides[Month] = Amount.Value;
    }

    public override string Describe(PYear y) => Amount is null
        ? $"{Line(y, LineId)}: remove the {PFormat.Month(Month)} override"
        : $"{Line(y, LineId)}: override {PFormat.Month(Month)} with {PFormat.Money(Amount.Value)}";
}

public sealed record EventEdit(string LineId, int Month, decimal Amount) : PEdit(LineId)
{
    public override void Apply(PYear y)
    {
        if (Find(y, LineId) is not PLine l) return;
        if (Amount == 0) l.Events.Remove(Month); else l.Events[Month] = Amount;
    }

    public override string Describe(PYear y) => $"{Line(y, LineId)}: {PFormat.Money(Amount)} in {PFormat.Month(Month)}";
}

public sealed record NewLineEdit(string LineId, string Name, string CategoryId, ProtoLineType Type, bool Income, decimal Amount, int From)
    : PEdit(LineId)
{
    public override void Apply(PYear y)
    {
        if (y.Lines.Any(l => l.Id == LineId)) return;
        PLine line = new() { Id = LineId, Name = Name, CategoryId = CategoryId, Type = Type, Income = Income };
        if (Type == ProtoLineType.Event) line.Events[From] = Amount; else line.Dated.Add(new PDated(From, Amount));
        y.Lines.Add(line);
    }

    public override string Describe(PYear y) => Type == ProtoLineType.Event
        ? $"New event line \"{Name}\": {PFormat.Money(Amount)} in {PFormat.Month(From)}"
        : $"New {PFormat.Type(Type).ToLowerInvariant()} line \"{Name}\": {PFormat.Money(Amount)} a month from {PFormat.Month(From)}";
}

public sealed record RemoveLayerEdit(string LineId, string Layer, int Index) : PEdit(LineId)
{
    public override void Apply(PYear y)
    {
        if (Find(y, LineId) is not PLine l) return;
        switch (Layer)
        {
            case "dated" when Index < l.Dated.Count && l.Dated[Index].From >= y.Current: l.Dated.RemoveAt(Index); break;
            case "yearly" when Index < l.Yearly.Count: l.Yearly.RemoveAt(Index); break;
            case "seasonal" when Index < l.Seasonal.Count: l.Seasonal.RemoveAt(Index); break;
            case "conditional" when Index < l.Conditional.Count: l.Conditional.RemoveAt(Index); break;
        }
    }

    public override string Describe(PYear y) => $"{Line(y, LineId)}: remove a {Layer} change";
}

// ---------------------------------------------------------------------------------------------------
// A budget year
// ---------------------------------------------------------------------------------------------------

public sealed class PYear
{
    public required int Year { get; init; }

    /// <summary>The current month. Months before it are frozen.</summary>
    public required int Current { get; init; }

    public List<PCategory> Categories { get; set; } = [];
    public List<PLine> Lines { get; set; } = [];
    public List<PTrigger> Triggers { get; set; } = [];
    public List<PProposal> Proposals { get; set; } = [];

    /// <summary>Categories looked at during set-up (variant C's checklist).</summary>
    public HashSet<string> Reviewed { get; set; } = [];

    public PLine Line(string id) => Lines.First(l => l.Id == id);

    public PYear Clone() => new()
    {
        Year = Year,
        Current = Current,
        Categories = [.. Categories.Select(c => c.Clone())],
        Lines = [.. Lines.Select(l => l.Clone())],
        Triggers = [.. Triggers.Select(t => t.Clone())],
        Proposals = [.. Proposals.Select(p => p.Clone())],
        Reviewed = [.. Reviewed]
    };

    public IEnumerable<PCategory> Tops => Categories.Where(c => c.ParentId is null);

    public IEnumerable<PCategory> Children(string id) => Categories.Where(c => c.ParentId == id);

    public string Path(string categoryId)
    {
        PCategory c = Categories.First(x => x.Id == categoryId);
        return c.ParentId is null ? c.Name : $"{Categories.First(x => x.Id == c.ParentId).Name} › {c.Name}";
    }

    /// <summary>Running baseline at a month: dated changes set it, yearly changes move it.</summary>
    public decimal BaselineAt(PLine line, int month, List<PStep>? steps = null)
    {
        decimal value = 0;
        for (int m = 0; m <= month; m++)
        {
            foreach (PDated d in line.Dated.Where(d => d.From == m))
            {
                value = d.Amount;
                steps?.Add(new PStep(m == 0 && d == line.Dated[0] ? "Baseline" : "Dated change",
                    m == 0 ? $"{PFormat.Money(d.Amount)} a month" : $"{PFormat.Money(d.Amount)} a month from {PFormat.Month(m)}", value, m));
            }

            foreach (PYearly yc in line.Yearly.Where(x => x.Month == m))
            {
                value = yc.Kind == PKind.Percent ? Math.Round(value * (1 + yc.Value / 100), 0) : value + yc.Value;
                steps?.Add(new PStep("Yearly change", $"{PFormat.Change(yc.Value, yc.Kind)} every {PFormat.MonthLong(m)}", value, m));
            }
        }

        return value;
    }

    public PCalc Calc(PLine line, int month)
    {
        List<PStep> steps = [];
        decimal value;
        if (line.Type == ProtoLineType.Event)
        {
            value = line.Events.GetValueOrDefault(month);
            if (value != 0) steps.Add(new PStep("Event", $"{PFormat.Money(value)} in {PFormat.Month(month)}", value));
        }
        else
        {
            decimal baseline = BaselineAt(line, month, steps);
            value = baseline;
            foreach (PSeasonal s in line.Seasonal.Where(s => s.Months.Contains(month)))
            {
                value += s.Kind == PKind.Percent ? Math.Round(baseline * s.Value / 100, 0) : s.Value;
                steps.Add(new PStep("Seasonal variation", $"{PFormat.Change(s.Value, s.Kind)} in {PFormat.Months(s.Months)}", value));
            }

            foreach (PConditional c in line.Conditional)
            {
                PTrigger? t = Triggers.FirstOrDefault(x => x.Id == c.TriggerId);
                if (t is null || t.Expected > month) continue;
                value += c.Kind == PKind.Percent ? Math.Round(baseline * c.Value / 100, 0) : c.Value;
                steps.Add(new PStep("Conditional change",
                    $"{PFormat.Change(c.Value, c.Kind)} from \"{t.Name}\" ({(t.Realized ? "realized" : "expected")} {PFormat.Month(t.Expected)})", value));
            }
        }

        if (line.Overrides.TryGetValue(month, out decimal o))
        {
            steps.Add(new PStep("Override", $"{PFormat.Money(o)} typed over {PFormat.Money(value)}", o));
            return new PCalc(o, value, true, steps);
        }

        return new PCalc(value, value, false, steps);
    }

    /// <summary>The planned amount: what was stored for a frozen month, the calculation from the current month on.</summary>
    public decimal Planned(PLine line, int month) => month < Current ? line.Stored[month] : Calc(line, month).Amount;

    /// <summary>A commit writes the calculated amounts from the current month on. Frozen months are left alone.</summary>
    public void Write()
    {
        foreach (PLine line in Lines)
        {
            for (int m = Current; m < 12; m++)
            {
                line.Stored[m] = Calc(line, m).Amount;
            }
        }
    }

    public decimal Net(int month) => Lines.Sum(l => (l.Income ? 1 : -1) * Planned(l, month));

    /// <summary>This year with the given proposals applied on top, for previewing.</summary>
    public PYear With(IEnumerable<PProposal> proposals)
    {
        PYear copy = Clone();
        foreach (PEdit edit in proposals.SelectMany(p => p.Edits))
        {
            edit.Apply(copy);
        }

        return copy;
    }

    /// <summary>The line and month cells a proposal changes.</summary>
    public HashSet<(string Line, int Month)> Touches(PProposal proposal)
    {
        PYear after = With([proposal]);
        HashSet<(string, int)> cells = [];
        foreach (PLine line in after.Lines)
        {
            PLine? before = Lines.FirstOrDefault(l => l.Id == line.Id);
            for (int m = Current; m < 12; m++)
            {
                if (before is null ? after.Calc(line, m).Amount != 0 : after.Calc(line, m).Amount != Calc(before, m).Amount)
                {
                    cells.Add((line.Id, m));
                }
            }
        }

        return cells;
    }

    /// <summary>Why a proposal can't be switched on alongside the ones already on, if it can't.</summary>
    public string? Conflict(PProposal proposal)
    {
        HashSet<(string Line, int Month)> mine = Touches(proposal);
        foreach (PProposal other in Proposals.Where(p => p.On && p != proposal))
        {
            (string Line, int Month) shared = Touches(other).FirstOrDefault(mine.Contains);
            if (shared != default)
            {
                return $"Can't be on with \"{other.Name}\": both change {Lines.FirstOrDefault(l => l.Id == shared.Line)?.Name ?? "a new line"} in {PFormat.MonthLong(shared.Month)}.";
            }
        }

        return null;
    }
}

// ---------------------------------------------------------------------------------------------------
// The store: one household, one owner, in memory. Reset puts the made-up data back.
// ---------------------------------------------------------------------------------------------------

public static class PlanStore
{
    public const int FirstYear = 2026;

    private static readonly Lock Gate = new();

    public static Dictionary<int, PYear> Years { get; private set; } = Seed();

    public static PYear Get(int year)
    {
        lock (Gate)
        {
            return Years.TryGetValue(year, out PYear? y) ? y : Years[FirstYear];
        }
    }

    public static void Change(Action action)
    {
        lock (Gate)
        {
            action();
        }
    }

    public static void Reset()
    {
        lock (Gate)
        {
            Years = Seed();
        }
    }

    private static Dictionary<int, PYear> Seed()
    {
        PYear y = new() { Year = FirstYear, Current = 6 };
        void Cat(string id, string name, string? parent = null) => y.Categories.Add(new PCategory { Id = id, Name = name, ParentId = parent });

        Cat("income", "Income");
        Cat("salaries", "Salaries", "income");
        Cat("rental", "Rental", "income");
        Cat("home", "Home");
        Cat("utilities", "Utilities", "home");
        Cat("maintenance", "Maintenance", "home");
        Cat("transport", "Transport");
        Cat("running", "Running costs", "transport");
        Cat("food", "Food");
        Cat("groceries-cat", "Household food", "food");
        Cat("children", "Children");
        Cat("school", "School", "children");
        Cat("celebrations", "Celebrations", "children");
        Cat("leisure", "Leisure");
        Cat("holidays", "Holidays", "leisure");
        Cat("hobbies", "Hobbies", "leisure");

        y.Triggers.Add(new PTrigger { Id = "tenant", Name = "Tenant moves in", Expected = 8 });
        y.Triggers.Add(new PTrigger { Id = "new-car", Name = "Second car bought", Expected = 9 });

        PLine L(string id, string name, string cat, ProtoLineType type, decimal baseline, bool income = false, string who = "household", int shareA = 50)
        {
            PLine line = new() { Id = id, Name = name, CategoryId = cat, Type = type, Income = income, Beneficiary = who, ShareA = shareA };
            if (type != ProtoLineType.Event) line.Dated.Add(new PDated(0, baseline));
            y.Lines.Add(line);
            return line;
        }

        L("salary-a", "Salary (Member A)", "salaries", ProtoLineType.Fixed, 33_000, income: true, who: "a", shareA: 100);
        L("salary-b", "Salary (Member B)", "salaries", ProtoLineType.Fixed, 27_500, income: true, who: "b", shareA: 0)
            .Dated.Add(new PDated(3, 28_600));
        L("rent", "Rent", "rental", ProtoLineType.Fixed, 0, income: true, shareA: 100)
            .Conditional.Add(new PConditional("tenant", 6_000, PKind.Rand));
        L("recovery", "Utility recovery", "rental", ProtoLineType.Variable, 0, income: true, shareA: 100)
            .Conditional.Add(new PConditional("tenant", 700, PKind.Rand));

        PLine electricity = L("electricity", "Electricity", "utilities", ProtoLineType.Variable, 1_800, shareA: 60);
        electricity.Yearly.Add(new PYearly(6, 12, PKind.Percent));
        electricity.Seasonal.Add(new PSeasonal([5, 6, 7], 400, PKind.Rand));
        electricity.Conditional.Add(new PConditional("tenant", 15, PKind.Percent));
        electricity.RentalShare = 20;
        L("water", "Water", "utilities", ProtoLineType.Variable, 700, shareA: 0).RentalShare = 20;
        PLine rates = L("rates", "Rates and levies", "utilities", ProtoLineType.Fixed, 2_500, shareA: 100);
        rates.Yearly.Add(new PYearly(5, 6, PKind.Percent));
        rates.RentalShare = 15;
        PLine security = L("security", "Security service", "utilities", ProtoLineType.Fixed, 650);
        security.Yearly.Add(new PYearly(2, 30, PKind.Rand));
        security.Yearly.Add(new PYearly(8, 30, PKind.Rand));

        L("maintenance", "Home maintenance", "maintenance", ProtoLineType.Provision, 1_000);
        L("geyser", "Geyser service", "maintenance", ProtoLineType.Event, 0, shareA: 100).Events[9] = 1_500;

        L("fuel", "Fuel", "running", ProtoLineType.Variable, 3_000, who: "a", shareA: 100)
            .Conditional.Add(new PConditional("new-car", 1_800, PKind.Rand));
        L("insurance", "Car insurance", "running", ProtoLineType.Fixed, 1_200, shareA: 0)
            .Conditional.Add(new PConditional("new-car", 950, PKind.Rand));
        L("car-service", "Car service", "running", ProtoLineType.Event, 0, shareA: 100).Events[2] = 4_000;

        L("groceries", "Groceries", "groceries-cat", ProtoLineType.Variable, 6_000).Overrides[11] = 8_000;

        L("school-fees", "School fees", "school", ProtoLineType.Fixed, 3_500, who: "child", shareA: 0);
        L("party", "Birthday party", "celebrations", ProtoLineType.Event, 0, who: "child", shareA: 100).Events[8] = 5_000;

        // Everything typed so far is committed: store it for every month, frozen ones included
        for (int m = 0; m < 12; m++)
        {
            foreach (PLine line in y.Lines)
            {
                line.Stored[m] = y.Calc(line, m).Amount;
            }
        }

        y.Proposals.Add(new PProposal
        {
            Id = "holiday",
            Name = "Holiday in December",
            Edits = [new NewLineEdit("holiday", "Family holiday", "holidays", ProtoLineType.Event, false, 15_000, 11)]
        });
        y.Proposals.Add(new PProposal { Id = "party-smaller", Name = "Party without the jumping castle", Edits = [new EventEdit("party", 8, 3_500)] });
        y.Proposals.Add(new PProposal { Id = "party-bigger", Name = "Bigger party", Edits = [new EventEdit("party", 8, 7_000)] });
        y.Proposals.Add(new PProposal { Id = "fuel-up", Name = "Raise the fuel allowance", Edits = [new DatedEdit("fuel", 7, 3_300)] });

        return new Dictionary<int, PYear> { [FirstYear] = y };
    }

    /// <summary>
    /// Year copy: the new baseline is December's with every dated and yearly change applied; seasonal
    /// variations, yearly changes, triggers not yet realized and proposals carry; a realized trigger folds into
    /// the baseline; each event line is kept or dropped; actuals and overrides stay behind. Planned from the start.
    /// </summary>
    public static PYear Copy(PYear from, IReadOnlySet<string> realized, IReadOnlySet<string> keepEvents)
    {
        PYear to = new() { Year = from.Year + 1, Current = 0, Categories = [.. from.Categories.Select(c => c.Clone())] };
        foreach (PTrigger t in from.Triggers.Where(t => !realized.Contains(t.Id)))
        {
            PTrigger c = t.Clone();
            c.Realized = false;
            c.Expected = t.Expected < from.Current ? 0 : t.Expected;
            to.Triggers.Add(c);
        }

        foreach (PLine line in from.Lines)
        {
            if (line.Type == ProtoLineType.Event && !keepEvents.Contains(line.Id))
            {
                continue;
            }

            PLine n = line.Clone();
            n.Overrides = [];
            n.Stored = new decimal[12];
            if (line.Type != ProtoLineType.Event)
            {
                decimal baseline = from.BaselineAt(line, 11);
                decimal folded = baseline;
                foreach (PConditional c in line.Conditional.Where(c => realized.Contains(c.TriggerId)))
                {
                    folded += c.Kind == PKind.Percent ? Math.Round(baseline * c.Value / 100, 0) : c.Value;
                }

                // The new baseline already holds this year's yearly changes; they're planned again in the new year
                n.Dated = [new PDated(0, folded)];
                n.Conditional = [.. line.Conditional.Where(c => !realized.Contains(c.TriggerId))];
            }

            to.Lines.Add(n);
        }

        to.Proposals = [.. from.Proposals.Select(p => { PProposal c = p.Clone(); c.On = false; return c; })];
        to.Write();
        return to;
    }
}

public static class PFormat
{
    private static readonly NumberFormatInfo Numbers = new() { NumberGroupSeparator = " ", NumberGroupSizes = [3] };

    public static string Money(decimal value) => (value < 0 ? "−" : "") + "R " + Math.Abs(value).ToString("#,0", Numbers);

    public static string Change(decimal value, PKind kind) =>
        (value >= 0 ? "+" : "−") + (kind == PKind.Percent ? Math.Abs(value).ToString("0.#", CultureInfo.InvariantCulture) + "%" : Money(Math.Abs(value)));

    public static string Month(int month) => new DateTime(2000, month + 1, 1).ToString("MMM", CultureInfo.InvariantCulture);

    public static string MonthLong(int month) => new DateTime(2000, month + 1, 1).ToString("MMMM", CultureInfo.InvariantCulture);

    public static string Months(IEnumerable<int> months)
    {
        int[] m = [.. months.Order()];
        bool run = m.Length > 2 && m[^1] - m[0] == m.Length - 1;
        return run ? $"{Month(m[0])} to {Month(m[^1])}" : string.Join(", ", m.Select(Month));
    }

    public static string Type(ProtoLineType type) => type switch
    {
        ProtoLineType.Fixed => "Fixed",
        ProtoLineType.Variable => "Variable",
        ProtoLineType.Provision => "Provision",
        _ => "Event"
    };

    public static string State(PYear y, int month) => month < y.Current ? "frozen" : month == y.Current ? "current" : "future";

    public static string Who(string key) => key switch { "a" => "Member A", "b" => "Member B", "child" => "Child", _ => "Household" };
}
