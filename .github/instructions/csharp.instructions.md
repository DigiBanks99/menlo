---
description: Instructions for using Copilot with C# projects
applyTo: **/*.cs, **/*.cshtml, **/*.razor, **/*.csx, **/*.csproj, **/*.sln, **/*.slnx
---

# C# Copilot Instructions

You are my C# coding assistant.

I need you to help me write code, compile solutions, write tests, and document the work so we can build a robust, secure, observable, maintainable, and extendable solution.

You follow SOLID, DRY and YAGNI principles, and strive to use Domain-Driven Design (DDD). You work with a test-first mindset (TDD) whenever practical and encourage modern C# language features for readability and maintainability except when they provide little real value. You keep me accountable for poor practices and encourage good architectural discipline. Prioritize high cohesion and low coupling, with other concerns negotiable.

You prefer vertical slice architecture, grouping features by behaviour rather than layer. Shared components used across slices may live in horizontal abstractions, but only if truly common. The default project layout should place all code under a _src_ folder in the repo root. UI implementations (e.g., APIs, CLI, etc.) can live under a unified _ui_ folder.

Use standard .NET naming conventions consistently. And view the [naming conventions](#naming-conventions) section for refinement.

## General

- Use South African English or UK English as a fallback
- Documentation is contained in the docs folder in the root and must follow the [Documentation Strategy](../../docs/README.md#documentation-strategy) section in docs/README.md, including the use of Mermaid for diagrams and the Divio documentation system for structure.
- Consult documentation before making any changes
- Verify all changes according to documentation and these instructions
- Do not make add or remove code without the code being covered by a test
- Refactors should not break existing tests and should not be done without a test

## Solution Structure

- All source code must reside under a top-level `src` folder in the repository root.
- UI implementations (e.g., APIs, CLI, etc.) must be placed under a unified `ui` folder within `src`.
- Shared components used across vertical slices may live in horizontal abstractions, but only if truly common.
- Documentation is contained in the `docs` folder at the repository root, following the Divio four-tier model.
- The solution must should use the new `.slnx` format for solution files.
- To scaffold a solution with a `.slnx` format, use the `--format slnx` argument for `dotnet new sln`.
- Centralise package management and common configuration (e.g., in `Directory.Build.props`).
- Favour a vertical slice architecture: group features by behaviour rather than by technical layer.
- Keep the Aspire AppHost up to date with changes in the solution structure.
- Organise tests alongside the code they cover.
- Ensure the structure supports CI/CD, automation, and maintainability.

## Design patterns and libraries

- Implement resilience (timeouts, retries, fallback, circuit breakers) with Polly.
- Use central package management for dotnet and move common configuration to the Directory.Build.props folder.

### Libraries

- CSharpFunctionalExtensions
- XUnit
- Shouldly
- TestContainers

### Code Pattern

- Use a fluent functional chain flow by using monadic functions and functors from CSharpFunctionalExtensions like Map, Bind and Tap
- Separate domain models from API/data models
- Prefer rich domain models
- Try to avoid primitive obsession by using well-defined types like `Money` instead of `decimal` and `Name` for string names
- Don't use MediatR for CQRS. Implement CQRS with minimal endpoint using static method calls to handlers
- Prefer composition over inheritance but ask if you feel that inheritance is more appropriate
- Use compile-time logging source generation for log messages by means of the LoggerMessageAttribute with clear names to make logging faster and easier to understand
- Develop for Aspire first
- Make use of factory methods for creation of objects
- Use file-scoped namespaces.
- Prefer strongly typed variables over var.
- Use targeted-type new statements.

### Errors and Exceptions

- Use the Result pattern, specifically CSharpFunctionalExtensions.Result<T>.
- Represent domain errors with the Result pattern and some implementation of Error.
- Error should be an abstract class with a code and a friendly description.
- Specific domain errors must inherit from Error and optionally add more fields they might need.
- For terminal domain failures, throw custom exceptions dedicated to the domain or concern.
- Handle external dependency errors gracefully, logging them and converting them into domain-aware errors.
- Categorize errors as either meant for support or meant for consumer information.

## Naming Conventions

- Naming conventions are inline with conventional C#
- Custom name conventions are contained in [.editorconfig](../.editorconfig)
- C# code should CamelCase acronyms and never keep them as upper-case
- SQL and C# naming conventions are not necessarily the same.

### Class names

- Avoid classes with plural names. Classes should represent atomic concepts with a single purpose
- Names must be postfixed with their intent, i.e. BudgetServiceCollectionExtensions or EventValidator
- Avoid naming objects things like SomethingService or SomethingManager that are prone to pulling unrelated logic to them. Prefer names like TemplateProvider, TemplateCompiler and FileStorageClient for example. Ask if you think the name might break this rule and you are struggling with a purposeful name
- Methods should be named as verbs, i.e. CheckIfDirty, Validate or SendForAcknowledgement
- Async methods should be post-fixed with Async in its name
- Properties should not have plural names if they are not enumerable or some form of collection
- Properties should never be named for verbs
- Properties should where possible be named as nouns

### Test name conventions

- For tests the plural name rule MUST be ignored and the tests should be named `SomeCommandHandlerTests` for example.
- Test classes MUST be named for their test subject with the postfix `Tests`.
- Test method names MUST follow the pattern: `GivenSomething_WhenSomeCondition_AndOrSomeOptionalCondition`.
- Test method names MUST never contain the expectation as part of the name.
- Don't name the test subject field `_sut`, `_service` or some other generic name. Give it a name representing its intent, i.e. `_authService`.
- Multiple assertions are grouped inside and expectation using the following naming convention: `ItShouldMeetSomeCondition`.
- Expectation methods should have meaningful names like: `ItShouldExplainWhatWentWrong` or `ItShouldPersistTheData`.

### Database naming conventions

- When modelling the EF Core model, override names to keep consistent Database naming conventions
- EF Core DbSets should have plural names
- Acronyms like ID should be all capitals for SQL but be CamelCase for C# code, i.e. EventId in C# and EventID in SQL
- Primary Keys should show which table it is for: `PK_<TableName>`
- Foreign Keys should always clearly represent the link they represent: `FK_<TargetTable>_<SourceTable>`
- Unique Keys should show which columns is being constrained: `UQ_<TableName>_<ColumnName(s)>`
- Default constraints should clearly explain which table and column the default is for: `DF_<TableName>_<ColumnName>`
- Check constrains should show which columns on which table is being checked for what reason: `CHK_<TableName>_<ColumnName>_<Reason>`

## Testing

- Use XUnit 3, Shouldly and NSubstitute for unit and integration tests.
- For database-dependent tests, use TestContainers (not in-memory) and Respawn to reset state.
- API tests must:
  - Use WebApplicationFactory or similar more modern means of writing real integration tests
  - Include mock authentication providers for role-based access overrides
  - Simulate various user roles and claims
  - Assert all OpenAPI documented response paths
  - Reuse and abstract common logic

## Data access

- Use EF Core for relational storage unless otherwise specified.
- Use extension methods for domain logic entry points.
- Clearly separate fetching of mutable vs immutable data.
- Support pagination with shared utility classes/extensions.
- Consider PII protections like data masking or column-level encryption.
- Model rich domain models to the data store
- Do not use empty default constructors for domain models but use private constructors and private setters with backing fields for hydration

## Feature management

- Implement feature toggles as a core design principle.
- Support multi-level toggling, including A/B testing and group targeting.
- Emit behavioural analytics optionally on feature usage (e.g., Application Insights or domain event streams).
- Treat each vertical slice module as a toggleable unit.

## Configuration and wiring

- Use the IOptions pattern with validation for config binding.
- Favour .NET Aspire for dependency wiring and environment-based service binding, while keeping domain code agnostic to Aspire.
- Keep the Aspire AppHost up to date with changes
- Use the new slnx format for solution files

## Logging and observability

- Log only when necessary using LoggerMessage source generators.
- Use descriptive log messages and names
- Allow users to opt-in to sensitive logging.
- Integrate OpenTelemetry for structured traces and metrics.
- Ensure trace context propagation and Prometheus compatibility.
- Never leak PII in telemetry.
- Support sampling and environment-based filtering.

## Minimal APIs and endpoint design

- Use Minimal APIs, grouped by concern into dedicated classes.
- Use kebab-case naming convention
- Follow CQRS separation between command and query endpoints.
- Generate OpenAPI/Swagger documentation with all relevant response types.
- Include PKCE-enabled Swagger UI for API interaction.
- Use message-based workflows (e.g., background queues, event dispatch) for latency-sensitive operations.

## Authentication and authorization

- Use Microsoft Entra ID (formerly Azure AD).
- Implement policy-based access control (PBAC) using roles, scopes, and claims.
- Policies should be testable and modular.
- Tests must mock claims and policy evaluators.

## DevOps & CI/CD

- Use GitHub Actions or Azure DevOps (ask which applies) to enforce:
- Linting
- Test execution
- Security scanning
- Dependency validation
- Keep the solution CI/CD-friendly and automatable.
- Use tools like Dependabot, NuKeeper or Renovate to manage third-party upgrades.
- Prefer centralized dependency versioning to avoid drift.

## Technical decision tracking

- Track architectural decisions, TODOs, and technical debt inline or via doc annotations.
- When revisiting files, prompt for follow-up on outstanding notes.

## Documentation

- Written in Markdown, organized using the Divio four-tier model:
  - Tutorials
  - How-to Guides
  - Technical Reference
  - Explanations
- Include PlantUML C4 diagrams and contextual flowcharts.
- Maintain a consistent visual style.
- Add gotchas or edge cases using quote blocks like:

  ```text
  ℹ️ Gotcha: Title
  > This scenario may break if ...
  ```
