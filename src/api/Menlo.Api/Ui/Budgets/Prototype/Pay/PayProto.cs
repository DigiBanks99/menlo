using Menlo.Api.Ui.Budgets.Prototype.Plan;
using Menlo.Ui.Components;
using System.Globalization;

namespace Menlo.Api.Ui.Budgets.Prototype.Pay;

// PROTOTYPE for "Prototype recording a payment" (#768). Throwaway: in-memory payments and receipts on top of the
// plan prototype's (#658) made-up 2026 budget. It follows "Decide the rules for entering, changing and removing a
// payment" (#767) and "Decide how a phone photo fills in a payment" (#770). The model never runs: a receipt's
// reading is one of a few canned scenarios, picked when the photo is sent. Nothing here is the real model.

public enum PayKind { Expense, Income, Refund }

/// <summary>A part of a payment: a line id, or "cat:" and a category id for an unsorted split.</summary>
public sealed record PSplit(string Target, decimal Amount)
{
    public bool Unsorted => Target.StartsWith("cat:", StringComparison.Ordinal);

    public string CategoryId => Target[4..];
}

public sealed class PPayment
{
    public required string Id { get; init; }
    public PayKind Kind { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string Member { get; set; } = "a";
    public string Note { get; set; } = "";
    public List<PSplit> Splits { get; set; } = [];

    /// <summary>"Nothing this month": an R0 payment on one line's month.</summary>
    public bool Nothing { get; set; }

    public int Sign => Kind == PayKind.Refund ? -1 : 1;
}

public enum ReceiptStatus { Waiting, Reading, Ready, CouldntRead }

public sealed record PPhoto(string ContentType, byte[] Bytes);

/// <summary>One line item the model read, and where it put it (null: it couldn't place it).</summary>
public sealed record PItem(string Text, decimal Amount, string? Target);

/// <summary>What the model read from a receipt. Null fields are ones it couldn't read.</summary>
public sealed record PRead(decimal? Total, DateOnly? Date, string? Merchant, bool Refund, IReadOnlyList<PItem> Items, int Unreadable, bool SeveralTotals = false);

public sealed class PReceipt
{
    public required string Id { get; init; }
    public required string TakenBy { get; init; }
    public DateTimeOffset TakenAt { get; init; }
    public List<PPhoto> Photos { get; init; } = [];
    public required string Scenario { get; init; }

    /// <summary>Seeded receipts skip the wait.</summary>
    public bool ReadAlready { get; init; }

    public int Tries { get; set; } = 1;
}

/// <summary>A canned reading, standing in for the model.</summary>
public sealed record PScenario(string Key, string Label, PRead? Read)
{
    public bool Fails => Read is null;
}

public static class PayStore
{
    private static readonly Lock Gate = new();

    public static List<PPayment> Payments { get; private set; } = [];

    public static List<PReceipt> Receipts { get; private set; } = [];

    /// <summary>Whether the shared model server is up. Switch it off to see receipts wait.</summary>
    public static bool ModelUp { get; set; } = true;

    /// <summary>When the model server came back, so waiting receipts start reading from then.</summary>
    public static DateTimeOffset ModelSince { get; private set; } = DateTimeOffset.MinValue;

    /// <summary>The line and month cells whose actual is the plan standing in.</summary>
    public static HashSet<(string Line, int Month)> StandIns { get; } = [];

    private static bool seeded;

    public static void Ensure()
    {
        lock (Gate)
        {
            if (!seeded)
            {
                Seed();
                seeded = true;
                Sync();
            }
        }
    }

    public static void Change(Action action)
    {
        lock (Gate)
        {
            action();
            Sync();
        }
    }

    public static void Reset()
    {
        lock (Gate)
        {
            PlanStore.Reset();
            Payments = [];
            Receipts = [];
            ModelUp = true;
            Seed();
            Sync();
        }
    }

    public static void SetModel(bool up)
    {
        lock (Gate)
        {
            if (up && !ModelUp)
            {
                ModelSince = DateTimeOffset.Now;
            }

            ModelUp = up;
        }
    }

    public static PYear Year => PlanStore.Get(PlanStore.FirstYear);

    // -------------------------------------------------------------------------------------------------
    // Actuals: the plan prototype's grid reads line.Actual, so it is rewritten from the payments.
    // -------------------------------------------------------------------------------------------------

    private static void Sync()
    {
        StandIns.Clear();
        foreach (PYear y in PlanStore.Years.Values)
        {
            List<PPayment> ofYear = [.. Payments.Where(p => p.Date.Year == y.Year)];
            foreach (PLine line in y.Lines)
            {
                for (int m = 0; m < 12; m++)
                {
                    int month = m;
                    List<(PPayment P, PSplit S)> on = [.. ofYear.Where(p => p.Date.Month - 1 == month)
                        .SelectMany(p => p.Splits.Where(s => s.Target == line.Id).Select(s => (p, s)))];
                    if (on.Count > 0)
                    {
                        line.Actual[m] = on.Sum(x => x.P.Sign * x.S.Amount);
                    }
                    else if (m <= y.Current && !Payslip(y, line))
                    {
                        // An unsorted split in its category turns the stand-in off
                        bool covered = ofYear.Any(p => p.Date.Month - 1 == month && p.Splits.Any(s => s.Target == "cat:" + line.CategoryId));
                        line.Actual[m] = covered ? 0 : y.Planned(line, m);
                        if (!covered)
                        {
                            StandIns.Add((line.Id, m));
                        }
                    }
                    else if (m <= y.Current)
                    {
                        line.Actual[m] = y.Planned(line, m);
                        StandIns.Add((line.Id, m));
                    }
                    else
                    {
                        line.Actual[m] = null;
                    }
                }
            }
        }

        PlanHooks.Unsorted = (categoryId, month) => Payments
            .Where(p => p.Date.Month - 1 == month)
            .SelectMany(p => p.Splits.Where(s => s.Target == "cat:" + categoryId).Select(s => p.Sign * s.Amount))
            .Sum();
    }

    // -------------------------------------------------------------------------------------------------
    // Lines and categories a payment may land on
    // -------------------------------------------------------------------------------------------------

    /// <summary>Payslip lines (the pre-tax deductions here) take no payments: their plan stands in.</summary>
    public static bool Payslip(PYear y, PLine line) => y.Categories.FirstOrDefault(c => c.Id == line.CategoryId)?.ParentId == "pretax";

    public static bool IncomeCategory(PYear y, string categoryId) => y.Categories.FirstOrDefault(c => c.Id == categoryId)?.ParentId == "income";

    public static IReadOnlyList<MnlOption> Targets(PYear y, PayKind kind)
    {
        bool income = kind == PayKind.Income;
        List<MnlOption> options = [];
        foreach (PCategory top in y.Tops.Where(t => t.Id != "pretax"))
        {
            foreach (PCategory child in y.Children(top.Id))
            {
                if (IncomeCategory(y, child.Id) != income)
                {
                    continue;
                }

                options.AddRange(y.Lines.Where(l => l.CategoryId == child.Id && !Payslip(y, l))
                    .Select(l => new MnlOption(l.Id, $"{l.Name} ({child.Name})")));
                options.Add(new MnlOption("cat:" + child.Id, $"{top.Name} › {child.Name}: not sorted yet"));
            }
        }

        return options;
    }

    public static string TargetName(PYear y, string target)
    {
        if (target.StartsWith("cat:", StringComparison.Ordinal))
        {
            PCategory? c = y.Categories.FirstOrDefault(x => x.Id == target[4..]);
            return c is null ? target : $"{c.Name} (not sorted)";
        }

        return y.Lines.FirstOrDefault(l => l.Id == target)?.Name ?? target;
    }

    public static bool ValidTarget(PYear y, PayKind kind, string target) => Targets(y, kind).Any(o => o.Value == target);

    /// <summary>Who a payment's lines point to, if they all point to the same member (#767).</summary>
    public static string? MemberFromLines(PYear y, PayKind kind, IEnumerable<string> targets)
    {
        HashSet<string?> who = [.. targets.Select(t =>
        {
            PLine? l = y.Lines.FirstOrDefault(x => x.Id == t);
            if (l is null) return null;
            if (kind == PayKind.Income) return l.Beneficiary is "a" or "b" ? l.Beneficiary : null;
            return l.ShareA switch { 100 => "a", 0 => "b", _ => null };
        })];
        return who.Count == 1 ? who.Single() : null;
    }

    /// <summary>The line picked last time for a payment with the same note: the model-free match for typed payments.</summary>
    public static string? Suggest(PayKind kind, string note)
    {
        string key = Normalise(note);
        if (key.Length == 0) return null;
        return Payments.Where(p => p.Kind == kind && !p.Nothing && Normalise(p.Note) == key && p.Splits.Count > 0)
            .OrderByDescending(p => p.Date)
            .Select(p => p.Splits.OrderByDescending(s => s.Amount).First().Target)
            .FirstOrDefault();
    }

    /// <summary>The lines used most with earlier payments, for quick entry.</summary>
    public static IReadOnlyList<string> Frequent(PayKind kind, int count) =>
        [.. Payments.Where(p => p.Kind == kind && !p.Nothing).SelectMany(p => p.Splits.Select(s => s.Target))
            .Where(t => !t.StartsWith("cat:", StringComparison.Ordinal))
            .GroupBy(t => t).OrderByDescending(g => g.Count()).Take(count).Select(g => g.Key)];

    public static IReadOnlyList<string> Merchants(PayKind kind) =>
        [.. Payments.Where(p => p.Kind == kind && p.Note.Length > 0).Select(p => p.Note).Distinct().Order()];

    private static string Normalise(string note) => note.Trim().ToLowerInvariant();

    /// <summary>A payment of the same kind and amount on the same date (#767): a warning, never a rule.</summary>
    public static PPayment? Duplicate(string? id, PayKind kind, DateOnly date, decimal amount) =>
        Payments.FirstOrDefault(p => p.Id != id && !p.Nothing && p.Kind == kind && p.Date == date && p.Amount == amount);

    public static IEnumerable<(PPayment Payment, PSplit Split)> On(string target, int month, int year = PlanStore.FirstYear) =>
        Payments.Where(p => p.Date.Year == year && p.Date.Month - 1 == month)
            .SelectMany(p => p.Splits.Where(s => s.Target == target).Select(s => (p, s)))
            .OrderBy(x => x.p.Date);

    /// <summary>A category's payments in a month: its unsorted splits and its lines' splits.</summary>
    public static IEnumerable<(PPayment Payment, PSplit Split)> InCategory(PYear y, string categoryId, int month) =>
        Payments.Where(p => p.Date.Year == y.Year && p.Date.Month - 1 == month)
            .SelectMany(p => p.Splits.Where(s => s.Target == "cat:" + categoryId || y.Lines.Any(l => l.Id == s.Target && l.CategoryId == categoryId)).Select(s => (p, s)))
            .OrderBy(x => x.p.Date);

    public static PPayment? Find(string? id) => Payments.FirstOrDefault(p => p.Id == id);

    public static string NewId() => Guid.NewGuid().ToString("N")[..8];

    // -------------------------------------------------------------------------------------------------
    // Receipts: the photos wait, are read, then wait for a member to check them.
    // -------------------------------------------------------------------------------------------------

    public static readonly TimeSpan WaitFor = TimeSpan.FromSeconds(5);

    public static readonly TimeSpan ReadFor = TimeSpan.FromSeconds(12);

    public static ReceiptStatus Status(PReceipt r)
    {
        if (r.ReadAlready)
        {
            return Scenario(r.Scenario).Fails ? ReceiptStatus.CouldntRead : ReceiptStatus.Ready;
        }

        if (!ModelUp)
        {
            return ReceiptStatus.Waiting;
        }

        // Read one at a time, in the order they were taken
        DateTimeOffset start = Max(r.TakenAt, ModelSince);
        foreach (PReceipt before in Receipts.Where(x => !x.ReadAlready && x.TakenAt < r.TakenAt))
        {
            start = Max(start, Max(before.TakenAt, ModelSince) + WaitFor + ReadFor);
        }

        TimeSpan since = DateTimeOffset.Now - start;
        if (since < WaitFor) return ReceiptStatus.Waiting;
        if (since < WaitFor + ReadFor) return ReceiptStatus.Reading;
        return Scenario(r.Scenario).Fails ? ReceiptStatus.CouldntRead : ReceiptStatus.Ready;
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;

    public static bool AnyMoving => Receipts.Any(r => Status(r) is ReceiptStatus.Waiting or ReceiptStatus.Reading);

    public static int Waiting => Receipts.Count;

    public static PReceipt? Receipt(string? id) => Receipts.FirstOrDefault(r => r.Id == id);

    public static PScenario Scenario(string key) => Scenarios.FirstOrDefault(s => s.Key == key) ?? Scenarios[0];

    private static readonly DateOnly Today = new(PlanStore.FirstYear, 7, 18);

    public static DateOnly TodayDate => Today;

    public static readonly IReadOnlyList<PScenario> Scenarios =
    [
        new("hardware", "Hardware shop, with snacks on the slip", new PRead(395, Today, "Hardware Depot", false,
            [new("Wood screws 50pk", 89, "maintenance"), new("Paint roller", 129, "maintenance"), new("Sandpaper assorted", 132, "maintenance"), new("Crisps + energy drink", 45, "groceries")], 0)),
        new("fuel", "Fuel stop, with a coffee it couldn't place", new PRead(1_045, Today, "Fuel Stop", false,
            [new("Unleaded 95, 41.2 L", 950, "fuel"), new("Coffee and a pie", 95, null)], 0)),
        new("mismatch", "Groceries, two lines unreadable", new PRead(420, Today.AddDays(-1), "Fresh Market", false,
            [new("Milk 2L", 32, "groceries"), new("Bread", 22, "groceries"), new("Chicken portions", 189, "groceries"), new("Washing powder", 152, "groceries")], 2)),
        new("smudged", "Smudged date, one item", new PRead(1_200, null, "Corner Grocer", false,
            [new("Groceries", 1_200, "groceries")], 0)),
        new("refund", "A refund at the hardware shop", new PRead(129, Today, "Hardware Depot", true,
            [new("Return: paint roller", 129, "maintenance")], 0)),
        new("two", "Two slips in one photo", new PRead(310, Today, "Fresh Market", false,
            [new("Milk 2L", 32, "groceries"), new("Bread", 22, "groceries"), new("Apples 1kg", 41, "groceries")], 0, SeveralTotals: true)),
        new("blurry", "Blurry: nothing usable", null)
    ];

    // -------------------------------------------------------------------------------------------------
    // Seed: the plan prototype's made-up actuals, turned into payments.
    // -------------------------------------------------------------------------------------------------

    private static readonly Dictionary<string, string[]> Notes = new()
    {
        ["groceries"] = ["Fresh Market", "Corner Grocer", "Bulk Store", "Fresh Market"],
        ["fuel"] = ["Fuel Stop", "Highway Garage"],
        ["electricity"] = ["Prepaid electricity"],
        ["water"] = ["City water"],
        ["rates"] = ["City council"],
        ["security"] = ["Security service"],
        ["insurance"] = ["Car insurer"],
        ["car-service"] = ["Service centre"],
        ["school-fees"] = ["School"],
        ["maintenance"] = ["Hardware Depot"],
        ["salary-a"] = ["Salary"],
        ["salary-b"] = ["Salary"],
        ["party"] = ["Party shop"]
    };

    private static void Seed()
    {
        PYear y = Year;
        foreach (PLine line in y.Lines.Where(l => !Payslip(y, l)))
        {
            for (int m = 0; m <= y.Current; m++)
            {
                decimal total = line.Actual[m] ?? 0;
                if (total <= 0) continue;
                int parts = line.Id switch { "groceries" => 4, "fuel" => 2, _ => 1 };
                if (m == y.Current) parts = Math.Max(1, parts / 2);
                decimal each = Math.Round(total / parts, 0);
                for (int i = 0; i < parts; i++)
                {
                    decimal amount = i == parts - 1 ? total - (each * (parts - 1)) : each;
                    int day = Math.Min(28, 3 + (i * 7) + (line.Id.Length % 4));
                    if (m == y.Current) day = Math.Min(day, 17);
                    string[] notes = Notes.GetValueOrDefault(line.Id) ?? [line.Name];
                    Payments.Add(new PPayment
                    {
                        Id = NewId(),
                        Kind = line.Income ? PayKind.Income : PayKind.Expense,
                        Date = new DateOnly(y.Year, m + 1, day),
                        Amount = amount,
                        Member = MemberFromLines(y, line.Income ? PayKind.Income : PayKind.Expense, [line.Id]) ?? (i % 2 == 0 ? "a" : "b"),
                        Note = notes[i % notes.Length],
                        Splits = [new PSplit(line.Id, amount)]
                    });
                }
            }
        }

        // One shop split across two lines, an unsorted split, and "Nothing this month"
        PPayment big = Payments.First(p => p.Date.Month == 5 && p.Splits[0].Target == "groceries");
        big.Note = "Bulk Store";
        big.Amount += 400;
        big.Splits = [big.Splits[0], new PSplit("maintenance", 400)];
        PPayment maint = Payments.First(p => p.Date.Month == 5 && p.Splits[0].Target == "maintenance");
        maint.Amount -= 400;
        maint.Splits = [new PSplit("maintenance", maint.Amount)];

        Payments.Add(new PPayment
        {
            Id = NewId(),
            Kind = PayKind.Expense,
            Date = new DateOnly(y.Year, 7, 12),
            Amount = 600,
            Member = "b",
            Note = "Bulk Store",
            Splits = [new PSplit("cat:groceries-cat", 600)]
        });
        Payments.Add(new PPayment
        {
            Id = NewId(),
            Kind = PayKind.Expense,
            Date = new DateOnly(y.Year, 6, 30),
            Amount = 0,
            Member = "a",
            Note = "",
            Nothing = true,
            Splits = [new PSplit("maintenance", 0)]
        });

        Receipts.Add(new PReceipt { Id = NewId(), TakenBy = "b", TakenAt = DateTimeOffset.Now.AddHours(-3), Scenario = "hardware", ReadAlready = true });
        Receipts.Add(new PReceipt { Id = NewId(), TakenBy = "a", TakenAt = DateTimeOffset.Now.AddHours(-1), Scenario = "mismatch", ReadAlready = true });
    }
}

public static class PayFormat
{
    private static readonly CultureInfo Numbers = CultureInfo.InvariantCulture;

    public static string Money(decimal value) => (value < 0 ? "−" : "") + "R " + Math.Abs(value).ToString("#,0.##", Numbers);

    public static string Date(DateOnly d) => d.ToString("d MMM", Numbers);

    public static string DateLong(DateOnly d) => d.ToString("dddd d MMMM yyyy", Numbers);

    public static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", Numbers);

    public static string Member(string key) => key switch { "a" => "Member A", "b" => "Member B", _ => key };

    public static string Kind(PayKind k) => k switch { PayKind.Income => "Income", PayKind.Refund => "Refund", _ => "Expense" };

    public static string Status(ReceiptStatus s) => s switch
    {
        ReceiptStatus.Waiting => "Waiting",
        ReceiptStatus.Reading => "Reading",
        ReceiptStatus.Ready => "Ready to check",
        _ => "Couldn't read"
    };

    public static MnlTone StatusTone(ReceiptStatus s) => s switch
    {
        ReceiptStatus.Waiting => MnlTone.Neutral,
        ReceiptStatus.Reading => MnlTone.Info,
        ReceiptStatus.Ready => MnlTone.Success,
        _ => MnlTone.Warning
    };

    public static string Ago(DateTimeOffset at)
    {
        TimeSpan t = DateTimeOffset.Now - at;
        return t.TotalMinutes < 1 ? "just now" : t.TotalHours < 1 ? $"{(int)t.TotalMinutes} min ago" : $"{(int)t.TotalHours} h ago";
    }
}
