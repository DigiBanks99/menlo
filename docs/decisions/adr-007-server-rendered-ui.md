# ADR-007: Server-Rendered UI with Razor Components and HTMX

## Status

**Accepted** - 2026-11-16

Supersedes the Angular SPA in [ADR-005](adr-005-bundled-spa-hosting.md). The rest of ADR-005 stays: the API image serves the UI, and there is one deploy.

## Context

The UI was an Angular 21 app in an Nx workspace, with two Storybooks, Tailwind, Vitest, ESLint, Prettier and TypeScript Playwright tests. For an app this size (home, budgets, budget detail, analytics and sign-in) the toolchain cost more than it gave:

- **Two toolchains.** A second language, package manager and lockfile, two frontend CI jobs, and a dev proxy between the Vite server and the API.
- **Tools that didn't fit together.** Nx, Angular and Storybook each wanted their own configuration. The app resolved the API client library from `dist/`, and `nx serve` didn't rebuild it, so a missing export broke the dev server until someone rebuilt by hand.
- **State kept twice.** The API owned the data and the cookie session; the SPA re-fetched it, held it in signals and mapped errors back onto forms.
- **Hard for agents to maintain.** Most changes touched an API handler, a typed client, a component and its story, across two build systems.

The API already owned sign-in (a cookie session, see [Authentication API](../reference/authentication-api.md)) and served the UI from the same origin, so rendering HTML on the server needed no new infrastructure.

The decision was taken with the owner on [Replace the Angular UI with server-rendered Razor components and HTMX](https://github.com/DigiBanks99/menlo/issues/651).

## Decision

- **The API renders the UI as HTML.** Pages are static Razor components (`.razor`), rendered on the server and returned from minimal API endpoints. There is no Blazor interactivity, no WebAssembly and no SignalR circuit.
- **HTMX makes it feel like an app.** [htmx](https://htmx.org) swaps parts of the page with HTML the server sends. Links and forms are boosted, so navigation doesn't reload the document.
- **A design system in its own library.** `src/ui/Menlo.Ui` holds the tokens (Catppuccin Latte for light, Mocha for dark), the components, the layouts and server-rendered SVG charts. A gallery at `/design` shows every component and replaces Storybook.
- **No Node toolchain.** Styles are plain CSS with custom properties, next to each component. htmx and the fonts are vendored files. There is no bundler, no `package.json` and no frontend build step.
- **One small script.** `menlo.js` handles presentation behaviour that needs no data: the theme, opening panels, toasts and repeated form rows. Anything that needs data is an htmx request.
- **A strict Content Security Policy.** `script-src 'self'; style-src 'self'`: no inline scripts, inline styles or `eval`. The API reference (Scalar), which needs inline code, is only mapped in Development and has its own policy.
- **Tests run through `dotnet test`.** Components are tested by rendering them to HTML, endpoints through `WebApplicationFactory`, and whole flows in a real browser with Playwright for .NET.
- **The JSON API stays.** The pages call the same handlers as `/api`, in process, so the two can't drift apart.
- **Server push will use server-sent events.** Slow work, such as image processing, will report progress over htmx's SSE extension, which is vendored and off until a page asks for it. WebSockets remain possible through htmx's WebSocket extension if a feature needs two-way messages.

How the UI is structured is in [UI Architecture](../explanations/ui-architecture.md).

## Options Considered

### 1. Razor components with HTMX (Selected)

- **Pros**: One language, one build and one test runner; components are typed and unit-testable; state lives on the server and in the URL; no client build; works under a strict CSP
- **Cons**: Every interaction is a round trip to the server; rich client-side interaction (drag and drop, offline use) would need more script; fewer ready-made component libraries
- **Assessment**: The app is forms and lists for one household on a home network, which is what server rendering is good at

### 2. Razor Pages with HTMX

- **Pros**: A familiar page model with handlers
- **Cons**: Reusable components are partials and tag helpers, which are weakly typed and awkward to unit test
- **Assessment**: The design system is the centre of this work, and Razor components make a better one

### 3. React only

- **Pros**: A large ecosystem; richer client-side interaction; drops Angular, Nx and their friction
- **Cons**: Keeps a Node toolchain, client state, a typed API client and a component gallery to maintain
- **Assessment**: Solves the tooling friction but not the duplicated state or the second toolchain

### 4. Blazor Server or WebAssembly

- **Pros**: Components with interactivity and no JavaScript
- **Cons**: A SignalR circuit per browser tab, or a large WebAssembly download; both need script the strict CSP would have to allow
- **Assessment**: More machinery than the app needs. Static rendering keeps the same components without it

## Consequences

**Positive:**

- One toolchain. `dotnet build` builds everything and `dotnet test` tests everything, the UI included
- CI has no frontend jobs, and the container image has no frontend build stage
- A page's data, rules and markup are in one place: an endpoint, a view model and a component
- The URL holds a page's state, so reloads, the back button and shared links work
- The Content Security Policy no longer allows inline styles

**Negative:**

- Interactions wait on the server. On the home network this is fast; over the tunnel it depends on the connection
- The session cookie is `SameSite=Strict`, so a navigation that starts on another site arrives without it. The UI answers such a request with a page that reloads the same address from this site (see [Authentication API](../reference/authentication-api.md))
- The gallery and the charts are ours to maintain
- The app has no offline mode. The PWA manifest and service worker ideas in earlier documents no longer apply

**Changed behaviour:**

- An address that matches no page shows a "not found" page. The SPA redirected to the home page
- The category tree is always expanded
- An item's payers are chosen from the household's members. The SPA asked for a typed user ID
- Recording a bill or a payment opens a small panel. The SPA showed the field in the row
- Members with the Reader role don't see controls they can't use

## Related

- [ADR-005: Bundled SPA Hosting](adr-005-bundled-spa-hosting.md)
- [UI Architecture](../explanations/ui-architecture.md)
- [Design System](../reference/design-system.md)
- [How to add a page](../guides/ui-page-howto.md)
