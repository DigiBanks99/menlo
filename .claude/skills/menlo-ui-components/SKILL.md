---
name: menlo-ui-components
description: Use, build and change Menlo's design system - the Menlo.Ui Razor component library in src/ui/Menlo.Ui (Mnl* components, layouts, charts, tokens, menlo.js and the /design gallery). Use this skill whenever you choose which component to use for something, compose a layout, pick colours, spacing or typography, add or change a component, token, icon, chart or client behaviour, or touch the Catppuccin theme. Use it even for small requests like "make this a card", "add an icon", "this needs a warning colour" or "the spacing looks off", and alongside menlo-ui-pages whenever a page needs something the library doesn't have yet.
---

# Menlo UI components

`src/ui/Menlo.Ui` is Menlo's design system: tokens, components, layouts, charts, a tiny client script and a gallery at `/design`. It knows nothing about budgets, routes, users or the API. The full catalogue is `docs/reference/design-system.md`; open `/design` on a running app to see every component.

## Choosing a component

Reach for these before writing markup or CSS:

| You need | Use |
| --- | --- |
| Vertical spacing | `MnlStack Gap=` |
| A row that wraps (buttons, title + badge) | `MnlCluster` (`Justify`, `StackOnMobile` for action rows) |
| Equal columns | `MnlGrid Columns=` (`Auto`, `Two`, `Three`, `Four`) |
| Main + side column | `MnlSplit Ratio=` |
| A heading / body text / section label | `MnlHeading Level Size`, `MnlText Size Tone Weight`, `MnlEyebrow` |
| A grouped surface | `MnlCard` (`Header`, `Footer`, `Interactive`) |
| The banner at the top of a page | `MnlPageHeader` (`Hero`, overlap cards as child content) |
| An action | `MnlButton` (`Href` for navigation; one `Primary` per view) |
| A status | `MnlBadge Tone` |
| A descriptive label | `MnlTag Tone` |
| A message that stays | `MnlAlert`; one that goes: a toast from the server |
| Nothing to show | `MnlEmptyState` |
| A row in a list | `MnlListItem` (`Leading`, `Trailing`) |
| An expandable row | `MnlDisclosure` |
| A figure | `MnlStat`; a share: `MnlProgress`; a headline %: `MnlRadialChart` |
| Charts | `MnlBarChart`, `MnlLineChart`, `MnlDonutChart` |
| A modal task | `MnlPanel` (`Mode=Dialog` for confirmations) |
| Form controls | `MnlFormField` around `MnlInput`, `MnlSelect`, `MnlAmountInput`, `MnlCheckbox`; `MnlFormLayout` for the form |
| Exclusive views | `MnlSegmented` |

Badge vs tag: a badge answers "what state is it in?" (Active, Draft, Over budget); a tag describes it (Expense, Rental, 42%).

Tone carries meaning, not decoration: `Success` good, `Warning` needs attention soon, `Error` failed or over, `Info` informational, `Accent` brand emphasis, `Neutral` none.

## Using components well

- Every component passes unknown attributes to its root: `data-testid`, `id`, `aria-*`, `hx-*` and an extra `class` all work. Put htmx on the component, not a wrapper.
- `MnlHeading` `Level` follows the document outline; `Size` follows the design. Set them separately.
- `MnlButton` inside a form submits only with `Type="MnlButtonType.Submit"`. While an htmx request from it (or its form) runs it shows a spinner.
- Pass enums, not strings: `Tone="MnlTone.Warning"`, `Gap="MnlGap.Lg"`.
- Pass formatted text. Components don't format money or dates; `MoneyFormat` does.
- Icons come from `MnlIconName`. A lone icon in a control needs `Label`.

## Tokens and styles

- Colours, sizes and shadows come from `wwwroot/css/tokens.css`. Use the **role** tokens (`--mnl-surface`, `--mnl-text`, `--mnl-subtext`, `--mnl-accent`, `--mnl-success`, `--mnl-*-soft`, `--mnl-space-*`, `--mnl-radius-*`). Use `--ctp-*` palette tokens only to define a new role.
- Never write a hex value or pixel spacing in a component or page. Add a token first.
- Light is Catppuccin Latte, dark is Mocha, via `light-dark(latte, mocha)` on each palette token. A new role that differs by theme uses `light-dark()` too. Text on an accent or status fill is `--mnl-on-accent` in both themes.
- Component CSS lives in `Thing.razor.css` (scoped). Style variants with `data-*` attributes (`.mnl-button[data-variant='ghost']`), set from enums via `Css.Token(...)`.
- No inline `style` anywhere: the Content Security Policy blocks it. Vary sizes with attributes the browser understands (`<progress value>`, SVG `width`/`height`) or a small set of `data-*` steps.
- Respect `prefers-reduced-motion` (base.css already shortens transitions).

## Adding or changing a component

1. `Components/MnlThing.razor` + `MnlThing.razor.css`, `@inherits MnlComponent`, root element gets `@attributes="Attributes"` and `class="@RootClass("mnl-thing")"`.
2. A comment at the top saying when to use it (and what to use instead).
3. Parameters are typed, documented with `<summary>`, and `[EditorRequired]` when needed. Use the shared enums in `Components/Enums.cs`; add a new enum there only for a new axis.
4. No services, no `@inject`, no I/O, no knowledge of Menlo's domain. Pure presentation.
5. Add an example to the matching `Gallery/Gallery*.razor` section with its markup in a `Code` constant.
6. Add a test in `src/ui/Menlo.Ui.Tests/Components` using `Rendered.ComponentAsync`. `GalleryTests` then also checks it has no inline style/script, labelled controls and unique ids.
7. Update `docs/reference/design-system.md`.

Layouts go in `Layouts/`, charts in `Charts/` (geometry in `ChartMath`, pure and tested), pure helpers in `Utilities/`.

## Client behaviour (`wwwroot/js/menlo.js`)

Only presentation behaviour that needs no data and is reusable: theme, panels, toasts, repeated rows. It hooks onto `data-mnl-*` attributes with delegated listeners and holds no state beyond the DOM. Before adding to it, ask whether an htmx request would do; usually it would. Never add inline handlers or `hx-on` (CSP). Document a new attribute in the file header and in `design-system.md`.

## Icons and vendored files

- New icon: add to `MnlIconName` and its Lucide SVG children to `MnlIconShapes` (copy from lucide.dev, 24x24, stroke). The gallery shows all icons automatically.
- htmx, its SSE extension and the fonts are vendored in `wwwroot/vendor` and `wwwroot/fonts`. To update, replace the file and the version in `wwwroot/vendor/README.md`. Dependabot doesn't track them.

## Checking your work

- `dotnet test --project src/ui/Menlo.Ui.Tests`
- Open `/design` on the running app (`aspire start`), check the example in light and dark mode and at 390px wide, and that the console is clean.
