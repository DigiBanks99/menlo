# Angular Result Pattern — Implementation Plan

A minimal, framework-native approach that is easy to adopt and test.

## Contracts (TypeScript)

- `Result<T, E>`:
  - constructors:
    - `success`
    - `failure`
  - and properties:
    - `isSuccess`
    - `isFailure`
    - `value`
    - `error`
  - and monadic functions:
    - `map`
    - `mapErr`
    - `bind`
    - `tap`
    - `tapErr`
    - `compensate`
- `ProblemDetails` and `ApiError` discriminated union like ASP.NET core ProblemDetails and RFC7807 mapping
- `toApiError(e: unknown): ApiError`
- `toResult<T>(): OperatorFunction<T, Result<T, ApiError>>`

## Integration points

- Services return `Observable<Result<Dto, ApiError>>`
- Components use Signals for `pending` and `result`
- Validation errors map to typed reactive forms via `setErrors({ api: message })`
- Non-validation errors surfaced via toast/snackbar

## Step-by-step tasks

1. Add shared utilities under `src/ui/web/projects/shared-utils/src/lib/types/result.ts`
1. Add API error types and converter under `src/ui/web/projects/shared-utils/src/lib/types/problem-details.ts`
1. Add RxJS operator `src/ui/web/projects/shared-utils/src/lib/operators/to-result.ts`
1. Write unit tests for utilities, service, and component

## Acceptance

- All tests in `test-cases.md` are implemented and passing
- Lint clean and typed strict
- Inline validation and friendly error UX verified manually

> Gotcha
>
> - Avoid throwing errors from services for expected API failures; always return `Result`.
> - Ensure form control names match backend problem `errors` keys to map correctly.
