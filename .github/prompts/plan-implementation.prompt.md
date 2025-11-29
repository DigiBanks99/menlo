---
description: Plan the implementation details for a feature specification
model: GPT-5 (Preview)
tools: ['edit/createFile', 'edit/createDirectory', 'edit/editFiles', 'search', 'runCommands', 'runTasks', 'AngularCLI/*', 'github/get_code_scanning_alert', 'github/get_commit', 'github/get_dependabot_alert', 'github/get_file_contents', 'github/get_issue', 'github/get_issue_comments', 'github/get_job_logs', 'github/get_label', 'github/get_me', 'github/get_project', 'github/get_project_field', 'github/get_project_item', 'github/get_secret_scanning_alert', 'github/get_workflow_run', 'github/get_workflow_run_logs', 'github/list_branches', 'github/list_code_scanning_alerts', 'github/list_commits', 'github/list_projects', 'github/list_pull_requests', 'github/list_secret_scanning_alerts', 'github/list_workflow_jobs', 'github/list_workflow_runs', 'github/list_workflows', 'github/search_code', 'github/search_issues', 'github/search_pull_requests', 'github/search_repositories', 'github/update_issue', 'MicrosoftDocs/*', 'nuget/*', 'sequential-thinking/*', 'usages', 'problems', 'changes', 'testFailure', 'openSimpleBrowser', 'fetch', 'githubRepo', 'mermaidchart.vscode-mermaid-chart/get_syntax_docs', 'mermaidchart.vscode-mermaid-chart/mermaid-diagram-validator', 'mermaidchart.vscode-mermaid-chart/mermaid-diagram-preview', 'todos', 'runTests']
---

You must collaboratively plan the implementation details for the feature specification provided after the "Specification: " section. You must ensure the prompt has the requirement context from the specific #docs/requirements folder before proceeding.

You MUST create or update the implementation.md file in the correct requirement subfolder as specified in the #file:docs/README.md. If the implementation.md file does not exist you have not been provided the correct context folder and you must stop and inform the user the correct context folder must be provided and you MUST NOT proceed further.

This project has no budget, time, handover or redundancy constraints so we will not need any requirements along those topics. We only need implementation details and considerations.

Research best-practices and ensure you are considering the #file:entity-design.md  and other #file:requirements when you need to clarify, need to find patterns and be consistent.

Specification: 
