using Menlo.Api.Ui.Budgets.Prototype.Plan;
using Menlo.Api.Ui.Shared;
using Menlo.Ui.Components;
using Menlo.Ui.Utilities;
using System.Globalization;

namespace Menlo.Api.Ui.Budgets.Prototype.Pay;

// PROTOTYPE (#768): the payment form's state and rules. Every variant posts the same fields, and the server
// works out the defaults again on each change: the member from the lines, the line from the note.

public sealed record SplitRow(string Target, string Amount);

/// <summary>Where the form is drawn: in a panel (A), as its own phone-first page (B), or under a line in the month table (C).</summary>
public enum PayLayout { Panel, Page, Inline }

public sealed record PayFormModel
{
    public required string Variant { get; init; }
    public required PayLayout Layout { get; init; }
    public string? Id { get; init; }
    public string? ReceiptId { get; init; }
    public PayKind Kind { get; init; }
    public string Date { get; init; } = "";
    public string Amount { get; init; } = "";
    public string Member { get; init; } = "a";
    public bool MemberByHand { get; init; }
    public string Note { get; init; } = "";
    public IReadOnlyList<SplitRow> Splits { get; init; } = [];
    public FormErrors Errors { get; init; } = FormErrors.None;
    public required string Back { get; init; }
    public required string SignedIn { get; init; }

    /// <summary>Set when a payment of the same kind and amount is already recorded that day.</summary>
    public string? Duplicate { get; init; }

    public bool ConfirmRemove { get; init; }

    /// <summary>Why a field was filled in, or what to check, shown beside it.</summary>
    public string? MemberHint { get; init; }

    public string? SplitHint { get; init; }

    /// <summary>From a receipt: fields the model couldn't read, and what the rules found.</summary>
    public IReadOnlySet<string> CouldntRead { get; init; } = new HashSet<string>();

    public IReadOnlyList<(string Text, MnlTone Tone)> Checks { get; init; } = [];

    public IReadOnlyList<PItem> Items { get; init; } = [];

    public bool IsNew => Id is null;

    public bool OneSplit => Splits.Count <= 1;

    public string Title => ReceiptId is not null ? "Check the receipt" : IsNew ? "Record a payment" : "Change the payment";

    public IReadOnlyList<MnlOption> TargetOptions => PayStore.Targets(PayStore.Year, Kind);

    public static IReadOnlyList<MnlOption> KindOptions { get; } =
        [new("Expense", "Expense (money out)"), new("Income", "Income (money in)"), new("Refund", "Refund (money back)")];

    public static IReadOnlyList<MnlOption> MemberOptions { get; } = [new("a", "Member A"), new("b", "Member B")];

    public string SplitTotal
    {
        get
        {
            decimal sum = Splits.Sum(s => MoneyFormat.TryParse(s.Amount, out decimal a) ? a : 0);
            return PayFormat.Money(sum);
        }
    }

    public string TargetName(string target) => PayStore.TargetName(PayStore.Year, target);
}

public static class PayForms
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string SignedIn(HttpContext context) => context.Request.Cookies["proto-pay-as"] is "b" ? "b" : "a";

    /// <summary>A blank form, optionally on a line or category and in a month.</summary>
    public static PayFormModel New(HttpContext context, string variant, PayLayout layout, string back, string? target, int? month, PayKind? kind = null)
    {
        PYear y = PayStore.Year;
        PayKind k = kind ?? (target is not null && !target.StartsWith("cat:", StringComparison.Ordinal) && y.Lines.FirstOrDefault(l => l.Id == target)?.Income == true
            ? PayKind.Income
            : target is not null && target.StartsWith("cat:", StringComparison.Ordinal) && PayStore.IncomeCategory(y, target[4..]) ? PayKind.Income : PayKind.Expense);
        DateOnly date = month is int m && m != PayStore.TodayDate.Month - 1
            ? new DateOnly(y.Year, m + 1, Math.Min(15, DateTime.DaysInMonth(y.Year, m + 1)))
            : PayStore.TodayDate;
        PayFormModel model = new()
        {
            Variant = variant,
            Layout = layout,
            Back = back,
            SignedIn = SignedIn(context),
            Kind = k,
            Date = PayFormat.Iso(date),
            Splits = [new SplitRow(target ?? "", "")]
        };
        return Defaults(model, noteChanged: false);
    }

    public static PayFormModel Edit(HttpContext context, PPayment p, string variant, PayLayout layout, string back) => Defaults(new PayFormModel
    {
        Variant = variant,
        Layout = layout,
        Back = back,
        SignedIn = SignedIn(context),
        Id = p.Id,
        Kind = p.Kind,
        Date = PayFormat.Iso(p.Date),
        Amount = p.Amount.ToString("0.##", Inv),
        Member = p.Member,
        MemberByHand = true,
        Note = p.Note,
        Splits = [.. p.Splits.Select(s => new SplitRow(s.Target, s.Amount.ToString("0.##", Inv)))]
    }, noteChanged: false);

    /// <summary>A receipt's reading, filled into the form (#770). Fields it couldn't read stay empty and are marked.</summary>
    public static PayFormModel FromReceipt(HttpContext context, PReceipt r, string variant, string back)
    {
        PRead? read = PayStore.Scenario(r.Scenario).Read;
        PayFormModel model = new()
        {
            Variant = variant,
            Layout = PayLayout.Page,
            Back = back,
            SignedIn = SignedIn(context),
            ReceiptId = r.Id,
            Member = r.TakenBy,
            MemberByHand = true,
            Kind = read?.Refund == true ? PayKind.Refund : PayKind.Expense,
            Splits = [new SplitRow("", "")]
        };
        if (read is null)
        {
            return model with { Checks = [("The model couldn't read anything usable from this photo. Type it in from the photo, or discard it.", MnlTone.Warning)] };
        }

        HashSet<string> unread = [];
        if (read.Total is null) unread.Add("amount");
        if (read.Date is null) unread.Add("date");
        if (read.Merchant is null) unread.Add("note");

        // The model's splits: its items grouped by line, the ones it couldn't place on an unsorted split
        decimal total = read.Total ?? read.Items.Sum(i => i.Amount);
        List<SplitRow> splits = [.. read.Items.Where(i => i.Target is not null).GroupBy(i => i.Target!)
            .Select(g => new SplitRow(g.Key, g.Sum(i => i.Amount).ToString("0.##", Inv)))];
        decimal placed = read.Items.Where(i => i.Target is not null).Sum(i => i.Amount);
        string fallback = "cat:" + (PayStore.Year.Lines.FirstOrDefault(l => l.Id == splits.FirstOrDefault()?.Target)?.CategoryId ?? "groceries-cat");
        List<(string, MnlTone)> checks = [];
        decimal unplaced = read.Items.Where(i => i.Target is null).Sum(i => i.Amount);
        decimal itemsSum = read.Items.Sum(i => i.Amount);
        if (read.Total is decimal t && itemsSum != t)
        {
            checks.Add(($"The items add up to {PayFormat.Money(itemsSum)}, the slip says {PayFormat.Money(t)}. The difference is on an unsorted split.", MnlTone.Warning));
        }

        if (read.Unreadable > 0)
        {
            checks.Add(($"{read.Unreadable} line(s) on the slip couldn't be read. Check them against the photo.", MnlTone.Warning));
        }

        if (unplaced > 0)
        {
            checks.Add(($"{PayFormat.Money(unplaced)} not sorted: the agent couldn't place {string.Join(", ", read.Items.Where(i => i.Target is null).Select(i => i.Text))}.", MnlTone.Info));
        }

        if (read.Date is DateOnly d && Math.Abs(d.DayNumber - PayStore.TodayDate.DayNumber) > 31)
        {
            checks.Add(("The date is more than a month from the day the photo was taken.", MnlTone.Warning));
        }

        if (read.SeveralTotals)
        {
            checks.Add(("The photo seems to show more than one total. One receipt is one payment: take one photo per slip.", MnlTone.Warning));
        }

        if (total - placed > 0)
        {
            splits.Add(new SplitRow(fallback, (total - placed).ToString("0.##", Inv)));
        }

        return model with
        {
            Amount = read.Total?.ToString("0.##", Inv) ?? "",
            Date = read.Date is DateOnly dd ? PayFormat.Iso(dd) : "",
            Note = read.Merchant ?? "",
            Splits = splits.Count == 0 ? [new SplitRow("", "")] : splits,
            CouldntRead = unread,
            Checks = checks,
            Items = read.Items,
            MemberHint = "Who took the photo. They were at the till."
        };
    }

    /// <summary>Reads the posted form. Same-named split fields arrive as parallel lists.</summary>
    public static PayFormModel Read(HttpContext context, IFormCollection f)
    {
        List<SplitRow> splits = [.. f["target"].Zip(f["share"], (t, a) => new SplitRow(t ?? "", a ?? ""))];
        PReceipt? receipt = PayStore.Receipt(f["receipt"]);
        PayFormModel baseModel = receipt is not null
            ? FromReceipt(context, receipt, f["variant"].ToString(), f["back"].ToString())
            : new PayFormModel { Variant = f["variant"].ToString(), Layout = PayLayout.Panel, Back = "", SignedIn = SignedIn(context) };
        return baseModel with
        {
            Variant = f["variant"].ToString() is { Length: > 0 } v ? v : "A",
            Layout = Enum.TryParse(f["layout"], out PayLayout l) ? l : PayLayout.Panel,
            Back = f["back"].ToString() is { Length: > 0 } b && b.StartsWith(PQuery.PayRoot, StringComparison.Ordinal) ? b : PQuery.PayRoot,
            Id = PayStore.Find(f["id"]) is null ? null : f["id"].ToString(),
            ReceiptId = receipt?.Id,
            Kind = Enum.TryParse(f["kind"], out PayKind k) ? k : PayKind.Expense,
            Date = f["date"].ToString(),
            Amount = f["amount"].ToString(),
            Member = f["member"] == "b" ? "b" : "a",
            MemberByHand = f["memberByHand"] == "true",
            Note = f["note"].ToString().Trim(),
            Splits = splits.Count == 0 ? [new SplitRow("", "")] : splits,
            SignedIn = SignedIn(context),
            Errors = FormErrors.None,
            Duplicate = null,
            ConfirmRemove = false
        };
    }

    /// <summary>
    /// Works out the defaults again after a change (#767): the line from an earlier payment with the same note,
    /// a one-split payment's amount from the total, and the member from the lines unless one was picked by hand.
    /// </summary>
    public static PayFormModel Defaults(PayFormModel m, bool noteChanged)
    {
        PYear y = PayStore.Year;
        List<SplitRow> splits = [.. m.Splits];
        string? splitHint = null;

        // Lines that don't suit the kind are picked again
        if (splits.Any(s => s.Target.Length > 0 && !PayStore.ValidTarget(y, m.Kind, s.Target)))
        {
            splits = [.. splits.Select(s => s.Target.Length > 0 && !PayStore.ValidTarget(y, m.Kind, s.Target) ? s with { Target = "" } : s)];
            splitHint = $"Pick the line again: {PayFormat.Kind(m.Kind).ToLowerInvariant()} payments go on {(m.Kind == PayKind.Income ? "income" : "expense")} lines.";
        }

        if (noteChanged && splits.Count == 1 && m.ReceiptId is null && PayStore.Suggest(m.Kind, m.Note) is string suggested)
        {
            splits[0] = splits[0] with { Target = suggested };
            splitHint = $"Picked from your last payment at \"{m.Note}\".";
        }

        if (splits.Count == 1)
        {
            splits[0] = splits[0] with { Amount = m.Amount };
        }

        string member = m.Member;
        string? memberHint = m.MemberHint;
        if (!m.MemberByHand)
        {
            string? fromLines = PayStore.MemberFromLines(y, m.Kind, splits.Where(s => s.Target.Length > 0).Select(s => s.Target));
            member = fromLines ?? m.SignedIn;
            memberHint = fromLines is not null
                ? m.Kind == PayKind.Income ? "Who the line is for." : "The line's only payer."
                : "You: the line is shared, or not picked yet.";
        }

        return m with { Splits = splits, Member = member, MemberHint = memberHint, SplitHint = splitHint ?? m.SplitHint };
    }

    /// <summary>The rules from #767. Returns the payment to save, or the form with its errors.</summary>
    public static (PPayment? Payment, PayFormModel Form) Validate(PayFormModel m, bool saveAnyway)
    {
        PYear y = PayStore.Year;
        FormErrors errors = new();
        bool amountOk = MoneyFormat.TryParse(m.Amount, out decimal amount);
        if (!amountOk || amount <= 0)
        {
            errors.Add("amount", "Enter an amount above R0. For R0, use \"Nothing this month\" on the line.");
        }

        bool dateOk = DateOnly.TryParseExact(m.Date, "yyyy-MM-dd", Inv, DateTimeStyles.None, out DateOnly date);
        if (!dateOk)
        {
            errors.Add("date", "Enter the date it was paid.");
        }
        else if (!PlanStore.Years.ContainsKey(date.Year))
        {
            errors.Add("date", $"There's no {date.Year} budget yet, so there are no lines to put it on.");
        }

        PPayment? existing = PayStore.Find(m.Id);
        List<SplitRow> rows = [.. m.Splits];
        if (existing is not null && dateOk && existing.Date.Year != date.Year && PlanStore.Years.ContainsKey(date.Year))
        {
            rows = [new SplitRow("", m.Amount)];
            errors.Add("splits", $"{date.Year} has its own lines: pick them again.");
        }

        List<PSplit> splits = [];
        for (int i = 0; i < rows.Count; i++)
        {
            SplitRow row = rows[i];
            if (row.Target.Length == 0)
            {
                errors.Add("splits", rows.Count == 1 ? "Pick the line it's for, or a category to sort it later." : $"Pick a line for split {i + 1}.");
                continue;
            }

            if (!MoneyFormat.TryParse(row.Amount, out decimal a) || a <= 0)
            {
                errors.Add("splits", $"Give split {i + 1} an amount above R0.");
                continue;
            }

            splits.Add(new PSplit(row.Target, a));
        }

        if (amountOk && amount > 0 && splits.Count == rows.Count && splits.Sum(s => s.Amount) != amount)
        {
            errors.Add("splits", $"The splits add up to {PayFormat.Money(splits.Sum(s => s.Amount))}, the payment is {PayFormat.Money(amount)}.");
        }

        PayFormModel form = m with { Splits = rows, Errors = errors };
        if (!errors.IsValid)
        {
            return (null, form);
        }

        if (!saveAnyway && PayStore.Duplicate(m.Id, m.Kind, date, amount) is PPayment twin)
        {
            string where = string.Join(", ", twin.Splits.Select(s => PayStore.TargetName(y, s.Target)));
            return (null, form with
            {
                Duplicate = $"A {PayFormat.Money(amount)} {PayFormat.Kind(m.Kind).ToLowerInvariant()} payment on {PayFormat.Date(date)} is already recorded"
                            + $"{(twin.Member == m.SignedIn ? " by you" : $" by {PayFormat.Member(twin.Member)}")}, on {where}{(twin.Note.Length > 0 ? $" (\"{twin.Note}\")" : "")}."
            });
        }

        return (new PPayment
        {
            Id = existing?.Id ?? PayStore.NewId(),
            Kind = m.Kind,
            Date = date,
            Amount = amount,
            Member = m.Member,
            Note = m.Note,
            Splits = splits
        }, form);
    }
}
