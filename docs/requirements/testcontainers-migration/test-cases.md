# Test Cases: In-Memory to TestContainers Migration

## Test Strategy
The primary goal is to ensure that all existing functional tests continue to pass after the infrastructure change. No new business logic is being added, so "functional parity" is the key metric.

## TC-001: Dependency Verification
**Objective:** Confirm strictly no usage of In-Memory provider remains.

**Steps:**
1. Open `src/api/Menlo.Api.Tests/Menlo.Api.Tests.csproj`.
2. Check for `<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />`.
3. Search entire solution for `UseInMemoryDatabase`.

**Expected Result:**
- The package reference is absent from the API tests project.
- No occurrences of `UseInMemoryDatabase` exist in the target files.

## TC-002: Build & Execution
**Objective:** Confirm the project builds and tests execute.

**Prerequisites:**
- Docker engine is running.

**Steps:**
1. Run `dotnet build src/api/Menlo.Api.Tests/Menlo.Api.Tests.csproj`.
2. Run `dotnet test src/api/Menlo.Api.Tests/Menlo.Api.Tests.csproj`.

**Expected Result:**
- Build succeeds with 0 warnings related to DB providers.
- All tests pass (Green).
- Console output indicates Testcontainers logic (e.g., "Starting container...").

## TC-003: Interceptor Functionality (Auditing)
**Objective:** specific verification of `AuditingInterceptorTests.cs`.

**Context:**
This test checks if `CreatedBy`, `CreatedAt` etc. are set. In Postgres, `DateTimeOffset` precision matters.

**Steps:**
1. Run the `AuditingInterceptorTests`.
2. Inspect the saved entities in the database.

**Expected Result:**
- Entities are successfully saved to the PostgreSQL container.
- Audit fields are populated.
- No `Microsoft.EntityFrameworkCore.DbUpdateException` due to constraint violations (meaning test data is valid).

## TC-004: Interceptor Functionality (Soft Delete)
**Objective:** specific verification of `SoftDeleteInterceptorTests.cs`.

**Context:**
Soft delete often uses Query Filters.

**Steps:**
1. Run `SoftDeleteInterceptorTests`.
2. Perform a delete operation.
3. Query the database directly (ignoring query filters) to ensure the row exists but `IsDeleted` is true.

**Expected Result:**
- The row is physically present in Postgres.
- `IsDeleted` column is set to true.

## TC-005: Database Fixture Lifecycle
**Objective:** Verify container reuse and cleanup.

**Steps:**
1. Run all tests in `Menlo.Api.Tests` sequentially or in parallel.
2. Observe execution time.

**Expected Result:**
- Tests do not fail due to generic "Table already exists" or "Primary Key violation" errors (proving `Respawn` or cleanup is working).
- Containers are spun down after test run completes.
