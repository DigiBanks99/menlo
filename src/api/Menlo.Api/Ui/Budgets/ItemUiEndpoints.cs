using Menlo.Api.Budget.Items;
using Menlo.Api.Ui.Shared;
using Menlo.Application.Auth;
using Menlo.Ui.Components;
using Menlo.Ui.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Menlo.Api.Ui.Budgets;

/// <summary>
/// The budget item forms of the items workspace: add one, add one per month, edit, fill forward,
/// record a bill or a payment, and delete. Each form opens in a panel. A successful change answers
/// with no content and the "items-changed" event; the workspace and the summary reload themselves.
/// </summary>
internal static class ItemUiEndpoints
{
    private const string Currency = "ZAR";
    private static readonly string[] Flows = ["Income", "Expense"];
    private static readonly string[] Attributions = ["Main", "Rental", "ServiceProvider"];

    public static RouteGroupBuilder MapItemForms(this RouteGroupBuilder budgets)
    {
        RouteGroupBuilder items = budgets.MapGroup("/{budgetId:guid}/categories/{categoryId:guid}/items");

        items.MapGet("/new", New);
        items.MapGet("/bulk/new", NewBulk);
        items.MapGet("/{itemId:guid}/edit", Edit);
        items.MapGet("/{itemId:guid}/fill-forward", FillForwardForm);
        items.MapGet("/{itemId:guid}/realize", RealizeForm);
        items.MapGet("/{itemId:guid}/spent", SpentForm);
        items.MapGet("/{itemId:guid}/delete", DeleteForm);

        items.MapPost("/", Create);
        items.MapPost("/bulk", CreateBulk);
        items.MapPut("/{itemId:guid}", Update);
        items.MapPost("/{itemId:guid}/fill-forward", FillForward);
        items.MapPut("/{itemId:guid}/realize", Realize);
        items.MapPut("/{itemId:guid}/spent", Spent);
        items.MapDelete("/{itemId:guid}", Delete);

        return budgets;
    }

    // ---- forms ---------------------------------------------------------------------------------

    private static async Task<IResult> New(
        Guid budgetId,
        Guid categoryId,
        IUserContext users,
        TimeProvider timeProvider,
        CancellationToken cancellationToken) =>
        Render.Fragment<ItemFormPanel>(await BlankAsync(
            ItemFormMode.Create, budgetId, categoryId, users, timeProvider.GetLocalNow().Month, cancellationToken));

    private static async Task<IResult> NewBulk(
        Guid budgetId,
        Guid categoryId,
        IUserContext users,
        TimeProvider timeProvider,
        CancellationToken cancellationToken) =>
        Render.Fragment<ItemFormPanel>(await BlankAsync(
            ItemFormMode.Bulk, budgetId, categoryId, users, timeProvider.GetLocalNow().Month, cancellationToken));

    private static async Task<IResult> Edit(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        HttpContext context,
        BudgetUi budgets,
        IUserContext users,
        CancellationToken cancellationToken)
    {
        BudgetItemDto? item = await FindAsync(budgets, budgetId, categoryId, itemId, cancellationToken);
        if (item is null)
        {
            return Failed(context, "That item no longer exists.");
        }

        return Render.Fragment<ItemFormPanel>(new ItemFormModel(
            ItemFormMode.Edit,
            budgetId,
            categoryId,
            itemId,
            item.Month.ToString(CultureInfo.InvariantCulture),
            item.BudgetFlow,
            item.PlannedAmount,
            item.RealizedAmount,
            item.SpentAmount,
            [.. item.PayerSplit.Select(payer => new PayerRowModel(payer.UserId.ToString(), payer.Percent.ToString(CultureInfo.InvariantCulture)))],
            [.. item.AttributionSplit.Select(split => new AttributionRowModel(split.Attribution, split.Percent.ToString(CultureInfo.InvariantCulture)))],
            await MembersAsync(users, cancellationToken),
            FormErrors.None));
    }

    private static async Task<IResult> FillForwardForm(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        HttpContext context,
        BudgetUi budgets,
        CancellationToken cancellationToken)
    {
        BudgetItemDto? item = await FindAsync(budgets, budgetId, categoryId, itemId, cancellationToken);
        return item is null
            ? Failed(context, "That item no longer exists.")
            : Render.Fragment<AmountFormPanel>(FillForwardModel(budgetId, categoryId, item, item.PlannedAmount, FormErrors.None));
    }

    private static IResult RealizeForm(Guid budgetId, Guid categoryId, Guid itemId) =>
        Render.Fragment<AmountFormPanel>(RealizeModel(budgetId, categoryId, itemId, amount: null, FormErrors.None));

    private static IResult SpentForm(Guid budgetId, Guid categoryId, Guid itemId) =>
        Render.Fragment<AmountFormPanel>(SpentModel(budgetId, categoryId, itemId, amount: null, FormErrors.None));

    private static IResult DeleteForm(Guid budgetId, Guid categoryId, Guid itemId) =>
        Render.Fragment<DeleteItemPanel>(new DeleteItemModel(ItemUrl(budgetId, categoryId, itemId), Error: null));

    // ---- actions -------------------------------------------------------------------------------

    private static async Task<IResult> Create(
        Guid budgetId,
        Guid categoryId,
        HttpContext context,
        BudgetUi budgets,
        IUserContext users,
        CancellationToken cancellationToken)
    {
        ItemFormModel model = Parse(
            ItemFormMode.Create, budgetId, categoryId, itemId: null,
            await context.Request.ReadFormAsync(cancellationToken),
            await MembersAsync(users, cancellationToken));

        if (!model.Errors.IsValid)
        {
            return Render.Invalid<ItemForm>(model);
        }

        Outcome<BudgetItemDto> outcome = await budgets.CreateItemAsync(
            budgetId,
            categoryId,
            new CreateBudgetItemRequest(
                int.Parse(model.Month, CultureInfo.InvariantCulture),
                model.BudgetFlow,
                model.PlannedAmount!.Value,
                Currency,
                PayerSplit(model),
                AttributionSplit(model)),
            cancellationToken);

        return outcome.IsSuccess
            ? Saved(context, "Budget item created.")
            : Render.Invalid<ItemForm>(model with { Errors = new FormErrors().AddForm(outcome.Error ?? "The item couldn't be saved.") });
    }

    private static async Task<IResult> CreateBulk(
        Guid budgetId,
        Guid categoryId,
        HttpContext context,
        BudgetUi budgets,
        IUserContext users,
        CancellationToken cancellationToken)
    {
        ItemFormModel model = Parse(
            ItemFormMode.Bulk, budgetId, categoryId, itemId: null,
            await context.Request.ReadFormAsync(cancellationToken),
            await MembersAsync(users, cancellationToken));

        if (!model.Errors.IsValid)
        {
            return Render.Invalid<ItemForm>(model);
        }

        Outcome<object> outcome = await budgets.BulkCreateItemsAsync(
            budgetId,
            categoryId,
            new BulkCreateBudgetItemRequest(
                model.BudgetFlow,
                model.PlannedAmount!.Value,
                Currency,
                PayerSplit(model),
                AttributionSplit(model)),
            cancellationToken);

        return outcome.IsSuccess
            ? Saved(context, "Budget items created for all 12 months.")
            : Render.Invalid<ItemForm>(model with { Errors = new FormErrors().AddForm(outcome.Error ?? "The items couldn't be saved.") });
    }

    private static async Task<IResult> Update(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        HttpContext context,
        BudgetUi budgets,
        IUserContext users,
        CancellationToken cancellationToken)
    {
        BudgetItemDto? existing = await FindAsync(budgets, budgetId, categoryId, itemId, cancellationToken);
        if (existing is null)
        {
            return Failed(context, "That item no longer exists.");
        }

        ItemFormModel model = Parse(
            ItemFormMode.Edit, budgetId, categoryId, itemId,
            await context.Request.ReadFormAsync(cancellationToken),
            await MembersAsync(users, cancellationToken)) with
        {
            Month = existing.Month.ToString(CultureInfo.InvariantCulture),
            BudgetFlow = existing.BudgetFlow
        };

        if (!model.Errors.IsValid)
        {
            return Render.Invalid<ItemForm>(model);
        }

        Outcome<BudgetItemDto> outcome = await budgets.UpdateItemAsync(
            budgetId, categoryId, itemId, ChangesTo(existing, model), cancellationToken);

        return outcome.IsSuccess
            ? Saved(context, "Budget item updated.")
            : Render.Invalid<ItemForm>(model with { Errors = new FormErrors().AddForm(outcome.Error ?? "The item couldn't be saved.") });
    }

    private static async Task<IResult> FillForward(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        HttpContext context,
        BudgetUi budgets,
        CancellationToken cancellationToken)
    {
        BudgetItemDto? item = await FindAsync(budgets, budgetId, categoryId, itemId, cancellationToken);
        if (item is null)
        {
            return Failed(context, "That item no longer exists.");
        }

        (decimal? amount, FormErrors errors) = ParseAmount(await context.Request.ReadFormAsync(cancellationToken));
        if (!errors.IsValid)
        {
            return Render.Invalid<AmountForm>(FillForwardModel(budgetId, categoryId, item, amount, errors));
        }

        Outcome<object> outcome = await budgets.FillForwardAsync(
            budgetId,
            categoryId,
            new FillForwardBudgetItemRequest(
                item.Month, item.BudgetFlow, amount!.Value, item.PlannedCurrency, item.PayerSplit, item.AttributionSplit),
            cancellationToken);

        return outcome.IsSuccess
            ? Saved(context, "Budget item filled forward.")
            : Render.Invalid<AmountForm>(FillForwardModel(budgetId, categoryId, item, amount, new FormErrors().AddForm(outcome.Error ?? "The item couldn't be filled forward.")));
    }

    private static async Task<IResult> Realize(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        HttpContext context,
        BudgetUi budgets,
        CancellationToken cancellationToken)
    {
        (decimal? amount, FormErrors errors) = ParseAmount(await context.Request.ReadFormAsync(cancellationToken));
        if (!errors.IsValid)
        {
            return Render.Invalid<AmountForm>(RealizeModel(budgetId, categoryId, itemId, amount, errors));
        }

        Outcome<BudgetItemDto> outcome = await budgets.RealizeItemAsync(budgetId, categoryId, itemId, amount!.Value, cancellationToken);

        return outcome.IsSuccess
            ? Saved(context, "Bill recorded.")
            : Render.Invalid<AmountForm>(RealizeModel(budgetId, categoryId, itemId, amount, new FormErrors().AddForm(outcome.Error ?? "The bill couldn't be recorded.")));
    }

    private static async Task<IResult> Spent(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        HttpContext context,
        BudgetUi budgets,
        CancellationToken cancellationToken)
    {
        (decimal? amount, FormErrors errors) = ParseAmount(await context.Request.ReadFormAsync(cancellationToken));
        if (!errors.IsValid)
        {
            return Render.Invalid<AmountForm>(SpentModel(budgetId, categoryId, itemId, amount, errors));
        }

        Outcome<BudgetItemDto> outcome = await budgets.RecordItemSpentAsync(budgetId, categoryId, itemId, amount!.Value, cancellationToken);

        return outcome.IsSuccess
            ? Saved(context, "Payment recorded.")
            : Render.Invalid<AmountForm>(SpentModel(budgetId, categoryId, itemId, amount, new FormErrors().AddForm(outcome.Error ?? "The payment couldn't be recorded.")));
    }

    private static async Task<IResult> Delete(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        HttpContext context,
        BudgetUi budgets,
        CancellationToken cancellationToken)
    {
        Outcome<object> outcome = await budgets.DeleteItemAsync(budgetId, categoryId, itemId, cancellationToken);
        return outcome.IsSuccess
            ? Saved(context, "Budget item deleted.")
            : Render.Invalid<DeleteItemPanel>(new DeleteItemModel(ItemUrl(budgetId, categoryId, itemId), outcome.Error));
    }

    // ---- parsing -------------------------------------------------------------------------------

    internal static ItemFormModel Parse(
        ItemFormMode mode,
        Guid budgetId,
        Guid categoryId,
        Guid? itemId,
        IFormCollection form,
        IReadOnlyList<MnlOption> members)
    {
        FormErrors errors = new();
        string month = form["month"].ToString().Trim();
        string flow = form["budgetFlow"].ToString();

        if (mode == ItemFormMode.Create
            && !(int.TryParse(month, NumberStyles.None, CultureInfo.InvariantCulture, out int monthNumber) && monthNumber is >= 1 and <= 12))
        {
            errors.Add("month", "Month is required (1–12)");
        }

        if (mode != ItemFormMode.Edit && !Flows.Contains(flow))
        {
            errors.Add("budgetFlow", "Budget flow is required");
        }

        decimal? planned = Amount(form, "plannedAmount");
        if (planned is null)
        {
            errors.Add("plannedAmount", mode == ItemFormMode.Bulk ? "Amount must be positive" : "Planned amount is required");
        }
        else if (mode == ItemFormMode.Bulk && planned <= 0m)
        {
            errors.Add("plannedAmount", "Amount must be positive");
        }

        decimal? realized = OptionalAmount(form, "realizedAmount", errors);
        decimal? spent = OptionalAmount(form, "spentAmount", errors);

        List<PayerRowModel> payers = [.. form["payerUserId"]
            .Zip(form["payerPercent"], (userId, percent) => new PayerRowModel(userId ?? string.Empty, percent ?? string.Empty))];

        List<AttributionRowModel> attributions = [.. form["attribution"]
            .Zip(form["attributionPercent"], (attribution, percent) => new AttributionRowModel(attribution ?? string.Empty, percent ?? string.Empty))];

        ItemFormModel model = new(
            mode, budgetId, categoryId, itemId, month, flow, planned, realized, spent, payers, attributions, members, errors);

        if (payers.Any(payer => !Guid.TryParse(payer.UserId, out _)))
        {
            errors.Add("payerSplit", "Choose a person for every payer row");
        }
        else if (!model.PayerOnTarget)
        {
            errors.Add("payerSplit", $"Payer split must total 100% (currently {model.PayerTotal}%)");
        }

        if (attributions.Any(split => !Attributions.Contains(split.Attribution)))
        {
            errors.Add("attributionSplit", "Choose an attribution for every row");
        }
        else if (!model.AttributionOnTarget)
        {
            errors.Add("attributionSplit", $"Attribution split must total 100% (currently {model.AttributionTotal}%)");
        }

        return model;
    }

    private static (decimal? Amount, FormErrors Errors) ParseAmount(IFormCollection form)
    {
        decimal? amount = Amount(form, "amount");
        FormErrors errors = new();

        if (amount is null or <= 0m)
        {
            errors.Add("amount", "Amount is required and must be positive");
        }

        return (amount, errors);
    }

    private static decimal? Amount(IFormCollection form, string field) =>
        MoneyFormat.TryParse(form[field], out decimal value) ? value : null;

    private static decimal? OptionalAmount(IFormCollection form, string field, FormErrors errors)
    {
        string text = form[field].ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (MoneyFormat.TryParse(text, out decimal value))
        {
            return value;
        }

        errors.Add(field, "Enter an amount, such as 1250.50");
        return null;
    }

    // Only what changed is sent, because the API treats every field it receives as an edit
    private static UpdateBudgetItemRequest ChangesTo(BudgetItemDto existing, ItemFormModel model)
    {
        List<PayerAllocationDto> payers = PayerSplit(model);
        List<AttributionAllocationDto> attributions = AttributionSplit(model);

        bool plannedChanged = model.PlannedAmount != existing.PlannedAmount;
        bool realizedChanged = model.RealizedAmount is not null && model.RealizedAmount != existing.RealizedAmount;
        bool spentChanged = model.SpentAmount is not null && model.SpentAmount != existing.SpentAmount;

        return new UpdateBudgetItemRequest(
            PlannedAmount: plannedChanged ? model.PlannedAmount : null,
            PlannedCurrency: plannedChanged ? Currency : null,
            RealizedAmount: realizedChanged ? model.RealizedAmount : null,
            RealizedCurrency: realizedChanged ? Currency : null,
            SpentAmount: spentChanged ? model.SpentAmount : null,
            SpentCurrency: spentChanged ? Currency : null,
            PayerSplit: payers.SequenceEqual(existing.PayerSplit) ? null : payers,
            AttributionSplit: attributions.SequenceEqual(existing.AttributionSplit) ? null : attributions);
    }

    private static List<PayerAllocationDto> PayerSplit(ItemFormModel model) =>
        [.. model.Payers.Select(payer => new PayerAllocationDto(
            Guid.Parse(payer.UserId),
            int.Parse(payer.Percent, CultureInfo.InvariantCulture)))];

    private static List<AttributionAllocationDto> AttributionSplit(ItemFormModel model) =>
        [.. model.Attributions.Select(split => new AttributionAllocationDto(
            split.Attribution,
            int.Parse(split.Percent, CultureInfo.InvariantCulture)))];

    // ---- models --------------------------------------------------------------------------------

    private static async Task<ItemFormModel> BlankAsync(
        ItemFormMode mode,
        Guid budgetId,
        Guid categoryId,
        IUserContext users,
        int currentMonth,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<MnlOption> members = await MembersAsync(users, cancellationToken);

        // Start with the commonest split: one payer and the main household, each at 100%
        return new ItemFormModel(
            mode,
            budgetId,
            categoryId,
            ItemId: null,
            Month: currentMonth.ToString(CultureInfo.InvariantCulture),
            BudgetFlow: string.Empty,
            PlannedAmount: null,
            RealizedAmount: null,
            SpentAmount: null,
            Payers: members.Count > 0 ? [new PayerRowModel(members[0].Value, "100")] : [],
            Attributions: [new AttributionRowModel("Main", "100")],
            members,
            FormErrors.None);
    }

    private static async Task<IReadOnlyList<MnlOption>> MembersAsync(IUserContext users, CancellationToken cancellationToken) =>
        [.. (await users.Users
                .AsNoTracking()
                .OrderBy(user => user.DisplayName)
                .Select(user => new { user.Id, user.DisplayName })
                .ToListAsync(cancellationToken))
            .Select(user => new MnlOption(user.Id.Value.ToString(), user.DisplayName))];

    private static AmountFormModel FillForwardModel(Guid budgetId, Guid categoryId, BudgetItemDto item, decimal? amount, FormErrors errors) => new(
        $"Fill forward from {BudgetPresentation.MonthName(item.Month)}",
        $"Copy {MoneyFormat.Format(item.PlannedAmount, item.PlannedCurrency)} from {BudgetPresentation.MonthName(item.Month)} through December, with the same splits.",
        "Amount",
        "Fill forward",
        $"{ItemUrl(budgetId, categoryId, item.Id)}/fill-forward",
        amount,
        errors)
    {
        Method = "post"
    };

    private static AmountFormModel RealizeModel(Guid budgetId, Guid categoryId, Guid itemId, decimal? amount, FormErrors errors) => new(
        "Record bill",
        "Enter the amount on the bill or payslip once it arrives.",
        "Bill amount",
        "Save",
        $"{ItemUrl(budgetId, categoryId, itemId)}/realize",
        amount,
        errors);

    private static AmountFormModel SpentModel(Guid budgetId, Guid categoryId, Guid itemId, decimal? amount, FormErrors errors) => new(
        "Record payment",
        "Enter the amount that was actually paid.",
        "Payment amount",
        "Save",
        $"{ItemUrl(budgetId, categoryId, itemId)}/spent",
        amount,
        errors);

    private static string ItemUrl(Guid budgetId, Guid categoryId, Guid itemId) =>
        $"/budgets/{budgetId}/categories/{categoryId}/items/{itemId}";

    private static async Task<BudgetItemDto?> FindAsync(
        BudgetUi budgets,
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        Outcome<List<BudgetItemDto>> items = await budgets.ListItemsAsync(budgetId, categoryId, cancellationToken);
        return items.Value?.FirstOrDefault(item => item.Id == itemId);
    }

    private static IResult Saved(HttpContext context, string message) =>
        new HxResponse()
            .Toast(message)
            .ClosePanel()
            .Raise(UiEvents.ItemsChanged)
            .NoContent(context);

    private static IResult Failed(HttpContext context, string error) =>
        new HxResponse().Toast(error, MnlTone.Error).NoContent(context);
}
