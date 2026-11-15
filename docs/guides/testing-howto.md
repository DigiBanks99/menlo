# How-to: Write .NET Tests

Menlo's .NET tests use [TUnit](https://tunit.dev/) to run tests, [Shouldly](https://docs.shouldly.org/) to assert and [NSubstitute](https://nsubstitute.github.io/) to fake dependencies. They run on Microsoft.Testing.Platform. This guide shows a typical test of each kind and the fixtures behind API tests.

## Test projects

| Project | Tests | Database |
|---|---|---|
| `src/lib/Menlo.Lib.Tests` | Domain model, value objects, pure logic | None |
| `src/lib/Menlo.AI.Tests` | AI service wrappers | None |
| `src/lib/Menlo.Application.Tests` | EF Core persistence, interceptors, migrations | PostgreSQL (Testcontainers) |
| `src/api/Menlo.Api.Tests` | Endpoints, auth, hosting, through `WebApplicationFactory` | PostgreSQL (Testcontainers) for the budget and user tests |

Testcontainers needs a running Docker engine.

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

When Codecov or the CI guardrail flags uncovered code, add tests. Exclude code only when it's infrastructure with no business logic, such as startup, seeding or SPA hosting.

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

The coverage collector leaves the type out of the Cobertura report. Codecov reads that report, so it drops the type. The CI guardrail treats a changed file that is missing from the report and carries the attribute as `EXCLUDED`. Without the attribute, a missing file fails as `MISSING`. Existing examples are `BudgetSeedingExtensions` (`src/lib/Menlo.Application/Common/`) and `SpaHostingExtensions` (`src/api/Menlo.Api/SpaHosting/`).

Don't add product code to the `ignore` list in `codecov.yml`: the guardrail never reads it. That list holds only paths that aren't hand-written product code:

- `src/api/Menlo.AppHost/**` (the Aspire AppHost)
- `src/api/**/*.Tests/**` (test projects)
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

## Write a budget API test

Budget endpoint tests run against a real PostgreSQL database, shared by every test in the run:

```csharp
[ClassDataSource<BudgetApiFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Budget")]
public sealed class CreateBudgetEndpointTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    [Test]
    public async Task ABudgetIsCreatedForThisYear()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);
        BudgetDto? budget = await response.Content.ReadFromJsonAsync<BudgetDto>(
            JsonOptions, TestContext.Current!.Execution.CancellationToken);

        ItShouldBeCreated();

        void ItShouldBeCreated() => response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
```

### Give every test its own data

Budgets are unique per year across the whole database, so tests that share the container would collide. `fixture.CreateIsolatedFactory()` empties the budget tables first and starts an API host on the shared database. The budget tests run one at a time (`[NotInParallel("Budget")]`), so no test sees another's data, whatever order they run in.

- When a second host in one test must see what the first wrote (for example, seeding with a feature on and reading with it off, or reading as another member), pass `keepData: true`.
- To test that members share data, pass `asDifferentMember: true` to the second factory, which signs in as another user.
- `fixture.CreateClient()` and `fixture.CreateAntiforgeryClientAsync()` use one host shared by every test that calls them. Use them only for requests that don't write data, such as a lookup of an unknown ID.

### Turn features on

Budget endpoints sit behind feature flags. `CreateIsolatedFactory` always sets `Features:Budget` to `true`, and sets `Features:BudgetItems` from its arguments:

```csharp
fixture.CreateIsolatedFactory(budgetItemsEnabled: true);                                 // items endpoints on
fixture.CreateIsolatedFactory(budgetItemsEnabled: true, simulateUnauthenticated: true);  // signed out
```

With a flag off, its endpoints return 404.

### Send writes with an antiforgery token

The API rejects unsafe requests without an antiforgery token. `CreateAntiforgeryClientAsync` fetches one and adds the `X-XSRF-TOKEN` header; `CreateClient` doesn't, so use it only for reads.

### Read JSON responses case-insensitively

Responses use camelCase, while the DTOs use PascalCase properties. Deserialize with `new JsonSerializerOptions { PropertyNameCaseInsensitive = true }`, as each test class's `JsonOptions` does.

## Pick a factory

| Factory | Use it for | User context |
|---|---|---|
| `BudgetTestWebApplicationFactory`, from `BudgetApiFixture.CreateIsolatedFactory` | Endpoints that read budget data (budgets, categories, items, summaries) | A substituted `IUserContextProvider` returns a user context; the real `/api` user provisioning runs against the database |
| `TestWebApplicationFactory` | Hosting, auth, headers, user provisioning, and anything that needs the real user context | The test auth handler signs in a user (`UserId`, `UserEmail`, `UserRoles`), or no one with `SimulateUnauthenticated` |

`TestWebApplicationFactory` defaults to the Production environment, a dummy connection string and no migrations, and it replaces the AI services with substitutes. Its `init` properties (`EnvironmentName`, `ConfigurationOverrides`, `MenloConnectionString`, `ConfigureApp` and more) cover the remaining cases.

## Share a fixture

Fixtures that start a container are shared with `[ClassDataSource<T>(Shared = SharedType.PerTestSession)]`, so one container serves every class that names it. They implement TUnit's `IAsyncInitializer` and `IAsyncDisposable`.

| Fixture | Project | Gives you |
|---|---|---|
| `BudgetApiFixture` | Api | Migrated database, `CreateIsolatedFactory`, a shared read-only client |
| `UserApiFixture` | Api | Migrated database, `Services` for seeding, `CreateFactory(userId, email, …)` for a signed-in host |
| `UserPersistenceFixture` | Api | Migrated database and `Services`, with no web host |
| `PersistenceFixture` | Application | Migrated database and `Services` |
| `InterceptorPersistenceFixture` | Application | Unmigrated database, a host with interceptors, and the connection string |

TUnit runs tests in parallel, including tests in the same class. Each fixture's classes also carry `[NotInParallel("<fixture group>")]`, so tests that share a fixture run one at a time, as they did under xUnit. Keep that attribute when you add a class to a fixture group. In tests without a shared fixture, give each test its own state.

User tests isolate by data: use a fresh `Guid` in every user ID and email, as `UserApiFixture.CreateFactory` does. `TestWebApplicationFactory` stands in a stub for user provisioning unless you set `UseRealUserProvisioning`, so tests with no database still reach `/api` endpoints.
