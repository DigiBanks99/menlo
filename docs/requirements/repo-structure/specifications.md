# Repo Scaffolding & Code Structure - Specifications

## Purpose

Define and enforce the directory layout, naming conventions, and architectural rationale for the Menlo codebase, ensuring maintainability, scalability, and alignment with the vertical slice architecture.

## Business Requirements

- The codebase must be easy to navigate for new and existing contributors.
- The structure must support rapid onboarding and clear separation of business domains.
- Documentation and code must be co-located for traceability.

## Technical Requirements

- All source code must reside under a top-level `src` folder.
- Backend APIs and related infrastructure must be placed under `src/api/`.
- The Aspire AppHost must also be placed under `src/api/`.
- Features are grouped by vertical slice (domain/feature), not by technical layer.
- Shared libraries and infrastructure live under `lib/`.
- UI implementations (e.g., web apps, mobile apps) are under `ui/`.
- Each vertical slice contains its own domain models, handlers, endpoints, and tests.
- Naming conventions and folder structure must follow the [Architecture Document](../../explanations/architecture-document.md#code-organization-structure).
- Aspire AppHost and service defaults must be present and up to date.
- All changes to the structure must be reflected in documentation and diagrams.

### Angular Structure

- The Angular frontend must reside under `src/ui/web/` .
- Use standalone components and organize code by feature/domain.
- Shared UI components and libraries should be placed under `src/ui/web/projects/menlo-lib/`.
- The main Angular application should be under `src/ui/web/projects/menlo-app/`.
- Follow Angular best practices for state management, signals, and strict typing as per [Angular Instructions](../../../.github/instructions/angular.instructions.md).

## References

- [Architecture Document](../../explanations/architecture-document.md#code-organization-structure)
- [Implementation Roadmap](../implementation-roadmap.md#infrastructure--database)
- [C4 Code Diagram](../../diagrams/c4-code-diagram.md)

## Non-Functional Requirements

- Structure must support CI/CD, automation, and testability.
- Must be extensible for new features and domains without major refactoring.
- Must support both local and cloud-based development workflows.
