# How-to: Add a Page to the UI

This guide adds a page, then a part of it that refreshes, then a form in a panel. It assumes you have read [UI Architecture](../explanations/ui-architecture.md). The `menlo-ui-pages` skill in `.claude/skills` holds the same rules for agents.

A page is three files in a feature folder under `src/api/Menlo.Api/Ui/`:

| File | Holds |
| --- | --- |
| `ThingPageModel.cs` | State: an immutable record, already formatted. |
| `ThingEndpoints.cs` | Behaviour: loads data, applies rules, builds the model, chooses the response. |
| `ThingPage.razor` | Presentation: turns the model into markup. |

## Add a page

### 1. Write the view model

Give the page everything it shows, as text and tones, not raw data:

```csharp
public sealed record SavingsPageModel(IReadOnlyList<SavingsGoalModel> Goals, string? LoadError);

public sealed record SavingsGoalModel(Guid Id, string Name, string Saved, decimal Percent, MnlTone Tone);
```

### 2. Write the endpoint

```csharp
internal static class SavingsEndpoints
{
    public static IEndpointRouteBuilder MapSavingsPage(this IEndpointRouteBuilder app)
    {
        app.MapGet("/savings", Page).RequireAuthorization(MenloPolicies.CanViewBudget);
        return app;
    }

    private static async Task<IResult> Page(HttpContext context, SavingsUi savings, CancellationToken cancellationToken)
    {
        Outcome<List<SavingsGoalDto>> outcome = await savings.ListGoalsAsync(cancellationToken);

        SavingsPageModel model = outcome is { IsSuccess: true, Value: { } goals }
            ? new SavingsPageModel([.. goals.Select(ToModel)], LoadError: null)
            : new SavingsPageModel([], outcome.Error);

        return Render.Page<SavingsPage>(context, model);
    }
}
```

- Call the handler behind the JSON endpoint through a `<Feature>Ui` adapter, such as `SavingsUi`, and read its result as an `Outcome<T>`. Don't query the database from a UI endpoint.
- Decide tones and text here or in the model (a `SavingsPresentation.ProgressTone`, say), not in the `.razor` file.
- Map the page in `UiEndpoints.MapMenloUi`. The group it is mapped in marks it as a UI endpoint, which turns on the sign-in redirect and the anti-forgery check.

### 3. Write the page

```razor
<AppPage Page="Page" Title="Savings">
    <MnlStack Gap="MnlGap.Xl">
        <MnlPageHeader>
            <Hero>
                <MnlHeading Level="1">Savings</MnlHeading>
            </Hero>
        </MnlPageHeader>

        @if (Model.LoadError is not null)
        {
            <MnlAlert Tone="MnlTone.Error" Title="We couldn't load your savings.">@Model.LoadError</MnlAlert>
        }
        else
        {
            <MnlGrid Columns="MnlGridColumns.Three">
                @foreach (SavingsGoalModel goal in Model.Goals)
                {
                    <MnlCard data-testid="@($"goal-{goal.Id}")">
                        <MnlStack>
                            <MnlHeading Level="2" Size="MnlTextSize.Xl">@goal.Name</MnlHeading>
                            <MnlProgress Value="goal.Percent" Tone="goal.Tone" AriaLabel="@($"{goal.Name} progress")" />
                            <MnlText Tone="MnlTextTone.Subtle">@goal.Saved</MnlText>
                        </MnlStack>
                    </MnlCard>
                }
            </MnlGrid>
        }
    </MnlStack>
</AppPage>

@code {
    [Parameter, EditorRequired]
    public PageContext Page { get; set; } = null!;

    [Parameter, EditorRequired]
    public SavingsPageModel Model { get; set; } = null!;
}
```

- Build the layout from `MnlStack`, `MnlCluster`, `MnlGrid` and `MnlSplit`. Add a `.razor.css` file only for what they can't do.
- Give elements a test needs a `data-testid`.
- No `@inject`, no inline `style`, no `<script>`.

### 4. Add it to the navigation

To give the page a navigation item, add an `MnlNavItem` to `PageContext.NavItemsFor`. Leave it out while the feature's flag is off, as the Budgets item does.

## Keep state in the URL

When a page has views, filters or a selection, make them query-string parameters, read into a record such as `SavingsQuery`:

1. Read the state from `context.Request.Query`, with a default for anything missing or invalid.
2. Give the state a `Url(...)` method, and build every link in the view model from it.
3. Wrap the part of the page that changes in an element with an id.
4. On each link, ask for the page and swap that element:

```razor
<MnlButton Href="@Model.NextMonthUrl" @attributes="SavingsRegion.GoTo(Model.NextMonthUrl)">Next Month</MnlButton>
```

`SavingsRegion.GoTo`, a small helper that returns a dictionary, adds `hx-get`, `hx-target`, `hx-select`, `hx-swap="outerHTML"`, `hx-replace-url` and `hx-sync`. The plain `href` still works without script.

## Reload when data changes

An endpoint that changes data raises an event. The region listens and reloads itself at the current address:

```razor
<div id="@SavingsRegion.Id" @attributes="SavingsRegion.ReloadOnChange()">
```

Add a name to `UiEvents` for a new kind of data, and raise it with `HxResponse.Raise`.

## Add a form in a panel

A form is two components and three endpoints.

**The form** replaces itself with the server's answer:

```razor
<form hx-post="@Model.Action" hx-target="this" hx-swap="outerHTML" novalidate>
    <MnlFormLayout>
        <ChildContent>
            <MnlFormField Label="Name" For="name" Required="true" Error="@Model.Errors.For("name")">
                <MnlInput Name="name" Value="@Model.Name" Invalid="@Model.Errors.Has("name")" />
            </MnlFormField>
            @if (Model.Errors.Form is not null)
            {
                <MnlAlert Tone="MnlTone.Error">@Model.Errors.Form</MnlAlert>
            }
        </ChildContent>
        <Actions>
            <MnlButton Variant="MnlButtonVariant.Ghost" data-mnl-close>Cancel</MnlButton>
            <MnlButton Type="MnlButtonType.Submit">Create</MnlButton>
        </Actions>
    </MnlFormLayout>
</form>
```

**The panel** wraps it in `<MnlPanel>` with a header.

**The endpoints:**

```csharp
// Opens the panel
private static IResult New(Guid budgetId) =>
    Render.Fragment<GoalFormPanel>(GoalFormModel.Blank(budgetId));

// Handles the submit
private static async Task<IResult> Create(Guid budgetId, HttpContext context, SavingsUi savings, CancellationToken cancellationToken)
{
    GoalFormModel model = Parse(budgetId, await context.Request.ReadFormAsync(cancellationToken));
    if (!model.Errors.IsValid)
    {
        return Render.Invalid<GoalForm>(model);          // 422: the form, with its errors
    }

    Outcome<GoalDto> outcome = await savings.CreateGoalAsync(/* ... */);
    return outcome.IsSuccess
        ? new HxResponse().Toast("Goal created.").ClosePanel().Raise(UiEvents.GoalsChanged).NoContent(context)
        : Render.Invalid<GoalForm>(model with { Errors = new FormErrors().AddForm(outcome.Error!) });
}
```

**The button that opens it** asks for the panel and puts it in the panel host:

```razor
<MnlButton hx-get="@($"/savings/{Model.BudgetId}/goals/new")" hx-target="@MnlPanel.HostSelector">Add goal</MnlButton>
```

- Validate on the server, in a `Parse` function that returns the model with its `FormErrors`. Browser validation is off (`novalidate`), so there is one set of messages.
- Return the form alone on a validation failure (`Render.Invalid<GoalForm>`), not the panel: the form swaps itself inside the open panel.
- After a redirect to another page, use `Flash.Set` and `Navigate.To` in place of `HxResponse`.

## Test it

Add tests under `src/api/Menlo.Api.Tests/Ui`: one that asks for the page and reads the HTML, and one per form for the invalid and valid submits. For behaviour that only a browser shows, add a test under `E2E`. See [How to write .NET tests](testing-howto.md#test-the-ui).

## Check it in the browser

Run the app with `aspire run`, open the page, and check it in both themes and at phone width. The Content Security Policy is the same in Development as in production, so an inline style or script shows up as a console error straight away.
