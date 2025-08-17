# Angular Result Pattern

Standardise a lightweight Result pattern for Angular to ensure consistent error handling, predictable UI state, and alignment with Menlo’s guidance on signals, typed forms, and ProblemDetails-based APIs.

## Business requirements

- Provide a consistent, predictable way to handle success and failure across Angular features without throwing exceptions across component/service boundaries.
- Improve user experience by mapping API validation errors to form controls and showing meaningful messages for non-validation errors.
- Enhance maintainability and testability through explicit, typed success/failure flows and simple helpers.
- Align with existing project guidance: Angular Signals for state, typed reactive forms, and APIs that return ProblemDetails.
- Support incremental adoption across features (no big-bang migration) and keep the approach framework-native and lightweight.

## Functional requirements

1. Result core
   - Define a generic `Result<T, E>` union with constructors (`ok`, `err`) and helpers (`isOk`, `isErr`, `map`, `mapErr`, `unwrapOr`).
2. API error normalisation
   - Represent API failures as a typed `ApiError` discriminated union with at least:
     - `problem` for RFC7807 ProblemDetails (including `errors` for validation),
     - `network` for connectivity/HTTP transport issues,
     - `unknown` for anything else.
   - Convert Angular HttpClient errors into `ApiError` with a small utility.
3. HttpClient integration
   - Provide an RxJS operator that converts `Observable<T>` from HttpClient into `Observable<Result<T, ApiError>>`, catching errors and returning `err(ApiError)` instead of throwing.
4. Service contracts
   - Feature services must return `Observable<Result<Dto, ApiError>>` for create/read/update/delete operations.
   - No service method should throw for expected API errors; those must be represented as `Result` values.
5. Component state with signals
   - Components should use Angular Signals to represent pending state and the latest `Result`.
   - Derived signals or computed values must provide user-friendly error text for non-validation errors.
6. Validation error mapping
   - When `ApiError.kind === 'problem'` and `problem.errors` is present, map field errors to the corresponding typed reactive form controls via `setErrors`.
   - Non-field errors (e.g., summary) must be surfaced in a toast/snackbar per the Angular instructions; validation errors remain inline.
7. UX consistency
   - Show progress/disabled states during pending operations.
   - On success, follow the feature’s navigation/refresh semantics (e.g., navigate to the created resource view).
8. Logging and observability
   - Do not log PII in the browser; keep logs minimal and useful for support.
   - Errors surfaced to users must be friendly; technical details should stay out of the UI.
9. Compatibility and adoption
   - The pattern must work with Angular’s standalone components, strict TypeScript, and signals.
   - The pattern must be incrementally adoptable (old code can coexist).

## Considerations

- ProblemDetails mapping should tolerate absent or partial fields and default gracefully.
- Global HTTP interceptors should remain compatible; the Result pattern must not rely on interceptor-specific behaviour.
- Internationalisation (i18n) of error messages should be supported by mapping server titles/keys to client-localised strings where applicable.
- Accessibility (a11y): inline validation must be aria-friendly; toasts should be accessible and non-blocking.
- SSR/Pre-rendering: utilities must be platform-agnostic and avoid direct DOM access.
- Performance: helpers are lightweight and tree-shakeable; avoid heavy dependencies.
- Security: never echo server-provided HTML directly into the DOM; render text content only.
