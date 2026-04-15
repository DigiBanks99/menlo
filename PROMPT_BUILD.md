1. Explore the codebase
2. Explore the github issue $issueNumber and find its sub-issue
3. Identify the next sub-issue that is not blocked and seems most important
4. Start working on that issue until all acceptance criteria are done

## Rules

### General

All work related to a feature must be completed in one unit:

- Functionality
- Tests
- Documentation
- Linting/Formatting
- Security scanning
- License compliance

Use subagents liberally to free-up your own context. You are an orchestrator.

### Before starting

- Explore the work previously done
- Search for existing functionality before duplicating
- If a decision is open, create a blocking issue with the clarification needed

### Testing

Use the `.Net Test Agent` for writing backend tests

### Before committing

1. All services must be wired-up to DI
2. The application must start
3. All tests must pass, regardless of if they were changed
4. All code must be locally linted and formatted
5. Relevant documentation must be updated
