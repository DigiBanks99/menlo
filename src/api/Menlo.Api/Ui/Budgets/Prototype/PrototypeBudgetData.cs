namespace Menlo.Api.Ui.Budgets.Prototype;

// PROTOTYPE for "Prototype the budget views" (#596). Throwaway: made-up, generic data held in memory.
// Nothing here is the real model; it exists to judge what the views should look like.

public enum ProtoLineType { Fixed, Variable, Provision, Event }

public sealed record ProtoLine
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Group { get; init; }
    public required string Category { get; init; }
    public required ProtoLineType Type { get; init; }
    public bool Income { get; init; }
    public bool PreTax { get; init; }
    public string Beneficiary { get; init; } = "household";
    public required decimal[] Planned { get; init; }
    public required decimal?[] Actual { get; init; }
    public decimal ShareA { get; init; }
    public decimal ShareB { get; init; }
    public decimal RentalShare { get; init; }
    public bool RentalIncome { get; init; }
    public int? OverrideMonth { get; init; }
    public decimal? OverrideCalculated { get; init; }
    public string? Story { get; init; }
}

public sealed record ProtoChange(string LineId, int Month, decimal Amount);

public sealed record ProtoProposal(string Id, string Name, string Summary, IReadOnlyList<ProtoChange> Changes, ProtoLine? NewLine = null);

public static class ProtoBudget
{
    public const int Year = 2026;

    /// <summary>The prototype's "today" is fixed in July, so Jan to Jun are frozen.</summary>
    public const int CurrentMonth = 6;

    public static readonly string[] Groups = ["Income", "Pre-tax deductions", "Home", "Transport", "Food", "Children", "Leisure"];

    // Categories with no lines still show up (for the "without lines" filter)
    public static readonly (string Group, string Category)[] EmptyCategories = [("Leisure", "Hobbies")];

    public static readonly (string Key, string Label)[] Views =
    [
        ("household", "Household"), ("a", "Member A"), ("b", "Member B"), ("rental", "Rental unit")
    ];

    public static readonly (string Key, string Label)[] Beneficiaries =
    [
        ("all", "Everyone"), ("household", "Household"), ("a", "Member A"), ("b", "Member B"), ("child", "Child")
    ];

    public static IReadOnlyList<ProtoLine> Lines { get; } = BuildLines();

    public static IReadOnlyList<ProtoProposal> Proposals { get; } =
    [
        new("holiday", "Holiday in December", "A new event line: R15 000 in December, shared equally.", [],
            new ProtoLine
            {
                Id = "holiday", Name = "Holiday", Group = "Leisure", Category = "Holidays", Type = ProtoLineType.Event,
                Planned = Only(11, 15_000), Actual = Future(), ShareA = 0.5m, ShareB = 0.5m, Story = "From the proposal"
            }),
        new("party-smaller", "Party without the jumping castle", "Birthday party in September drops from R5 000 to R3 500.",
            [new("party", 8, 3_500)]),
        new("party-bigger", "Bigger party", "Birthday party in September goes up from R5 000 to R7 000.",
            [new("party", 8, 7_000)]),
        new("fuel-up", "Raise the fuel allowance", "Fuel goes from R3 000 to R3 300 a month from August.",
            [.. Enumerable.Range(7, 5).Select(m => new ProtoChange("fuel", m, 3_300))])
    ];

    public static string MonthName(int month) =>
        new DateTime(Year, month + 1, 1).ToString("MMM", System.Globalization.CultureInfo.InvariantCulture);

    public static string MonthLong(int month) =>
        new DateTime(Year, month + 1, 1).ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture);

    private static IReadOnlyList<ProtoLine> BuildLines()
    {
        decimal[] electricity = [1_800, 1_800, 1_800, 1_800, 1_800, 2_380, 2_380, 2_380, 1_980, 1_980, 1_980, 1_980];
        decimal[] rates = [2_500, 2_500, 2_500, 2_500, 2_500, 2_650, 2_650, 2_650, 2_650, 2_650, 2_650, 2_650];
        decimal[] groceries = [.. Flat(6_000)[..11], 8_000];
        decimal[] rent = [0, 0, 0, 0, 6_000, 6_000, 6_000, 6_000, 6_000, 6_000, 6_000, 6_000];
        decimal[] recovery = [0, 0, 0, 0, 700, 700, 700, 700, 700, 700, 700, 700];

        return
        [
            // Income
            new ProtoLine
            {
                Id = "salary-a", Name = "Salary (Member A)", Group = "Income", Category = "Salaries", Type = ProtoLineType.Fixed,
                Income = true, Beneficiary = "a", Planned = Flat(33_000), Actual = Paid(Flat(33_000), currentPaid: false), ShareA = 1
            },
            new ProtoLine
            {
                Id = "salary-b", Name = "Salary (Member B)", Group = "Income", Category = "Salaries", Type = ProtoLineType.Fixed,
                Income = true, Beneficiary = "b", Planned = Flat(27_500), Actual = Paid(Flat(27_500), currentPaid: false), ShareB = 1
            },
            new ProtoLine
            {
                Id = "rent", Name = "Rent", Group = "Income", Category = "Rental", Type = ProtoLineType.Fixed, Income = true,
                Planned = rent, Actual = [0, 0, 0, 0, 0, 6_000, 6_000, null, null, null, null, null], ShareA = 1, RentalIncome = true,
                Story = "Trigger \"Tenant moves in\", expected May. The tenant moved in in June."
            },
            new ProtoLine
            {
                Id = "recovery", Name = "Utility recovery", Group = "Income", Category = "Rental", Type = ProtoLineType.Variable, Income = true,
                Planned = recovery, Actual = [0, 0, 0, 0, 0, 640, 0, null, null, null, null, null], ShareA = 1, RentalIncome = true,
                Story = "Trigger \"Tenant moves in\". Recorded net, as received."
            },

            // Pre-tax deductions
            new ProtoLine
            {
                Id = "pension-a", Name = "Pension (Member A)", Group = "Pre-tax deductions", Category = "Retirement", Type = ProtoLineType.Fixed,
                PreTax = true, Beneficiary = "a", Planned = Flat(3_000), Actual = Paid(Flat(3_000), currentPaid: false), ShareA = 1
            },
            new ProtoLine
            {
                Id = "pension-b", Name = "Pension (Member B)", Group = "Pre-tax deductions", Category = "Retirement", Type = ProtoLineType.Fixed,
                PreTax = true, Beneficiary = "b", Planned = Flat(2_500), Actual = Paid(Flat(2_500), currentPaid: false), ShareB = 1
            },

            // Home
            new ProtoLine
            {
                Id = "electricity", Name = "Electricity", Group = "Home", Category = "Utilities", Type = ProtoLineType.Variable,
                Planned = electricity, Actual = Wiggle(electricity, 3), ShareA = 0.6m, ShareB = 0.4m, RentalShare = 0.2m,
                Story = "Yearly change +10% from June; seasonal variation +R400 Jun to Aug."
            },
            new ProtoLine
            {
                Id = "water", Name = "Water", Group = "Home", Category = "Utilities", Type = ProtoLineType.Variable,
                Planned = Flat(700), Actual = Wiggle(Flat(700), 7), ShareB = 1, RentalShare = 0.2m
            },
            new ProtoLine
            {
                Id = "rates", Name = "Rates and levies", Group = "Home", Category = "Utilities", Type = ProtoLineType.Fixed,
                Planned = rates, Actual = Paid(rates, currentPaid: true), ShareA = 1, RentalShare = 0.15m,
                Story = "Yearly change +6% from June."
            },
            new ProtoLine
            {
                Id = "maintenance", Name = "Home maintenance", Group = "Home", Category = "Maintenance", Type = ProtoLineType.Provision,
                Planned = Flat(1_000), Actual = [0, 3_200, 0, 0, 4_500, 0, 0, null, null, null, null, null], ShareA = 0.5m, ShareB = 0.5m,
                Story = "R12 000 a year, timing unknown."
            },
            new ProtoLine
            {
                Id = "geyser", Name = "Geyser service", Group = "Home", Category = "Maintenance", Type = ProtoLineType.Event,
                Planned = Only(9, 1_500), Actual = Paid(Only(9, 1_500), currentPaid: false), ShareA = 1
            },

            // Transport
            new ProtoLine
            {
                Id = "fuel", Name = "Fuel", Group = "Transport", Category = "Running costs", Type = ProtoLineType.Variable, Beneficiary = "a",
                Planned = Flat(3_000), Actual = [2_850, 3_100, 2_950, 3_300, 3_450, 3_600, 1_500, null, null, null, null, null], ShareA = 1
            },
            new ProtoLine
            {
                Id = "insurance", Name = "Car insurance", Group = "Transport", Category = "Running costs", Type = ProtoLineType.Fixed,
                Planned = Flat(1_200), Actual = [1_200, 1_200, 1_200, 1_350, 1_350, 1_350, 1_350, null, null, null, null, null], ShareB = 1,
                Story = "Premium went up in April; the change was never committed."
            },
            new ProtoLine
            {
                Id = "car-service", Name = "Car service", Group = "Transport", Category = "Running costs", Type = ProtoLineType.Event,
                Planned = Only(2, 4_000), Actual = [0, 0, 4_600, 0, 0, 0, 0, null, null, null, null, null], ShareA = 1
            },

            // Food
            new ProtoLine
            {
                Id = "groceries", Name = "Groceries", Group = "Food", Category = "Household food", Type = ProtoLineType.Variable,
                Planned = groceries, Actual = Wiggle(groceries, 11), ShareA = 0.5m, ShareB = 0.5m,
                OverrideMonth = 11, OverrideCalculated = 6_300, Story = "December has an override for the festive season."
            },

            // Children
            new ProtoLine
            {
                Id = "school", Name = "School fees", Group = "Children", Category = "School", Type = ProtoLineType.Fixed, Beneficiary = "child",
                Planned = Flat(3_500), Actual = Paid(Flat(3_500), currentPaid: true), ShareB = 1
            },
            new ProtoLine
            {
                Id = "party", Name = "Birthday party", Group = "Children", Category = "Celebrations", Type = ProtoLineType.Event, Beneficiary = "child",
                Planned = Only(8, 5_000), Actual = Paid(Only(8, 5_000), currentPaid: false), ShareA = 1
            }
        ];
    }

    private static decimal[] Flat(decimal value) => [.. Enumerable.Repeat(value, 12)];

    private static decimal[] Only(int month, decimal value)
    {
        decimal[] values = new decimal[12];
        values[month] = value;
        return values;
    }

    private static decimal?[] Future() => new decimal?[12];

    // Frozen months were paid as planned; the current month is paid if currentPaid; future months have no spends yet
    private static decimal?[] Paid(decimal[] planned, bool currentPaid) =>
        [.. planned.Select((value, month) => month < CurrentMonth ? value : month == CurrentMonth ? (currentPaid ? value : 0) : (decimal?)null)];

    // Variable lines swing around their allowance; the current month is part-way through
    private static decimal?[] Wiggle(decimal[] planned, int seed) =>
    [
        .. planned.Select((value, month) => month < CurrentMonth
            ? Math.Round(value * (1 + (((seed * 31) + (month * 17)) % 25 - 10) / 100m), 0)
            : month == CurrentMonth ? Math.Round(value * 0.45m, 0) : (decimal?)null)
    ];
}
