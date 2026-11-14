# Angular Result Pattern

The Result pattern lets Angular code handle success and failure as values instead of thrown errors. API calls return `Observable<Result<T, ApiError>>`, and components hold the latest `Result` in a signal.

Everything here is exported from the `shared-util` package (`src/ui/web/projects/shared-util`). The API clients in `data-access-menlo-api` return Results through `toResult()`.

See [How to use the Result pattern](../guides/angular-result-pattern-howto.md) for examples.

## Result utilities

- `Success<T>` — discriminant `{ isSuccess: true; value: T }`
- `Failure<E>` — discriminant `{ isFailure: true; error: E }`
- `Result<T, E = Error>` — `Success<T> | Failure<E>`

### Factory functions

- `success<T>(value: T): Success<T>`
- `failure<E>(error: E): Failure<E>`

### Guards and helpers

- `isSuccess<T, E>(r: Result<T, E>): r is Success<T>`
- `isFailure<T, E>(r: Result<T, E>): r is Failure<E>`
- `map<T, U, E>(r: Result<T, E>, fn: (v: T) => U): Result<U, E>`
- `mapErr<T, E, F>(r: Result<T, E>, fn: (e: E) => F): Result<T, F>`
- `bind<T, U, E>(r: Result<T, E>, fn: (v: T) => Result<U, E>): Result<U, E>`
- `tap<T, E>(r: Result<T, E>, fn: (v: T) => void): Result<T, E>`
- `tapErr<T, E>(r: Result<T, E>, fn: (e: E) => void): Result<T, E>`
- `compensate<T, E>(r: Result<T, E>, fn: (e: E) => Result<T, E>): Result<T, E>`

### Utilities

- `unwrap<T, E>(r: Result<T, E>): T` — throws on `Failure`
- `unwrapOr<T, E>(r: Result<T, E>, defaultValue: T): T`
- `unwrapOrElse<T, E>(r: Result<T, E>, fn: (e: E) => T): T`

### Combinators

- `combine<T, E>(results: Result<T, E>[]): Result<T[], E>` — short-circuits on first failure
- `combineAll<T, E>(results: Result<T, E>[]): Result<T[], E[]>` — collects all errors

### Bridging helpers

- `tryCatch<T, E = unknown>(fn: () => T, errorMapper?: (e: unknown) => E): Result<T, E>`
- `fromPromise<T, E = unknown>(promise: Promise<T>, errorMapper?: (e: unknown) => E): Promise<Result<T, E>>`

## ApiError & ProblemDetails

- `ProblemDetails` — RFC 7807 shape with optional `errors?: Record<string, string[]>` and `traceId`.
- `ProblemApiError` — `{ kind: 'problem'; problem: ProblemDetails; status?: number }`
- `NetworkApiError` — `{ kind: 'network'; status?: number; message: string; originalError?: unknown }`
- `UnknownApiError` — `{ kind: 'unknown'; message: string; originalError?: unknown }`

### Factories

- `problemError(problem: ProblemDetails, status?: number): ProblemApiError`
- `networkError(status?: number, message?: string, originalError?: unknown): NetworkApiError`
- `unknownError(message?: string, originalError?: unknown): UnknownApiError`

### Guards & helpers

- `isProblemError(e: ApiError): e is ProblemApiError`
- `isNetworkError(e: ApiError): e is NetworkApiError`
- `isUnknownError(e: ApiError): e is UnknownApiError`
- `hasValidationErrors(e: ApiError): boolean` — true when `problem.errors` exists and is non-empty
- `getValidationErrors(e: ApiError): Record<string, string[]>` — returns the validation map, with each key's first letter lower-cased (`Name` becomes `name`), or `{}`
- `mapValidationErrorsToForm(e: ApiError, form: { get(path: string): { setErrors(obj: object): void; markAsTouched(): void } | null }, options?: { markAsTouched?: boolean }): void` — sets `{ api: '<messages joined by ", ">' }` on each control that matches a validation key, and marks it touched unless `markAsTouched` is `false`. Keys with no matching control are ignored.

### Conversion & messaging

- `toApiError(error: unknown): ApiError` — normalises thrown errors and HttpErrorResponse-like shapes (duck-typed):
  - status `0` becomes a `network` error;
  - a body that looks like ProblemDetails (an object, or a JSON string, with a `type`, `title`, `status`, `detail` or `errors`) becomes a `problem` error;
  - any other HTTP error becomes a `network` error with its status;
  - anything else becomes an `unknown` error.
- `getErrorMessage(e: ApiError, defaultMessage?: string): string` — for a `problem`, the first non-empty of `detail`, `title` and `type`; otherwise the error's `message`; else `defaultMessage` ("An unexpected error occurred")
- `getErrorStatus(e: ApiError): number | undefined` — the HTTP status, falling back to `problem.status`

## RxJS Operators (operators/to-result.ts)

- `toResult<T>(): OperatorFunction<T, Result<T, ApiError>>` — converts next/error paths into `Result` values
- `toResultWith<T, E>(errorMapper: (error: unknown) => E): OperatorFunction<T, Result<T, E>>` — custom mapper variant
- `unwrapResult<T, E>(): OperatorFunction<Result<T, E>, T>` — emits the value, or errors the stream with the failure's error
- `filterSuccess<T, E>(): OperatorFunction<Result<T, E>, T>` — emits only successes
- `filterFailure<T, E>(): OperatorFunction<Result<T, E>, E>` — emits only failures

## Files

- `src/ui/web/projects/shared-util/src/lib/types/result.ts`
- `src/ui/web/projects/shared-util/src/lib/types/problem-details.ts`
- `src/ui/web/projects/shared-util/src/lib/operators/to-result.ts`

## Examples

### Service (HttpClient + toResult)

```typescript
import { ApiError, Result, toResult } from 'shared-util';

listBudgets(): Observable<Result<BudgetListItemResponse[], ApiError>> {
  return this.http.get<BudgetListItemResponse[]>(`${this.apiBaseUrl}/api/budgets`).pipe(toResult());
}
```

### Result handling in a component (Signals)

```typescript
import { computed, signal } from '@angular/core';
import { ApiError, getErrorMessage, isFailure, Result } from 'shared-util';

const result = signal<Result<BudgetResponse, ApiError> | null>(null);
budgetApi.getBudget(id).subscribe((r) => result.set(r));

const errorText = computed(() => {
  const r = result();
  return r && isFailure(r) ? getErrorMessage(r.error) : '';
});
```
