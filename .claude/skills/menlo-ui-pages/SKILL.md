---
name: menlo-ui-pages
description: Build and change Menlo's server-rendered UI - pages, fragments, forms, panels and the endpoints behind them in src/api/Menlo.Api/Ui. Use this skill whenever you add or change a screen, a form, a list, a panel, a filter, a navigation item, a toast or anything a person sees or clicks in Menlo, whenever you touch a .razor file under Menlo.Api, or whenever someone asks how the UI separates pages, components, utilities, state and behaviour. Use it even if the request only says "add a button to the budget page", "show X on the home page" or "add a form for Y". For changes inside the design system itself (src/ui/Menlo.Ui), use menlo-ui-components as well.
---

# Menlo UI pages

Menlo's UI is HTML rendered by the API: static Razor components returned from minimal API endpoints, with htmx swapping parts of the page. There is no SPA, no Node toolchain and no client state. Read `docs/explanations/ui-architecture.md` once if you haven't; this skill is the working checklist.

## Where things go

| You are writing | Put it in | Named |
| --- | --- | --- |
| A screen at an address | `src/api/Menlo.Api/Ui/<Feature>/` | `<Thing>Page.razor` |
| A part the server sends alone: a form, a panel, a list | same folder | `<Thing>.razor`, `<Thing>Panel.razor`, `<Thing>Form.razor` |
| The data a page or fragment shows | same folder | `<Thing>Model` records, often in `<Feature>Models.cs` |
| Loading, rules and the response | same folder | `<Feature>Endpoints.cs`, a static class |
| Formatting or tone rules shared by a feature | same folder | `<Feature>Presentation.cs`, static and pure |
| Reusable presentation with no Menlo knowledge | `src/ui/Menlo.Ui` | `Mnl*` - see `menlo-ui-components` |

Map new endpoints from `UiEndpoints.MapMenloUi` so they get the UI marker (sign-in redirect, anti-forgery, user stamping).

## The separation rule

- **State** is a view model: an immutable record holding everything the markup shows, already formatted as text and `MnlTone`s. `"R 1 700.00 spent of R 2 000.00"`, not two decimals.
- **Behaviour** is the endpoint: read the request, call the handler behind the JSON endpoint (through a `<Feature>Ui` adapter returning `Outcome<T>`), apply rules, build the model, choose the response.
- **Presentation** is the `.razor` file and its `.razor.css`: model in, markup out.

A `.razor` file under `Menlo.Api/Ui` must not contain `@inject`, a rule (`percent >= 95`), number or date formatting, or a database or handler call. If you need one, add a property to the view model. `UiConventionTests` fails the build on `@inject`, inline `style`, `<script>` and `hx-on`.

## Writing a page

1. Model: `public sealed record XPageModel(...)` with formatted values and an error string for the load failure.
2. Endpoint: `app.MapGet("/x", Page).RequireAuthorization(MenloPolicies.CanViewBudget)`; end with `Render.Page<XPage>(context, model)`.
3. Page: wrap everything in `<AppPage Page="Page" Title="...">`; parameters are `PageContext Page` and `XPageModel Model`, both `[Parameter, EditorRequired]`.
4. Layout from `MnlStack`, `MnlCluster`, `MnlGrid`, `MnlSplit`; text from `MnlHeading`, `MnlText`, `MnlEyebrow`. A page-level `.razor.css` only for what those can't do, using `--mnl-*` tokens.
5. Show a load failure with `MnlAlert`, an empty list with `MnlEmptyState`.
6. Put `data-testid` on anything a test will find.

## State in the URL

A page's views, filters and selection are query-string parameters, read into a record with defaults for bad input. Give it a `Url(...)` method and build every link from it. Links that change the state keep a real `href` and add htmx to swap only the region. Keep the attributes in a small `<Feature>Region` helper that returns them as a dictionary:

```razor
<MnlButton Href="@row.ToggleUrl" @attributes="ThingRegion.GoTo(row.ToggleUrl)">View items</MnlButton>
```

`GoTo(url)` sets `hx-get` to the url, `hx-target` and `hx-select` to the region, `hx-swap="outerHTML"`, `hx-replace-url="true"` and `hx-sync`.

Every request that swaps the same region must carry `hx-sync="#region:replace"` so a slow reload can't overwrite a newer one. A text input that triggers a swap as you type needs `hx-preserve="true"` and its own `hx-trigger="input changed delay:300ms"`.

## Forms

Two components and an endpoint per action:

- `<Thing>Panel.razor`: `<MnlPanel>` with a header, wrapping the form. Returned by a GET (`Render.Fragment`) and opened by a button with `hx-get="..." hx-target="@MnlPanel.HostSelector"`.
- `<Thing>Form.razor`: `<form hx-post|hx-put="@Model.Action" hx-target="this" hx-swap="outerHTML" novalidate>` inside `MnlFormLayout`. Each control in `MnlFormField` with `Error="@Model.Errors.For("name")"` and the control's `Invalid` set. Cancel is a ghost button with `data-mnl-close`; submit is `Type="MnlButtonType.Submit"` and comes last.
- Submit endpoint:
  1. Parse the form into the model with `FormErrors` (a static `Parse` function). Validation is server-side only.
  2. Invalid: `return Render.Invalid<ThingForm>(model);` (422, the form alone, which replaces itself in the open panel).
  3. Call the handler. Refused: `Render.Invalid` again with `FormErrors().AddForm(outcome.Error)` - or `.Add("name", ...)` for a conflict on a field.
  4. Saved: `return new HxResponse().Toast("Thing created.").ClosePanel().Raise(UiEvents.ThingsChanged).NoContent(context);`
  5. Saved and moving to another page: `Flash.Set(context, "...")` then `return Navigate.To(context, url);`

Regions that show the changed data listen for the event (`hx-get=""`, `hx-trigger="things-changed from:body"`, `hx-select` the region). The endpoint never knows which regions show its data. Add new event names to `UiEvents`.

Repeated rows (splits, line items) use `data-mnl-rows`, `data-mnl-rows-list`, a `<template data-mnl-rows-template>`, `data-mnl-rows-add`/`-remove` and an `<output data-mnl-rows-total data-target="100">`. Same-named inputs arrive as parallel lists in `IFormCollection`; zip them. The server still checks the total.

## Permissions

Endpoints that change data use `MenloPolicies.CanEditBudget`; read-only pages `CanViewBudget`. Put `bool CanEdit` in the model (from `IAuthorizationService`) and leave edit controls out of the markup for readers - don't just disable them.

## Things that will bite you

- **CSP**: `script-src 'self'; style-src 'self'` everywhere. No `style=""` - vary appearance with `data-*` attributes and classes in `.razor.css`.
- **htmx inheritance is off.** An `hx-*` attribute only affects its own element. Don't expect a parent's `hx-target` to apply to children.
- **422 swaps, other 4xx/5xx don't.** A failure that isn't a form error should be a 204 with an error toast (`HxResponse().Toast(msg, MnlTone.Error)`).
- **Razor reserves `<text>`.** In SVG use `MnlSvgText`.
- A `@foreach`/`@for` variable used inside a child component's content must be copied to a local first (BL0014).
- Don't write JavaScript for a feature. If it needs data, it is an htmx request; if it is pure presentation and reusable, it belongs in `menlo.js` via a `data-mnl-*` attribute (see `menlo-ui-components`).

## Testing

- Page or form: a test in `src/api/Menlo.Api.Tests/Ui` that requests it (`UiHtml.HtmxAsync` for fragments) and reads the HTML with `DocumentAsync()`/`ByTestId()`; assert status, markup and `Triggers()`. Pages that need a real database use `PostgresApiFixture`.
- Only a browser can show it (swap, panel, toast, theme, scroll): a test in `src/api/Menlo.Api.Tests/E2E` with `BrowserFixture`, ending with `app.BrowserErrors.ShouldBeEmpty()`.
- Pure presentation rules (`<Feature>Presentation`, query records): plain unit tests.
- Changed `.cs` files under `Menlo.Api` must stay at 70% line coverage.

Then run the app (`aspire start`), open the page, and check it in light and dark mode and at 390px wide.

## Done when

- [ ] No rules, formatting, `@inject` or inline style/script in any `.razor` file you touched
- [ ] State that should survive a reload is in the URL
- [ ] Forms validate on the server and re-render with errors on 422
- [ ] Saves raise an event instead of naming the regions to refresh
- [ ] Readers see no edit controls
- [ ] Tests at the right level, and the page checked in a browser in both themes
