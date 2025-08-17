# Angular Result Pattern — Implementation Plan

A minimal, framework-native approach that is easy to adopt and test.

## Contracts (TypeScript)

- `Result<T, E>` utilities: `ok`, `err`, `isOk`, `isErr`, `map`, `mapErr`, `unwrapOr`
- `ProblemDetails` and `ApiError` discriminated union
- `toApiError(e: unknown): ApiError`
- `toResult<T>(): OperatorFunction<T, Result<T, ApiError>>`

## Integration points

- Services return `Observable<Result<Dto, ApiError>>`
- Components use Signals for `pending` and `result`
- Validation errors map to typed reactive forms via `setErrors({ api: message })`
- Non-validation errors surfaced via toast/snackbar

## Step-by-step tasks

1. Add shared utilities under `shared/utils/result.ts`
1. Add API error types and converter under `shared/api/problem-details.ts`
1. Add RxJS operator `shared/api/to-result.ts`
1. Update BudgetService to use `toResult` and return `Observable<Result<...>>`
1. Implement CreateBudget component using signals and map validation errors to controls
1. Write unit tests for utilities, service, and component

## Acceptance

- All tests in `test-cases.md` are implemented and passing
- Lint clean and typed strict
- Inline validation and friendly error UX verified manually

> Gotcha
>
> - Avoid throwing errors from services for expected API failures; always return `Result`.
> - Ensure form control names match backend problem `errors` keys to map correctly.
