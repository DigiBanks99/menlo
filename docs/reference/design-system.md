# Design System Reference

The design system is the `Menlo.Ui` library (`src/ui/Menlo.Ui`): tokens, components, layouts and charts. Pages are built from it. It has no dependency on the rest of Menlo.

To see every component with its variants and markup, run the app and open `/design`. The gallery is mapped in Development, or anywhere `Features:DesignGallery` is `true`, and needs no sign-in.

## Tokens

Every colour, size and shadow is a CSS custom property in `wwwroot/css/tokens.css`. There are two tiers:

| Tier | Example | Use |
| --- | --- | --- |
| Palette | `--ctp-pink`, `--ctp-base` | The 26 [Catppuccin](https://catppuccin.com) colours: Latte in light mode, Mocha in dark. Use one only to define a role. |
| Role | `--mnl-surface`, `--mnl-text`, `--mnl-accent` | What components use. A role says what a colour is for, so it stays right in both themes. |

| Group | Tokens |
| --- | --- |
| Surfaces | `--mnl-bg`, `--mnl-surface`, `--mnl-surface-alt`, `--mnl-surface-muted`, `--mnl-border`, `--mnl-border-subtle` |
| Text | `--mnl-text`, `--mnl-subtext`, `--mnl-on-accent` |
| Accent and status | `--mnl-accent`, `--mnl-accent-strong`, `--mnl-success`, `--mnl-warning`, `--mnl-error`, `--mnl-info`, each with a `-soft` tint; `--mnl-gradient` |
| Charts | `--mnl-chart-1` to `--mnl-chart-8` |
| Type | `--mnl-font`, `--mnl-text-xs` to `--mnl-text-5xl`, `--mnl-weight-*`, `--mnl-leading-*` |
| Spacing | `--mnl-space-1` to `--mnl-space-16`, a 4px scale |
| Shape | `--mnl-radius-sm` to `--mnl-radius-full`, `--mnl-shadow-sm`, `--mnl-shadow-md`, `--mnl-focus-ring` |

### Theme

The theme follows the operating system until someone uses the toggle. The choice is saved in `localStorage` under `menlo.theme`. `theme-init.js` sets `data-theme="light"` or `"dark"` on `<html>` before the first paint, and each palette colour is defined once with `light-dark(latte, mocha)`.

## Components

Components are `Mnl*.razor` files. Each has its styles beside it in a `.razor.css` file, its parameters documented in the file, and an example in the gallery.

Every component passes attributes it doesn't declare to its root element, so `id`, `data-testid`, `aria-*` and `hx-*` attributes work on all of them, and a `class` is added to the component's own.

### Layout

| Component | Use it to |
| --- | --- |
| `MnlStack` | Space children in a column. The default way to space things vertically. |
| `MnlCluster` | Lay children out in a row that wraps: buttons, a title beside a badge. |
| `MnlGrid` | Lay out equal columns that collapse to one on small screens. |
| `MnlSplit` | Put two regions side by side on wide screens and stack them on narrow ones. |
| `MnlAppShell` | Frame a signed-in page with navigation and a header. See [App Shell](app-shell.md). |
| `MnlDocument` | Render the HTML document: head, stylesheets, scripts and body. |

### Text

| Component | Use it for |
| --- | --- |
| `MnlHeading` | A heading. `Level` sets the element (h1 to h4); `Size` sets how big it looks. |
| `MnlText` | Body text with a size, tone and weight. |
| `MnlEyebrow` | A small uppercase line above a heading. |
| `MnlIcon` | An icon from `MnlIconName` ([Lucide](https://lucide.dev), inlined as SVG). |

### Actions and labels

| Component | Use it for |
| --- | --- |
| `MnlButton` | An action. With `Href` it renders a link, for navigation. Variants: `Primary`, `Secondary`, `Ghost`, `Destructive`. |
| `MnlBadge` | A solid pill for a status: Active, On track. |
| `MnlTag` | A soft tint for a label that isn't a status: a flow, an attribution, a count. |
| `MnlAvatar` | A person, shown as their initials. |
| `MnlSegmented` | Mutually exclusive views, such as Yearly and Monthly. |
| `MnlThemeToggle` | The light and dark switch. |

### Surfaces and feedback

| Component | Use it for |
| --- | --- |
| `MnlCard` | A raised surface that groups content, with optional header and footer. |
| `MnlPageHeader` | The gradient banner at the top of a page. |
| `MnlPanel` | A modal for a focused task: a form or a confirmation. |
| `MnlAlert` | A message that stays on the page. |
| `MnlToastRegion` | Where toasts appear. `MnlDocument` renders it. |
| `MnlEmptyState` | What a list shows when it has nothing in it. |
| `MnlListItem` | A row with optional leading and trailing content. |
| `MnlDisclosure` | A row that expands, built on `<details>`. |
| `MnlStat` | One headline figure with its label and trend. |
| `MnlProgress` | How much of something is used, 0 to 100. |

### Forms

| Component | Use it for |
| --- | --- |
| `MnlFormField` | A control's label, hint and error message. |
| `MnlInput` | A single-line text, number or search input. |
| `MnlSelect` | A dropdown. Choices are `MnlOption` values. |
| `MnlAmountInput` | A money input with a currency symbol. |
| `MnlCheckbox` | A checkbox, or a switch with `Switch`. |
| `MnlFormLayout` | A form's frame: title, fields and actions. |

### Charts

Charts are SVG rendered on the server, coloured by the chart tokens. Hovering a mark shows its value.

| Component | Use it for |
| --- | --- |
| `MnlBarChart` | Comparing amounts across categories. Several series draw grouped bars. |
| `MnlLineChart` | Amounts over time. |
| `MnlDonutChart` | Parts of a whole, six slices or fewer. |
| `MnlRadialChart` | One headline percentage. |

Data is `MnlChartSeries` (a name and one value per category) or `MnlChartSlice` (a label and a value).

## Utilities

| Class | What it does |
| --- | --- |
| `MoneyFormat` | Formats amounts (`R 12 000.00`) and parses what a person typed, accepting spaces and a decimal comma. |
| `Css` | Joins class names and turns enum values into `data-*` attribute values. |
| `ChartMath` | Axis ticks, scaling and shares for the charts. |
| `MnlAssets` | Addresses of the library's static files, with a version suffix. |

## Client behaviour

`wwwroot/js/menlo.js` adds behaviour through attributes:

| Attribute | Behaviour |
| --- | --- |
| `data-mnl-theme-toggle` | Switches and remembers the theme. |
| `data-mnl-panel` (on a `<dialog>`) | Opened when it arrives in the page; removed when it closes. |
| `data-mnl-close` | Closes the panel it is in. |
| `data-mnl-rows` and its `-list`, `-template`, `-add`, `-remove`, `-value`, `-total` parts | Repeated form rows with a running total. |

It also adds the anti-forgery header to htmx requests, shows the `mnl:toast` event as a toast, closes the open panel on `mnl:close-panel`, and sends the browser to `/sign-in` when a fragment request returns 401.

## Vendored files

`wwwroot/vendor` holds htmx and its server-sent events extension; `wwwroot/fonts` holds Nunito Sans. Versions, sources and licences are in `wwwroot/vendor/README.md`. To update one, replace the file and the version in that README.

## Adding to the design system

1. Add `MnlThing.razor` and `MnlThing.razor.css` under `Components/`, inheriting `MnlComponent`.
2. Use role tokens for every colour and size. Add a token before adding a literal value.
3. Add an example to the gallery, with the markup that produces it.
4. Add a test under `src/ui/Menlo.Ui.Tests`.

The `menlo-ui-components` skill in `.claude/skills` has the full rules.
