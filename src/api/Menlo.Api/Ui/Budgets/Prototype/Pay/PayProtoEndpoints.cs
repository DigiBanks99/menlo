using Menlo.Api.Ui.Budgets.Prototype.Plan;
using Menlo.Api.Ui.Shared;
using Menlo.Ui.Components;
using Menlo.Ui.Layouts;
using System.Text;

namespace Menlo.Api.Ui.Budgets.Prototype.Pay;

// PROTOTYPE (#768): the routes. Mapped only where the design gallery is (Development), on made-up data, so they
// need no sign-in. "Signed in as" is a prototype switch (a cookie), for trying the member defaults.

public static class PayVariants
{
    public static readonly (string Key, string Name)[] All =
    [
        ("A", "Panel over the budget"), ("B", "Quick entry page"), ("C", "In the month table")
    ];

    public static string Of(string? key) => All.Any(v => v.Key == key) ? key! : "A";

    public static string Name(string key) => All.First(v => v.Key == key).Name;
}

/// <summary>The budget page with payments on it.</summary>
public sealed record PayPageModel(PSheet Sheet, string Variant, string SignedIn, string? Editing, bool Adding)
{
    public PQuery Query => Sheet.Query;

    public PYear Year => Sheet.Plan;

    public string VariantName => PayVariants.Name(Variant);
}

/// <summary>A line's or a category's payments in one month: how a payment is found again without a payments page.</summary>
public sealed record PayMonthModel(string Variant, string Target, int Month, string Back, string SignedIn, PayFormModel? Form = null, string? Editing = null)
{
    public PYear Year => PayStore.Year;

    public bool IsCategory => Target.StartsWith("cat:", StringComparison.Ordinal);

    public PLine? Line => IsCategory ? null : Year.Lines.FirstOrDefault(l => l.Id == Target);

    public string Name => IsCategory ? Year.Categories.FirstOrDefault(c => c.Id == Target[4..])?.Name ?? Target : Line?.Name ?? Target;

    public string Title => $"{Name}, {PFormat.MonthLong(Month)}";

    public IReadOnlyList<(PPayment Payment, PSplit Split)> Rows =>
        [.. IsCategory ? PayStore.InCategory(Year, Target[4..], Month) : PayStore.On(Target, Month)];

    public bool StandIn => Line is not null && PayStore.StandIns.Contains((Line.Id, Month));

    public bool CanNothing => Line is not null && Rows.Count == 0 && !PayStore.Payslip(Year, Line);

    public string Total => PayFormat.Money(Rows.Sum(r => r.Payment.Sign * r.Split.Amount));

    public string Planned => Line is null ? "" : PayFormat.Money(Year.Planned(Line, Month));
}

/// <summary>Links that keep the budget view a receipt was opened from, so Close and Cancel go back to it.</summary>
public static class PayLinks
{
    public static string Receipts(string variant, string budget) =>
        $"{PQuery.PayRoot}/receipts?variant={variant}&budget={Uri.EscapeDataString(budget)}";

    public static string Receipt(string id, string variant, string budget, bool hand = false) =>
        $"{PQuery.PayRoot}/receipts/{id}?variant={variant}&budget={Uri.EscapeDataString(budget)}{(hand ? "&hand=1" : "")}";

    public static string Budget(string? budget, string variant) =>
        !string.IsNullOrEmpty(budget) && budget.StartsWith(PQuery.PayRoot + "?", StringComparison.Ordinal) ? budget : $"{PQuery.PayRoot}?variant={variant}";
}

public sealed record PayReceiptsModel(string Variant, string SignedIn, string Budget)
{
    public IReadOnlyList<PReceipt> Receipts => [.. PayStore.Receipts.OrderByDescending(r => r.TakenAt)];
}

public sealed record PayReceiptModel(PReceipt Receipt, ReceiptStatus Status, PayFormModel? Form, string Variant, string Budget);

internal static class PayProtoEndpoints
{
    private const string Root = PQuery.PayRoot;

    public static IEndpointRouteBuilder MapPayPrototype(this IEndpointRouteBuilder app)
    {
        PageContext.PrototypeNav = () =>
        [
            new MnlNavItem("Pay (prototype)", Root + "/quick?variant=B", MnlIconName.Plus),
            new MnlNavItem($"Receipts ({PayStore.Waiting})", Root + "/receipts", MnlIconName.Receipt)
        ];

        app.MapGet(Root, (HttpContext context) =>
            {
                PayStore.Ensure();
                PQuery q = PQuery.From(context.Request);
                string variant = PayVariants.Of(context.Request.Query["variant"]);
                return Render.Page<PayProtoPage>(context, new PayPageModel(PSheet.Build(q), variant, PayForms.SignedIn(context),
                    context.Request.Query["pay"], context.Request.Query["add"] == "1"));
            })
            .AllowAnonymous();

        // A: a panel; B: the quick-entry page
        app.MapGet(Root + "/form", (HttpContext context) =>
            {
                PayStore.Ensure();
                IQueryCollection q = context.Request.Query;
                string variant = PayVariants.Of(q["variant"]);
                string back = Back(context, q["back"]);
                int? month = int.TryParse(q["month"], out int m) ? m : null;
                string? target = q["target"].ToString() is { Length: > 0 } t ? t : null;
                bool panel = variant == "A" && HxRequest.IsFragment(context.Request);
                PayLayout layout = panel ? PayLayout.Panel : PayLayout.Page;
                PayFormModel form = PayStore.Find(q["id"]) is PPayment p
                    ? PayForms.Edit(context, p, variant, layout, back)
                    : PayForms.New(context, variant, layout, back, target, month);
                return panel ? (IResult)Render.Fragment<PayPanel>(new PayPanelModel(form, null)) : Render.Page<PayQuickPage>(context, form);
            })
            .AllowAnonymous();

        app.MapGet(Root + "/quick", (HttpContext context) =>
            {
                PayStore.Ensure();
                string variant = PayVariants.Of(context.Request.Query["variant"]);
                PayFormModel form = PayForms.New(context, variant, PayLayout.Page, Back(context, context.Request.Query["back"]), null, null);
                return Render.Page<PayQuickPage>(context, form);
            })
            .AllowAnonymous();

        app.MapPost(Root + "/form/refresh", (HttpContext context, IFormCollection f) =>
            {
                PayFormModel m = PayForms.Read(context, f);
                string trigger = context.Request.Headers["HX-Trigger-Name"].ToString();
                List<SplitRow> splits = [.. m.Splits];
                string op = f["op"].ToString();
                if (f["setkind"].ToString() is { Length: > 0 } sk && Enum.TryParse(sk, out PayKind k)) m = m with { Kind = k };
                if (f["pick"].ToString() is { Length: > 0 } pick) splits = [new SplitRow(pick, m.Amount)];
                if (op == "add")
                {
                    if (splits.Count == 1) splits[0] = splits[0] with { Amount = m.Amount };
                    splits.Add(new SplitRow("", ""));
                }
                else if (op.StartsWith("remove:", StringComparison.Ordinal) && int.TryParse(op[7..], out int i) && i < splits.Count)
                {
                    splits.RemoveAt(i);
                    if (splits.Count == 0) splits.Add(new SplitRow("", m.Amount));
                }

                m = m with { Splits = splits, MemberByHand = m.MemberByHand || trigger == "member" };
                return Render.Fragment<PayFormView>(PayForms.Defaults(m, noteChanged: trigger == "note"));
            })
            .AllowAnonymous();

        app.MapPost(Root + "/save", (HttpContext context, IFormCollection f) =>
            {
                PayFormModel m = PayForms.Defaults(PayForms.Read(context, f), noteChanged: false);
                (PPayment? payment, PayFormModel form) = PayForms.Validate(m, f["anyway"] == "true");
                if (payment is null)
                {
                    return (IResult)Render.Invalid<PayFormView>(form);
                }

                PayStore.Change(() =>
                {
                    PayStore.Payments.RemoveAll(p => p.Id == payment.Id);
                    // A real payment replaces "Nothing this month" on its lines
                    PayStore.Payments.RemoveAll(p => p.Nothing && p.Date.Year == payment.Date.Year && p.Date.Month == payment.Date.Month
                                                     && payment.Splits.Any(s => s.Target == p.Splits[0].Target));
                    PayStore.Payments.Add(payment);
                    if (form.ReceiptId is not null) PayStore.Receipts.RemoveAll(r => r.Id == form.ReceiptId);
                });
                string where = string.Join(", ", payment.Splits.Select(s => PayStore.TargetName(PayStore.Year, s.Target)));
                Flash.Set(context, $"{(form.IsNew ? "Saved" : "Changed")}: {PayFormat.Money(payment.Amount)} on {where}, {PayFormat.Date(payment.Date)}."
                                   + (form.ReceiptId is not null ? " The receipt and its photos are deleted." : ""));
                // From a receipt: back to the list while others wait, otherwise to the budget view it came from
                return Navigate.To(context, form.ReceiptId is not null && PayStore.Receipts.Count > 0 ? PayLinks.Receipts(form.Variant, form.Back) : form.Back);
            })
            .AllowAnonymous();

        app.MapPost(Root + "/remove", (HttpContext context, IFormCollection f) =>
            {
                PayFormModel m = PayForms.Read(context, f);
                if (f["confirm"] != "true")
                {
                    return (IResult)Render.Fragment<PayFormView>(m with { ConfirmRemove = true, Amount = PayFormat.Money(PayStore.Find(m.Id)?.Amount ?? 0) });
                }

                PayStore.Change(() => PayStore.Payments.RemoveAll(p => p.Id == m.Id));
                Flash.Set(context, "Payment removed for good.", MnlTone.Info);
                return Navigate.To(context, m.Back);
            })
            .AllowAnonymous();

        app.MapPost(Root + "/nothing", (HttpContext context, IFormCollection f) =>
            {
                string line = f["target"].ToString();
                int month = int.TryParse(f["month"], out int mm) ? mm : 0;
                string back = Back(context, f["back"]);
                PLine? l = PayStore.Year.Lines.FirstOrDefault(x => x.Id == line);
                if (l is null || PayStore.On(line, month).Any())
                {
                    Flash.Set(context, "Something is already recorded on that line this month.", MnlTone.Error);
                    return Navigate.To(context, back);
                }

                PayStore.Change(() => PayStore.Payments.Add(new PPayment
                {
                    Id = PayStore.NewId(),
                    Kind = l.Income ? PayKind.Income : PayKind.Expense,
                    Nothing = true,
                    Amount = 0,
                    Date = new DateOnly(PayStore.Year.Year, month + 1, DateTime.DaysInMonth(PayStore.Year.Year, month + 1)),
                    Member = PayForms.SignedIn(context),
                    Splits = [new PSplit(line, 0)]
                }));
                Flash.Set(context, $"{l.Name}: nothing in {PFormat.MonthLong(month)}. Its actual is R0, not the plan.", MnlTone.Info);
                return Navigate.To(context, back);
            })
            .AllowAnonymous();

        // A: a panel; B: a page. (C shows the same list under the line, in the month table.)
        app.MapGet(Root + "/month", (HttpContext context) =>
            {
                PayStore.Ensure();
                IQueryCollection q = context.Request.Query;
                string variant = PayVariants.Of(q["variant"]);
                int month = int.TryParse(q["month"], out int mm) ? Math.Clamp(mm, 0, 11) : 0;
                string back = Back(context, q["back"]);
                PayMonthModel model = new(variant, q["target"].ToString(), month, back, PayForms.SignedIn(context));
                return variant == "A" && HxRequest.IsFragment(context.Request)
                    ? (IResult)Render.Fragment<PayPanel>(new PayPanelModel(null, model))
                    : Render.Page<PayMonthPage>(context, model);
            })
            .AllowAnonymous();

        app.MapGet(Root + "/receipts", (HttpContext context) =>
            {
                PayStore.Ensure();
                string v = PayVariants.Of(context.Request.Query["variant"]);
                return Render.Page<PayReceiptsPage>(context, new PayReceiptsModel(v, PayForms.SignedIn(context), PayLinks.Budget(context.Request.Query["budget"], v)));
            })
            .AllowAnonymous();

        app.MapGet(Root + "/receipts/list", (HttpContext context) =>
                Render.Fragment<PayReceiptList>(new PayReceiptsModel(PayVariants.Of(context.Request.Query["variant"]), PayForms.SignedIn(context),
                    PayLinks.Budget(context.Request.Query["budget"], PayVariants.Of(context.Request.Query["variant"])))))
            .AllowAnonymous();

        app.MapGet(Root + "/receipts/new", (HttpContext context) =>
                Results.LocalRedirect(PayLinks.Receipts(PayVariants.Of(context.Request.Query["variant"]),
                    PayLinks.Budget(context.Request.Query["budget"], PayVariants.Of(context.Request.Query["variant"]))) + "#take"))
            .AllowAnonymous();

        app.MapPost(Root + "/receipts", async (HttpContext context) =>
            {
                IFormCollection f = await context.Request.ReadFormAsync();
                string variant = PayVariants.Of(f["variant"]);
                string budget = PayLinks.Budget(f["budget"], variant);
                List<IFormFile> files = [.. f.Files.Where(x => x.Length > 0)];
                if (files.Count is 0 or > 3)
                {
                    Flash.Set(context, files.Count == 0 ? "Take or pick a photo first." : "A receipt has up to 3 photos: the pages of one slip.", MnlTone.Error);
                    return Navigate.To(context, PayLinks.Receipts(variant, budget));
                }

                List<PPhoto> photos = [];
                foreach (IFormFile file in files)
                {
                    using MemoryStream ms = new();
                    await file.CopyToAsync(ms);
                    photos.Add(new PPhoto(file.ContentType, ms.ToArray()));
                }

                PayStore.Change(() => PayStore.Receipts.Add(new PReceipt
                {
                    Id = PayStore.NewId(),
                    TakenBy = PayForms.SignedIn(context),
                    TakenAt = DateTimeOffset.Now,
                    Photos = photos,
                    Scenario = PayStore.Scenario(f["scenario"].ToString()).Key
                }));
                Flash.Set(context, PayStore.ModelUp
                    ? "Sent. The agent is reading the slip: you can close this and check it later."
                    : "Sent. The model server is down, so it waits and is read when the server is back.", MnlTone.Info);
                return Navigate.To(context, PayLinks.Receipts(variant, budget));
            })
            .AllowAnonymous();

        app.MapGet(Root + "/receipts/{id}", (HttpContext context, string id) =>
            {
                PayStore.Ensure();
                string variant = PayVariants.Of(context.Request.Query["variant"]);
                string back = PayLinks.Budget(context.Request.Query["budget"], variant);
                if (PayStore.Receipt(id) is not PReceipt r)
                {
                    Flash.Set(context, "That receipt was saved or discarded.", MnlTone.Info);
                    return Results.LocalRedirect(PayLinks.Receipts(variant, back));
                }

                ReceiptStatus status = PayStore.Status(r);
                bool hand = context.Request.Query["hand"] == "1";
                PayFormModel? form = status switch
                {
                    ReceiptStatus.Ready => PayForms.FromReceipt(context, r, variant, back),
                    ReceiptStatus.CouldntRead => PayForms.FromReceipt(context, r, variant, back),
                    _ when hand => PayForms.New(context, variant, PayLayout.Page, back, null, null) with { ReceiptId = r.Id, Member = r.TakenBy, MemberByHand = true },
                    _ => null
                };
                return Render.Page<PayReceiptPage>(context, new PayReceiptModel(r, status, form, variant, back));
            })
            .AllowAnonymous();

        app.MapGet(Root + "/receipts/{id}/photo/{n:int}", (string id, int n) =>
            {
                if (PayStore.Receipt(id) is not PReceipt r) return Results.NotFound();
                if (r.Photos.Count == 0) return Results.Text(SlipSvg(r), "image/svg+xml", Encoding.UTF8);
                return n < r.Photos.Count ? Results.File(r.Photos[n].Bytes, r.Photos[n].ContentType) : Results.NotFound();
            })
            .AllowAnonymous();

        app.MapPost(Root + "/receipts/{id}/discard", (HttpContext context, string id, IFormCollection f) =>
            {
                PayStore.Change(() => PayStore.Receipts.RemoveAll(r => r.Id == id));
                Flash.Set(context, "Receipt discarded. Its photos are deleted.", MnlTone.Info);
                string v = PayVariants.Of(f["variant"]);
                string budget = PayLinks.Budget(f["back"], v);
                return Navigate.To(context, PayStore.Receipts.Count > 0 ? PayLinks.Receipts(v, budget) : budget);
            })
            .AllowAnonymous();

        app.MapPost(Root + "/settings", (HttpContext context, IFormCollection f) =>
            {
                string back = Back(context, f["back"]);
                switch (f["op"].ToString())
                {
                    case "model":
                        PayStore.SetModel(f["up"] == "true");
                        Flash.Set(context, PayStore.ModelUp ? "Model server back up: waiting receipts are read in order." : "Model server down: new receipts wait.", MnlTone.Info);
                        break;
                    case "as":
                        context.Response.Cookies.Append("proto-pay-as", f["member"] == "b" ? "b" : "a");
                        Flash.Set(context, $"Signed in as {PayFormat.Member(f["member"] == "b" ? "b" : "a")} (prototype).", MnlTone.Info);
                        break;
                    case "reset":
                        PayStore.Reset();
                        Flash.Set(context, "Prototype data put back.", MnlTone.Info);
                        break;
                }

                return Navigate.To(context, back);
            })
            .AllowAnonymous();

        return app;
    }

    private static string Back(HttpContext context, string? back)
    {
        if (!string.IsNullOrEmpty(back) && back.StartsWith(Root, StringComparison.Ordinal)) return back;
        string current = context.Request.Headers["HX-Current-URL"].ToString();
        if (Uri.TryCreate(current, UriKind.Absolute, out Uri? u) && u.PathAndQuery.StartsWith(Root, StringComparison.Ordinal)) return u.PathAndQuery;
        return Root + "?variant=" + PayVariants.Of(context.Request.Query["variant"]);
    }

    /// <summary>Seeded receipts have no real photo: draw a till slip from what the scenario "read".</summary>
    private static string SlipSvg(PReceipt r)
    {
        PRead read = PayStore.Scenario(r.Scenario).Read ?? new PRead(null, null, null, false, [], 0);
        StringBuilder sb = new();
        int h = 230 + (read.Items.Count * 26) + (read.Unreadable * 26);
        sb.Append($"""<svg xmlns="http://www.w3.org/2000/svg" width="320" height="{h}" viewBox="0 0 320 {h}" font-family="monospace" font-size="14">""");
        sb.Append($"""<rect width="320" height="{h}" fill="#f4f1ea"/><text x="160" y="40" text-anchor="middle" font-size="18" font-weight="bold">{Esc(read.Merchant ?? "")}</text>""");
        sb.Append($"""<text x="160" y="62" text-anchor="middle">{(read.Date is DateOnly d ? d.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture) : "▒▒/▒▒/2026")}</text>""");
        int yPos = 100;
        foreach (PItem item in read.Items)
        {
            sb.Append($"""<text x="20" y="{yPos}">{Esc(item.Text)}</text><text x="300" y="{yPos}" text-anchor="end">{item.Amount:0.00}</text>""");
            yPos += 26;
        }

        for (int i = 0; i < read.Unreadable; i++)
        {
            sb.Append($"""<rect x="20" y="{yPos - 14}" width="280" height="16" fill="#cfc8b8"/>""");
            yPos += 26;
        }

        sb.Append($"""<line x1="20" y1="{yPos}" x2="300" y2="{yPos}" stroke="#333" stroke-dasharray="4 3"/>""");
        sb.Append($"""<text x="20" y="{yPos + 30}" font-weight="bold">{(read.Refund ? "REFUND" : "TOTAL")}</text><text x="300" y="{yPos + 30}" text-anchor="end" font-weight="bold">{read.Total:0.00}</text>""");
        sb.Append($"""<text x="160" y="{yPos + 70}" text-anchor="middle" font-size="11">Made-up slip for the prototype</text></svg>""");
        return sb.ToString();
    }

    private static string Esc(string s) => System.Security.SecurityElement.Escape(s) ?? "";
}
