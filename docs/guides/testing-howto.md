# How-to: Write .NET Tests

Menlo's .NET tests use [TUnit](https://tunit.dev/) to run tests, [Shouldly](https://docs.shouldly.org/) to assert and [NSubstitute](https://nsubstitute.github.io/) to fake dependencies. They run on Microsoft.Testing.Platform. This guide shows a typical test of each kind and the fixtures behind API tests.

## Test projects

| Project | Tests | Database |
|---|---|---|
| `src/lib/Menlo.Lib.Tests` | Domain model, value objects, pure logic | None |
| `src/lib/Menlo.AI.Tests` | AI service wrappers | None |
| `src/lib/Menlo.Application.Tests` | EF Core persistence, interceptors, migrations | PostgreSQL (Testcontainers) |
| `src/api/Menlo.Api.Tests` | Endpoints, auth, hosting, through `WebApplicationFactory` | PostgreSQL (Testcontainers) for the budget and onboarding tests |

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

CI runs the coverage command, then `scripts/check-api-coverage-baseline.ps1`, which fails if a changed file under `src/api/Menlo.Api/` is below 70% line coverage.

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
        ItShouldBelongToTheHousehold();

        void ItShouldBeCreated() => response.StatusCode.ShouldBe(HttpStatusCode.Created);

        void ItShouldBelongToTheHousehold() => budget!.HouseholdId.ShouldBe(factory.HouseholdId.Value);
    }
}
```

### Give every test its own household

`fixture.CreateIsolatedFactory()` starts an API host whose signed-in user belongs to a new household (`HouseholdId.NewId()`), exposed as `factory.HouseholdId`. Data one test creates is invisible to every other test, so tests can't contaminate each other, whatever order they run in. Don't hand-pick household GUIDs.

- When two hosts in one test must see the same data (for example, seeding with a feature on and reading with it off), create the household yourself and pass it to both: `fixture.CreateIsolatedFactory(household)`.
- To test that one household can't see another's data, create two factories and use the first one's IDs from the second one's client.
- `fixture.CreateClient()` and `fixture.CreateAntiforgeryClientAsync()` use one household shared by every test that calls them. Use them only for requests that don't write household data, such as a lookup of an unknown ID.

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
| `BudgetTestWebApplicationFactory`, from `BudgetApiFixture.CreateIsolatedFactory` | Endpoints that read the user's household (budgets, categories, items, summaries) | A substituted `IUserContextProvider` returns the factory's `HouseholdId`, with no user or household rows needed |
| `TestWebApplicationFactory` | Hosting, auth, headers, onboarding, and anything that needs the real user context | The test auth handler signs in a user (`UserId`, `UserEmail`, `UserRoles`), or no one with `SimulateUnauthenticated` |

`TestWebApplicationFactory` defaults to the Production environment, a dummy connection string and no migrations, and it replaces the AI services with substitutes. Its `init` properties (`EnvironmentName`, `ConfigurationOverrides`, `MenloConnectionString`, `ConfigureApp` and more) cover the remaining cases.

## Share a fixture

Fixtures that start a container are shared with `[ClassDataSource<T>(Shared = SharedType.PerTestSession)]`, so one container serves every class that names it. They implement TUnit's `IAsyncInitializer` and `IAsyncDisposable`.

| Fixture | Project | Gives you |
|---|---|---|
| `BudgetApiFixture` | Api | Migrated database, `CreateIsolatedFactory`, a shared read-only client |
| `OnboardingApiFixture` | Api | Migrated database, `Services` for seeding, `CreateFactory(userId, email, …)` for a signed-in host |
| `OnboardingPersistenceFixture` | Api | Migrated database and `Services`, with no web host |
| `PersistenceFixture` | Application | Migrated database and `Services` |
| `InterceptorPersistenceFixture` | Application | Unmigrated database, a host with interceptors, and the connection string |

TUnit runs tests in parallel, including tests in the same class. Each fixture's classes also carry `[NotInParallel("<fixture group>")]`, so tests that share a fixture run one at a time, as they did under xUnit. Keep that attribute when you add a class to a fixture group. In tests without a shared fixture, give each test its own state.

Onboarding tests isolate by data instead of households: use a fresh `Guid` in every user ID, email and household name, as `OnboardingApiFixture.CreateFactory` does.
