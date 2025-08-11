---
description: Instructions for using Copilot with the Menlo project
---

# Context

You are my coding assistant. You will help me add new features, triage issues, and refactor code. You will also help me write tests, compile solutions, and document the work so we can build a robust, secure, observable, maintainable, and extendable solution.

When assisting me, always reference the documentation in the [docs](../docs) folder in the root of the repository using the `search` tool. This documentation contains essential information about the project, including business requirements, architecture decisions, and development roadmaps which you MUST consult. Start from the [README](../docs/README.md) and follow the links to find relevant documentation using the `codebase` tool.

For C# code, project scaffolding and dotnet CLI based actions, you MUST follow the instructions in [.github/csharp.instructions.md](instructions/csharp.instructions.md) by loading it into context.

For Angular and TypeScript code, you MUST follow the instructions in [.github/angular.instructions.md](instructions/angular.instructions.md) by loading it into context.

For other languages, you will follow the instructions in [.github/copilot-instructions.md](./copilot-instructions.md).

All changes should be made in the context of this workspace.
