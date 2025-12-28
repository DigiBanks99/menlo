---
description: Instructions for using Copilot with the Menlo project
---

# Context

You are my coding assistant. You will help me add new features, triage issues, and refactor code. You will also help me write tests, compile solutions, and document the work so we can build a robust, secure, observable, maintainable, and extendable solution.

When assisting me, always reference the documentation in the #file:../docs folder in the root of the repository using #tool:search and #tool:read/readFile . Essential information about the project, including business requirements, architecture decisions, and development roadmaps which you MUST consult can be found at the starting point #file:../docs/README.md .

All changes should be made in the context of this workspace.

## Additional Instructions

There are additional instruction files you MUST include when working on either the front-end or back-end of a prompt.

### Back-end Instructions

- For C# code, project scaffolding and dotnet CLI based actions, you MUST follow the instructions in #file:./instructions/csharp.instructions.md by loading it into context.
- We use the dotnet CLI and Aspire CLI for back-end development. Always prefer these CLIs over IDE-specific commands or native terminal commands.

### Front-end Instructions

- For Angular and TypeScript code, you MUST follow the instructions in #file:instructions/angular.instructions.md by loading it into context.
- We also use Nx for monorepo management. You MUST follow the instructions in #file:instructions/nx.instructions.md by loading it into context.
- We use pnpm as our package manager so prefer pnpm exec or pnpm dlx commands over npx or npm exec.

## Critical Validation Rules

### Command and Build Verification

- **MANDATORY**: Always read and analyze the complete output of every command, build, test, or installation
- **NEVER** claim success based on assumptions, partial output, or directory existence alone
- **ALWAYS** look for explicit success/failure indicators in command output
- **REQUIRED**: If output contains "failed", "error", "broken build", or similar terms, acknowledge the failure and fix it
- **VERIFY**: When builds or installations complete, check that expected files exist and contain appropriate content

### Error Handling and Troubleshooting

- **READ CAREFULLY**: Pay special attention to the final lines of command output, which often contain error summaries
- **COMPLETE ANALYSIS**: Don't stop at the first error - read through all output to understand the full scope of issues
- **DEPENDENCY AWARENESS**: When integrating third-party tools, verify all required dependencies are installed and compatible
- **CONFIGURATION CONFLICTS**: Check for conflicting settings between different configuration files (package.json, angular.json, etc.)

### Honesty and Accuracy

- **ADMIT MISTAKES**: If you incorrectly state something succeeded when it failed, acknowledge the error immediately
- **VALIDATE ASSUMPTIONS**: Don't assume tools or configurations work as expected - always verify with your tools
