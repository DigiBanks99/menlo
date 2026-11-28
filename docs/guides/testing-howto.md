# How-to: Write .NET Tests

Menlo's .NET tests use [TUnit](https://tunit.dev/) to run tests, [Shouldly](https://docs.shouldly.org/) to assert and [NSubstitute](https://nsubstitute.github.io/) to fake dependencies. They run on Microsoft.Testing.Platform. This guide shows a typical test of each kind and the fixtures behind API tests.

## Test projects

| Project | Tests | Database |
|---|---|---|
| `src/lib/Menlo.Lib.Tests` | Domain model, value objects, pure logic | None |
| `src/lib/Menlo.AI.Tests` | AI service wrappers | None |
| `src/lib/Menlo.Application.Tests` | EF Core persistence, interceptors, migrations | PostgreSQL (Testcontainers) |
| `src/api/Menlo.Api.Tests` | Endpoints, auth, hosting and the UI's pages, through `WebApplicationFactory`; browser tests in `E2E/` | PostgreSQL (Testcontainers) for the user, database and browser tests |
| `src/ui/Menlo.Ui.Tests` | Design-system components, charts and utilities | None |

Testcontainers needs a running Docker engine. Where it can't reach one, set `MENLO_TEST_POSTGRES` to a PostgreSQL connection string and the tests on `PostgresApiFixture`, the browser tests among them, use that server. Point it at a database that holds nothing else.

The browser tests drive the installed Chrome. See [Test the UI](#test-the-ui).

## Run the tests

`global.json` selects Microsoft.Testing.Platform, so `dotnet test` takes MTP options:

```shell
dotnet test                                                     # every project
dotnet test --project src/api/Menlo.Api.Tests                   # one project
dotnet test --project src/api/Menlo.Api.Tests --treenode-filter "/*/*/CreateBudgetEndpointTests/*"   # one class
dotnet test --coverage --coverage-output-format cobertura --results-directory ./coverage/api-baseline
```

The tree-node filter is `/<assembly>/<namespace>/<class>/<test>`, and `[Category=X]` selects by `[Category("X")]`. A run that finds no tests exits with code 8; add `--ignore-exit-code 8` when that's expected.

Tests that call a real model are skipped unless `MENLO_AI_LIVE_ENDPOINT` is set to a `llama-server` base URL including `/v1`, such as the Aspire `llama` resource's. See [AI Services](../reference/ai-services.md#tests).

CI runs the coverage command, then `scripts/check-api-coverage-baseline.ps1`, which fails if a changed file under `src/api/Menlo.Api/` is below 70% line coverage. To leave infrastructure code out of coverage, see [Coverage exclusions](#coverage-exclusions).

## Coverage exclusions

When Codecov or the CI guardrail flags uncovered code, add tests. Exclude code only when it's infrastructure with no business logic, such as startup or seeding.

### Exclude a type

Put `[ExcludeFromCodeCoverage]` on the type. It is the only way to exclude product code:

```csharp
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public static class BudgetSeedingExtensions
{
    // Seeds the demo budget at startup. Nested types are excluded with it.
}
```

The coverage collector leaves the type out of the Cobertura report. Codecov reads that report, so it drops the type. The CI guardrail treats a changed file that is missing from the report and carries the attribute as `EXCLUDED`. Without the attribute, a missing file fails as `MISSING`. An existing example is `BudgetSeedingExtensions` (`src/lib/Menlo.Application/Common/`).

The guardrail checks `.cs` files only. A page's `.razor` markup isn't measured, which is one more reason to keep rules out of it and in the endpoint and view model.

Don't add product code to the `ignore` list in `codecov.yml`: the guardrail never reads it. That list holds only paths that aren't hand-written product code:

- `src/api/Menlo.AppHost/**` (the Aspire AppHost)
- `src/api/**/*.Tests/**` and `src/ui/**/*.Tests/**` (test projects)
- `src/ui/Menlo.Ui/Gallery/**` (the component gallery)
- `src/api/**/obj/**`, `**/*.g.cs` and `**/*.generated.cs` (generated code)

### Partial coverage

Exclude a whole infrastructure type, never part of a type that holds business logic. If a type mixes the two, move the infrastructure into its own type and exclude that, then test the rest. Don't put the attribute on single methods to lift a business-logic type over the line.

### Targets by layer

| Scope | Target | Set by |
|---|---|---|
| Each changed file under `src/api/Menlo.Api/**` | 70% line coverage | `scripts/check-api-coverage-baseline.ps1` (fails CI) |
| `src/api/Menlo.Api/**`, whole project and PR patch | 70%, 2% threshold | `codecov.yml` `api` status |
| Whole repository, project | No drop of more than 5% from the base commit (`auto`) | `codecov.yml` `default` project status |
| Whole repository, PR patch | 70%, 5% threshold | `codecov.yml` `default` patch status |

## Write a typical test

Name the test after the situation, act, then call `ItShould…()` local functions that hold the assertions:

```csharp
public sealed class ChatServiceTests
{
    [Test]
    public void TheKernelIsMissing()
    {
        Exception? exception = Catch.Exception(() => new ChatService(null!));

        ItShouldRefuseToBeCreated();

        void ItShouldRefuseToBeCreated() => exception.ShouldBeOfType<ArgumentNullException>();
    }
}
```

- Use `[Test]`, and `[Arguments(...)]` for several inputs to one scenario.
- Use `[Before(Test)]` for async setup, and implement `IDisposable` or `IAsyncDisposable` for teardown. TUnit creates a new class instance for each test.
- Pass `TestContext.Current!.Execution.CancellationToken` to async calls made by the test. Code that runs on another thread, such as a request handler in a fake server, has no `TestContext`: use that thread's own token (for example `HttpContext.RequestAborted`).

> Many existing tests still use `Given…_When…_Then…` names and inline assertions from before the move to TUnit. Write new tests in the style above, and restyle old ones when you change them.

## Write an API test against a real database

Tests that need the whole app on a real PostgreSQL database share one migrated database for the run, through `PostgresApiFixture`:

```csharp
[ClassDataSource<PostgresApiFixture>(Shared = SharedType.PerTestSession)]
public sealed class ThingEndpointTests(PostgresApiFixture fixture) : TestFixture
{
    [Test]
    public async Task AThingIsCreated()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/things", new { Name = "Rent" }, TestContext.Current!.Execution.CancellationToken);

        ItShouldBeCreated();

        void ItShouldBeCreated() => response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
```

The database is shared, so give each test its own data (fresh IDs and names), or empty the tables it uses first and mark the class `[NotInParallel("<group>")]`.

### Turn features on

Pass configuration to the factory: `fixture.CreateFactory(new Dictionary<string, string?> { ["Features:Budget"] = "true" })`, or set `ConfigurationOverrides` on a `TestWebApplicationFactory`. A feature's endpoints return 404 while its flag is off.

### Send writes with an antiforgery token

The API rejects unsafe requests without an antiforgery token. `CreateAntiforgeryClientAsync` fetches one and adds the `X-XSRF-TOKEN` header; `CreateClient` doesn't, so use it only for reads.

### Read JSON responses case-insensitively

Responses use camelCase, while the DTOs use PascalCase properties. Deserialize with `new JsonSerializerOptions { PropertyNameCaseInsensitive = true }`, as each test class's `JsonOptions` does.

## Test the UI

The UI is tested at three levels. Use the fastest one that can show what you need.

| Level | Where | Use it for | Needs |
|---|---|---|---|
| Component | `src/ui/Menlo.Ui.Tests` | What a component renders for given parameters | Nothing |
| Page | `src/api/Menlo.Api.Tests/Ui` | What an endpoint returns: markup, status, redirects, `HX-Trigger` events | A database for pages that read data |
| Browser | `src/api/Menlo.Api.Tests/E2E` | What only a browser shows: htmx swaps, panels, toasts, the theme, the Content Security Policy | A database and Chrome |

### Test a component

Render it with `Rendered.ComponentAsync` and query the result with AngleSharp:

```csharp
[Test]
public async Task GivenAnHref_WhenRendered()
{
    IDocument document = await Rendered.ComponentAsync<MnlButton>(new Dictionary<string, object?>
    {
        ["Href"] = "/analytics",
        ["ChildContent"] = Rendered.Text("Analytics")
    });

    ItShouldBeALinkStyledAsAButton();

    void ItShouldBeALinkStyledAsAButton() =>
        document.Find("a.mnl-button").GetAttribute("href").ShouldBe("/analytics");
}
```

`GalleryTests` renders the whole gallery and checks rules that hold for every component: no inline styles or scripts, every form control labelled, no repeated ids.

### Test a page or a form

Ask for the page and read the HTML. `UiHtml` has the helpers: `DocumentAsync()` parses a response, `ByTestId()` finds an element, `HtmxAsync()` sends a request the way htmx does, and `Triggers()` reads the `HX-Trigger` header.

```csharp
HttpResponseMessage invalid = await client.HtmxAsync(HttpMethod.Post, categories, [new("name", "")]);
IHtmlDocument form = await invalid.DocumentAsync();

invalid.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
form.ByTestId("error-name").TextContent.ShouldBe("Name is required");
```

A successful change returns `204` with events, so assert on `Triggers()`, then ask for the page again to see the result.

### Test in a browser

Browser tests use [Playwright for .NET](https://playwright.dev/dotnet/) and carry `[Category("E2E")]`. `BrowserFixture.StartAsync` starts the app on a real port and opens a page on it, signed in as the test user:

```csharp
await using AppSession app = await browser.StartAsync(fixture.CreateFactory());
await app.Page.GotoAsync("/");
await Expect(app.Page.GetByTestId("home-hero")).ToBeVisibleAsync();

app.BrowserErrors.ShouldBeEmpty();
```

- `app.BrowserErrors` holds console errors and uncaught exceptions. A Content Security Policy violation is a console error, so asserting it is empty covers the policy.
- To seed data, use a second factory from the same fixture and the JSON API: both hosts share the database.
- The tests drive the installed Chrome, so there is nothing to download. Set `MENLO_E2E_BROWSER_CHANNEL` to another channel (such as `msedge`), or to `bundled` after running `playwright install chromium`. Set `MENLO_E2E_HEADED=1` to watch.
- Run only them with `--treenode-filter "/*/*/*/*[Category=E2E]"`.

## Pick a factory

| Factory | Use it for | User context |
|---|---|---|
| `PostgresTestWebApplicationFactory`, from `PostgresApiFixture.CreateFactory` | Endpoints and pages that read or write data, and browser tests | A substituted `IUserContextProvider` returns a user context; the real `/api` user provisioning runs against the database |
| `TestWebApplicationFactory` | Hosting, auth, headers, user provisioning, and anything that needs the real user context | The test auth handler signs in a user (`UserId`, `UserEmail`, `UserRoles`), or no one with `SimulateUnauthenticated` |

`TestWebApplicationFactory` defaults to the Production environment, a dummy connection string and no migrations, and it replaces the AI services with substitutes. Its `init` properties (`EnvironmentName`, `ConfigurationOverrides`, `MenloConnectionString`, `ConfigureApp` and more) cover the remaining cases.

## Share a fixture

Fixtures that start a container are shared with `[ClassDataSource<T>(Shared = SharedType.PerTestSession)]`, so one container serves every class that names it. They implement TUnit's `IAsyncInitializer` and `IAsyncDisposable`.

| Fixture | Project | Gives you |
|---|---|---|
| `PostgresApiFixture` | Api | Migrated database (or `MENLO_TEST_POSTGRES`) and `CreateFactory` for a signed-in host |
| `UserApiFixture` | Api | Migrated database, `Services` for seeding, `CreateFactory(userId, email, …)` for a signed-in host |
| `UserPersistenceFixture` | Api | Migrated database and `Services`, with no web host |
| `PersistenceFixture` | Application | Migrated database and `Services` |
| `InterceptorPersistenceFixture` | Application | Unmigrated database, a host with interceptors, and the connection string |

TUnit runs tests in parallel, including tests in the same class. Each fixture's classes also carry `[NotInParallel("<fixture group>")]`, so tests that share a fixture run one at a time, as they did under xUnit. Keep that attribute when you add a class to a fixture group. In tests without a shared fixture, give each test its own state.

User tests isolate by data: use a fresh `Guid` in every user ID and email, as `UserApiFixture.CreateFactory` does. `TestWebApplicationFactory` stands in a stub for user provisioning unless you set `UseRealUserProvisioning`, so tests with no database still reach `/api` endpoints.
