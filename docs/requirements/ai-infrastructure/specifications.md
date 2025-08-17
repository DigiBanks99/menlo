# AI Infrastructure — Specifications

## Business Requirements

- Local-first AI: All AI inference must run on the home server via Ollama; no external AI APIs are permitted.
- "Blueberry Muffin" enablement: AI enhances existing workflows (budgeting, planning lists, transactions) without interrupting them.
- Cost-conscious operation: Prefer small, efficient models (Phi-4-mini, Phi-4-vision) that run within home server constraints.
- Privacy-first: No PII or family data leaves the home server; no telemetry containing PII is sent externally.
- Traceable AI actions: All AI-generated suggestions and decisions must be reviewable, reversible, and auditable.
- Degradation: If AI is unavailable, core workflows must continue to function with graceful fallbacks.

## Functional Requirements

- Model hosting
  - Ollama hosts approved models: Phi-4-mini (text), Phi-4-vision (vision)
  - Models are version pinned; upgrades are controlled and documented
- Orchestration
  - Microsoft Semantic Kernel coordinates prompts, tools, and multi-step reasoning
  - Domain services expose AI via interfaces (e.g., `IBudgetAnalyser`, `TransactionCategorizer`) injected into aggregates/handlers
- Data contracts
  - All prompts and outputs use explicit, typed contracts to avoid brittle parsing (JSON schemas where applicable)
  - Input sanitisation and output validation for safety and reliability
- Budgeting assistants
  - BudgetAnalyser: suggest planned amounts, detect anomalies, forecast seasonality
  - TransactionCategorizer: predict category, attribution (main vs rental), payer/contributor hints
  - ListInterpreter: interpret handwritten planning list photos; extract items, costs, intent
  - Recommendation memory: learn from user corrections (feedback loop)
- Storage & audit
  - Persist AI suggestions, user actions, and feedback in PostgreSQL with links to domain entities (budget, category, transaction)
  - Maintain provenance: model, version, prompt hash, timestamp, user id
- Security
  - Entra ID-authenticated API; no anonymous access to AI endpoints
  - No PII in logs; redact sensitive fields
- Operations
  - Health checks for Ollama and orchestration layer
  - Feature toggles to enable/disable assistants per environment

## Considerations

- Constrained environment: Handle model warm-up, memory pressure, and timeouts; provide backpressure or queued processing for heavy tasks
- Explainability: Surface why a suggestion was made (top features or matched patterns) where feasible
- Internationalisation: Prefer model prompts that are locale-aware (South African context)
- Testing: Include golden-test prompts and deterministic seeds for regression where supported

## Dependencies

- Architecture and principles per `docs/explanations/architecture-document.md`
- Related requirements: `budget-aggregate-minimum`, `budget-create-vertical`, `budget-item`, `budget-categories-vertical`

## Diagrams

- See `diagrams/ai-flow.md` for component and data flow.
