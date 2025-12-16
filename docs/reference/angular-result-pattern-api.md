# Angular Result Pattern

This document lists the public types and functions implemented in `shared-util/src/lib`.

See the developer how-to for examples and rationale: [How-to guide](../guides/angular-result-pattern-howto.md)
and the implementation plan: [Implementation plan](../requirements/angular-result-pattern/implementation.md)

## Result utilities (types/result.ts)

### Core types

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

## ApiError & ProblemDetails (types/problem-details.ts)

### Core types

- `ProblemDetails` — RFC 7807 shape with optional `errors?: Record<string, string[]>` and `traceId`.
- `ProblemApiError` — `{ kind: 'problem'; problem: ProblemDetails; status?: number }`
- `NetworkApiError` — `{ kind: 'network'; status?: number; message: string; originalError?: unknown }`
- `UnknownApiError` — `{ kind: 'unknown'; message: string; originalError?: unknown }`

### Factories

- `problemError(problem: ProblemDetails, status?: number): ProblemApiError`
- `networkError(status?: number, message?: string, originalError?: unknown): NetworkApiError`
- `unknownError(message?: string, originalError?: unknown): UnknownApiError`

### Guards & helpers

- `isProblemDetails(value: unknown): value is ProblemDetails`
- `isProblemError(e: ApiError): e is ProblemApiError`
- `isNetworkError(e: ApiError): e is NetworkApiError`
- `isUnknownError(e: ApiError): e is UnknownApiError`
- `hasValidationErrors(e: ApiError): boolean` — true when `problem.errors` exists and is non-empty
- `getValidationErrors(e: ApiError): Record<string, string[]>` — returns validation map or `{}`
- `mapValidationErrorsToForm(e: ApiError, form: { get(path: string): { setErrors(obj: object): void; markAsTouched(): void } | null }, options?: { markAsTouched?: boolean }): void`

### Conversion & messaging

- `toApiError(error: unknown): ApiError` — normalises thrown errors and HttpErrorResponse-like shapes (duck-typed)
- `getErrorMessage(e: ApiError, defaultMessage?: string): string`
- `getErrorStatus(e: ApiError): number | undefined`

## RxJS Operators (operators/to-result.ts)

- `toResult<T>(): OperatorFunction<T, Result<T, ApiError>>` — converts next/error paths into `Result` values
- `toResultWith<T, E>(errorMapper: (error: unknown) => E): OperatorFunction<T, Result<T, E>>` — custom mapper variant
- `unwrapResult<T, E>(): OperatorFunction<Result<T, E>, T>` — emits value or errors
- `filterSuccess<T, E>(): OperatorFunction<Result<T, E>, T>` — emits only successes
- `filterFailure<T, E>(): OperatorFunction<Result<T, E>, E>` — emits only failures

## Files

- `src/ui/web/projects/shared-util/src/lib/types/result.ts`
- `src/ui/web/projects/shared-util/src/lib/types/problem-details.ts`
- `src/ui/web/projects/shared-util/src/lib/operators/to-result.ts`

## Examples

Import paths shown use a monorepo-local path; adapt to your package entrypoint when published.

### Service (HttpClient + toResult)

```typescript
import { toResult } from 'projects/shared-util/src/lib/operators/to-result';

// returns Observable<Result<BudgetDto, ApiError>>
this.http.post<BudgetDto>('/api/budgets', cmd).pipe(toResult());
```

### Result handling in a component (Signals)

```typescript
import { signal, computed } from '@angular/core';
import { Result, isFailure } from 'projects/shared-util/src/lib/types/result';
import { ApiError, getErrorMessage } from 'projects/shared-util/src/lib/types/problem-details';

const result = signal<Result<UserDto, ApiError> | null>(null);
service.getUser(id).subscribe(r => result.set(r));

const errorText = computed(() => {
  const r = result();
  if (!r) return '';
  return isFailure(r) ? getErrorMessage(r.error) : '';
});
```

## Where this lives

See `src/ui/web/projects/shared-util/src/lib` for source code and tests.
