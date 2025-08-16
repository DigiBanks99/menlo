---
description: Plan the implementation details for a feature specification
mode: CodePlanner
model: GPT-5 (Preview)
tools: ['codebase', 'usages', 'think', 'problems', 'changes', 'testFailure', 'terminalSelection', 'terminalLastCommand', 'openSimpleBrowser', 'fetch', 'findTestFiles', 'searchResults', 'githubRepo', 'runTests', 'editFiles', 'search', 'runCommands', 'runTasks', 'MicrosoftDocs', 'AngularCLI', 'sequential-thinking', 'get_syntax_docs', 'mermaid-diagram-validator', 'mermaid-diagram-preview', 'azure_summarize_topic', 'azure_query_azure_resource_graph', 'azure_get_schema_for_Bicep', 'azure_recommend_service_config', 'azure_get_dotnet_template_tags', 'azure_design_architecture', 'azure_check_region_availability', 'azure_check_quota_availability']
---

You must collaboratively plan the implementation details for the feature specification provided after the "Specification: " section. You must ensure the prompt has the requirement context from the specific #docs/requirements folder before proceeding.

You MUST update the implementation.md file in the correct requirement subfolder as specified in the #file:docs/README.md. If the implementation.md file does not exist you have not been provided the correct context folder and you must stop and inform the user the correct context folder must be provided and you MUST NOT proceed further.

This project has no budget, time, handover or redundancy constraints so we will not need any requirements along those topics. We only need implementation details and considerations.

Research best-practices and ensure you are considering the #file:entity-design.md  and other #file:requirements when you need to clarify, need to find patterns and be consistent.

Specification: 
