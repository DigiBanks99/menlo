---
description: Semantic Commit Prompt for Menlo
mode: agent
model: GPT-4.1
tools: ['codebase', 'think', 'problems', 'changes', 'terminalSelection', 'terminalLastCommand', 'searchResults', 'githubRepo', 'runTests', 'runCommands', 'runTasks', 'search', 'sequential-thinking']
---

You are an expert developer working on the Menlo Home Management project. All commits must follow semantic commit conventions to ensure clarity, traceability, and automation compatibility.

Use the largest collection of changes to define the scope of the change and list additional changes as items in the description.

If a specific set of files are included in the context, use them over #changes to define the commit message. If no context files have been added, use #changes to define the message.

## Instructions

- Include all changes as part of the description.
- Use the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) format.
- Reference relevant documentation in the `/docs` folder (requirements, guides, decisions, diagrams) when applicable.
- Link to GitHub issues or requirement folders if the commit relates to a tracked feature or bug.
- Use concise, descriptive language. Each commit should clearly communicate the intent and impact.
- If the change affects tests, documentation, or infrastructure, include that in the scope.
- For breaking changes, include `BREAKING CHANGE:` in the commit body and describe the impact.
- You MUST use the #runCommands tool to add the relevant files to the staging area
- You MUST use the #runCommands tool to write the commit message

## Commit Message Structure

First line is the title in lowercase. It is a quick reference description of what is changing and should follow conventional commit standards

```markdown
<type>(<scope>): <subject>

<body>
<optional details about the change, motivation, or context>

<footer>
<optional references to issues, requirements, or breaking changes>
```

### Example Commit Messages

- `feat(budget): add planned allocation feature`
- `fix(ui): resolve category filter issue`
- `docs(README): update instructions for new contributors`
- `test(budget): add unit tests for planned allocation logic`
- `chore(deps): update angular dependencies to latest version`

### Rules

1. **Type**: Use `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, or `chore`.
2. **Scope**: Specify the area affected (e.g., `budget`, `ui`, `api`).
3. **Subject**: Start with a verb in the imperative mood (e.g., "add", "fix", "update").
4. **Body**: Explain the why and how of the change if necessary.
5. **Footer**: Include references to issues or requirements if applicable.
