# PostgreSQL Setup Implementation Guide

This document outlines the step-by-step process for integrating PostgreSQL with Entity Framework Core and .NET Aspire in the Menlo Home Management solution.

## Prerequisites

- Review the [Architecture Document](../../explanations/architecture-document.md) for infrastructure requirements.
- Ensure you have Podman installed for local container orchestration.
- .NET 9 SDK or later installed.

## Steps

### 1. Add PostgreSQL EF Core Provider ✅

- Add the `Npgsql.EntityFrameworkCore.PostgreSQL` NuGet package to your data access projects (e.g., `Menlo.Api`).
  - Use the following command:

    ```sh
    dotnet add src/api/Menlo.Api/Menlo.Api.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
    ```

### 2. Configure EF Core for PostgreSQL ✅

- In your `DbContext` or in `Program.cs`, configure EF Core to use the Npgsql provider:

    ```csharp
    builder.Services.AddDbContext<YourDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    ```

- Use Aspire's configuration system to setup the connection string.

### 3. Update Aspire AppHost ✅

- Add `Aspire.Hosting.PostgreSQL` as a package reference to `api/Menlo.AppHost`.
- Add `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` to `api/Menlo.Api`.
- In `Menlo.AppHost`, add a PostgreSQL container resource to the Aspire orchestration. Use the Microsoft Docs mcp to find the `.NET Aspire PostgreSQL integration` documentation for configuring it correctly.
- Wire up the connection string from Aspire to your API/service projects.

### 4. Create/Update Migrations ✅

- Ensure the Persitance folder is renamed Persistence
- Move the EF Core dependency configuration to a dedicated extension class in the `Menlo.Api/Persistence` folder and use it in `Program.cs`.
- Ensure `Menlo.Api` references package `Microsoft.EntityFrameworkCore.Design`.
- Add an initial EF Core migration for your schema:

    ```sh
    dotnet ef migrations add InitialCreate --project src/api/Menlo.Api/Menlo.Api.csproj -o src/api/Menlo.Api/Persistence/Migrations
    ```

- Ensure the migration targets PostgreSQL.

### 5. Test Local Development ✅

- Automated migrations: Implemented via a Hosted Service that runs EF Core migrations on startup, controlled by configuration.
- Aspire orchestration: Solution runs with Aspire, verifying the Postgres container starts and the API connects successfully.
- Integration test: A TestContainer-based integration test (`GivenTheMenloApplication_WhenStartingTheApplication`) is implemented in `Menlo.Api.IntegrationTests`. This test:
  - Starts a PostgreSQL Testcontainer.
  - Launches the Menlo API configured to use the container.
  - Verifies the API responds to the OpenAPI endpoint (`/openapi/menlo-api.json`).
  - Asserts that EF Core migrations have been applied by checking that the `__EFMigrationsHistory` table contains records after startup.

### 6. Documentation & Validation ✅

- Documentation updated to reflect the new setup and testing approach.
- Integration test ensures DB connectivity and that migrations are applied on startup.
- No CRUD tests are required at this stage, as per current requirements.

---

For more details, see the [Implementation Roadmap](../../requirements/implementation-roadmap.md) and [Architecture Document](../../explanations/architecture-document.md).
