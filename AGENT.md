# Menlo - AGENT.md

## Quick Start
```bash
aspire run  # Starts full stack: PostgreSQL, API, Web UI
```
Wait for all resources to report healthy before proceeding.

## Validation Commands
```bash
# Must all pass before committing
aspire run                          # All resources healthy
dotnet build Menlo.slnx             # Build succeeds
dotnet test Menlo.slnx              # All tests pass
pnpm --dir src/ui/web test:all      # Frontend tests pass
pnpm --dir src/ui/web lint          # No lint errors
```

## Where Things Live
- **Backend**: `src/api/` (Menlo.Api, Menlo.AppHost) and `src/lib/` (Menlo.Lib, Menlo.AI)
- **Frontend**: `src/ui/web/projects/` (menlo-app, menlo-lib, data-access)
- **Specs**: `docs/requirements/` (READ-ONLY - do not modify)
- **Plan**: `docs/plans/fix_plan.md` (your working TODO list)

## Tech Stack
- .NET 10, C# 14, Entity Framework Core, PostgreSQL
- Angular 21, TypeScript, Vite, Vitest
- Aspire 13.1 for orchestration

## Learnings
<!-- Ralph updates this section with discoveries - keep brief -->

- **xUnit v3 tests**: Run test exe directly (e.g., `/tmp/menlo-build/Menlo.Lib.Tests/bin/Debug/net10.0/Menlo.Lib.Tests`) for visible output. `dotnet test` doesn't show xUnit v3 output properly.
- **Duplicate assembly errors**: If CS0579 errors appear, clean both `/tmp/menlo-build` AND local `src/**/obj` dirs: `find src -name obj -type d -exec rm -rf {} +`
- **API endpoint pattern**: Use extension methods on RouteGroupBuilder (C# 14 feature) - see `src/api/Menlo.Api/Budgets/Endpoints/` for examples.
- **EF Core BudgetId queries**: Use `b.Id == budgetId` directly rather than `b.Id.Value == id`. The value converters handle the comparison properly.
- **EF Core nullable Money**: Use `ComplexProperty()` instead of shadow properties for nullable value objects. Shadow properties don't hydrate back to the domain model properly.
- **Test assertion JSON**: ProblemDetails extensions return JsonElement objects. Use `?.ToString()` for string comparisons: `problemDetails.Extensions["errorCode"]?.ToString().ShouldBe("expected")`
- **Entity property access**: User entity has `ExternalId` and `DisplayName` properties (not `ExternalUserId` and `Name`). Budget uses `AddSubcategory()` for nested categories.
- **Money nullable testing**: Access Money properties via `.Value.Amount` and `.Value.Currency` when Money? is not null. Money is a struct so nullable access differs from class references.
- **Domain method additions**: When adding new domain operations, add the method to the aggregate root and delegate to entity methods. Use `internal` for entity methods to maintain encapsulation. Example: `Budget.UpdateCategoryDescription()` → `BudgetCategory.UpdateDescription()`
- **Money.Create return type**: Returns `Result<Money, Error>` not `Result<Money, string>`. Import `Menlo.Lib.Common.Abstractions.Error` for proper typing.
- **Category endpoints**: Built 5 endpoints for budget category management: CREATE, UPDATE, DELETE, SET_AMOUNT, CLEAR_AMOUNT. All follow same pattern: auth check → validate request → load budget → find category → execute domain operation → save → return response.
- **Aspire AppHost.Sdk 13.1.0**: Remove explicit `Aspire.Hosting.AppHost` package from `Directory.Packages.props` when upgrading to 13.1.0 - the SDK includes it automatically. DCP path issues may persist in some development environments.
- **Endpoint test patterns**: API endpoint tests follow pattern: create test scenarios for success, validation errors, not found, unauthorized access. Use helper methods for assertions with descriptive names like `ItShouldHaveRequestedCurrency()`. Always test error response structure and status codes.
- **Frontend-Backend integration**: TypeScript interfaces should match C# DTOs exactly. Use `toResult()` operator from shared-util for consistent error handling in Angular services. Signal-based state management works well with Result pattern - handle loading, success, and error states in separate signals.

## Rules

- Avoid in-memory databases. Prefer test containers
