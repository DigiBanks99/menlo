using Menlo.Api.Budget.Categories;
using Menlo.Api.Ui.Shared;
using Menlo.Ui.Components;

namespace Menlo.Api.Ui.Budgets;

/// <summary>
/// Creating, editing, deleting and restoring categories from the budget detail page.
/// The form opens in a panel. A successful change answers with no content and the
/// "categories-changed" event; the regions of the page that show categories reload themselves.
/// </summary>
internal static class CategoryUiEndpoints
{
    private static readonly string[] Flows = ["Income", "Expense", "Both"];
    private static readonly string[] Attributions = ["Main", "Rental", "ServiceProvider"];

    public static RouteGroupBuilder MapCategoryForms(this RouteGroupBuilder budgets)
    {
        RouteGroupBuilder categories = budgets.MapGroup("/{budgetId:guid}/categories");

        categories.MapGet("/new", New);
        categories.MapGet("/{categoryId:guid}/edit", Edit);
        categories.MapPost("/", Create);
        categories.MapPut("/{categoryId:guid}", Update);
        categories.MapDelete("/{categoryId:guid}", Delete);
        categories.MapPut("/{categoryId:guid}/restore", Restore);

        return budgets;
    }

    private static IResult New(Guid budgetId, Guid? parentId = null) =>
        Render.Fragment<CategoryFormPanel>(new CategoryFormModel(
            budgetId,
            CategoryId: null,
            parentId,
            Name: string.Empty,
            Description: string.Empty,
            BudgetFlow: string.Empty,
            Attribution: string.Empty,
            IncomeContributor: string.Empty,
            ResponsiblePayer: string.Empty,
            FormErrors.None));

    private static async Task<IResult> Edit(
        Guid budgetId,
        Guid categoryId,
        HttpContext context,
        BudgetUi budgets,
        CancellationToken cancellationToken)
    {
        Outcome<CategoryDto> outcome = await budgets.GetCategoryAsync(budgetId, categoryId, cancellationToken);
        if (outcome is not { IsSuccess: true, Value: { } category })
        {
            return Failed(context, outcome.Error);
        }

        return Render.Fragment<CategoryFormPanel>(new CategoryFormModel(
            budgetId,
            category.Id,
            ParentId: null,
            category.Name,
            category.Description ?? string.Empty,
            category.BudgetFlow,
            category.Attribution ?? string.Empty,
            category.IncomeContributor ?? string.Empty,
            category.ResponsiblePayer ?? string.Empty,
            FormErrors.None));
    }

    private static async Task<IResult> Create(
        Guid budgetId,
        HttpContext context,
        BudgetUi budgets,
        CancellationToken cancellationToken)
    {
        IFormCollection form = await context.Request.ReadFormAsync(cancellationToken);
        Guid? parentId = Guid.TryParse(form["parentId"], out Guid parent) ? parent : null;
        CategoryFormModel model = Parse(budgetId, categoryId: null, parentId, form);

        if (!model.Errors.IsValid)
        {
            return Render.Invalid<CategoryForm>(model);
        }

        Outcome<CategoryDto> outcome = await budgets.CreateCategoryAsync(
            budgetId,
            new CreateCategoryRequest(
                model.Name,
                model.BudgetFlow,
                parentId,
                NullIfBlank(model.Description),
                NullIfBlank(model.Attribution),
                NullIfBlank(model.IncomeContributor),
                NullIfBlank(model.ResponsiblePayer)),
            cancellationToken);

        return outcome.IsSuccess ? Saved(context, "Category created.") : Rejected(model, outcome);
    }

    private static async Task<IResult> Update(
        Guid budgetId,
        Guid categoryId,
        HttpContext context,
        BudgetUi budgets,
        CancellationToken cancellationToken)
    {
        IFormCollection form = await context.Request.ReadFormAsync(cancellationToken);
        CategoryFormModel model = Parse(budgetId, categoryId, parentId: null, form);

        if (!model.Errors.IsValid)
        {
            return Render.Invalid<CategoryForm>(model);
        }

        Outcome<CategoryDto> outcome = await budgets.UpdateCategoryAsync(
            budgetId,
            categoryId,
            new UpdateCategoryRequest(
                model.Name,
                model.BudgetFlow,
                NullIfBlank(model.Description),
                NullIfBlank(model.Attribution),
                NullIfBlank(model.IncomeContributor),
                NullIfBlank(model.ResponsiblePayer)),
            cancellationToken);

        return outcome.IsSuccess ? Saved(context, "Category updated.") : Rejected(model, outcome);
    }

    private static async Task<IResult> Delete(
        Guid budgetId,
        Guid categoryId,
        HttpContext context,
        BudgetUi budgets,
        CancellationToken cancellationToken)
    {
        Outcome<object> outcome = await budgets.DeleteCategoryAsync(budgetId, categoryId, cancellationToken);
        return outcome.IsSuccess ? Saved(context, "Category deleted.") : Failed(context, outcome.Error);
    }

    private static async Task<IResult> Restore(
        Guid budgetId,
        Guid categoryId,
        HttpContext context,
        BudgetUi budgets,
        CancellationToken cancellationToken)
    {
        Outcome<object> outcome = await budgets.RestoreCategoryAsync(budgetId, categoryId, cancellationToken);
        return outcome.IsSuccess ? Saved(context, "Category restored.") : Failed(context, outcome.Error);
    }

    private static CategoryFormModel Parse(Guid budgetId, Guid? categoryId, Guid? parentId, IFormCollection form)
    {
        FormErrors errors = new();
        string name = form["name"].ToString().Trim();
        string flow = form["budgetFlow"].ToString();
        string attribution = form["attribution"].ToString();

        if (name.Length == 0)
        {
            errors.Add("name", "Name is required");
        }

        if (!Flows.Contains(flow))
        {
            errors.Add("budgetFlow", "Budget flow is required");
        }

        if (attribution.Length > 0 && !Attributions.Contains(attribution))
        {
            errors.Add("attribution", "Choose an attribution from the list");
        }

        return new CategoryFormModel(
            budgetId,
            categoryId,
            parentId,
            name,
            form["description"].ToString().Trim(),
            flow,
            attribution,
            form["incomeContributor"].ToString().Trim(),
            form["responsiblePayer"].ToString().Trim(),
            errors);
    }

    private static IResult Saved(HttpContext context, string message) =>
        new HxResponse()
            .Toast(message)
            .ClosePanel()
            .Raise(UiEvents.CategoriesChanged)
            .NoContent(context);

    // The server refused the change: a duplicate name belongs on the name field, anything else on the form
    private static IResult Rejected(CategoryFormModel model, Outcome<CategoryDto> outcome)
    {
        string message = outcome.Error ?? "The category couldn't be saved.";
        FormErrors errors = outcome.IsConflict ? new FormErrors().Add("name", message) : new FormErrors().AddForm(message);
        return Render.Invalid<CategoryForm>(model with { Errors = errors });
    }

    private static IResult Failed(HttpContext context, string? error) =>
        new HxResponse()
            .Toast(error ?? "Something went wrong. Please try again.", MnlTone.Error)
            .NoContent(context);

    private static string? NullIfBlank(string value) => value.Length == 0 ? null : value;
}
