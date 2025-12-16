# Angular Result Pattern — How To

Audience: developers and automated agents who need a concise, actionable guide showing why and how to adopt the Result pattern implemented in `shared-util`.

This document assumes you have the library sources at `shared-util/src/lib` and are familiar with Angular's `HttpClient`, RxJS, and Signals.

- [Angular Result Pattern — How To](#angular-result-pattern--how-to)
  - [Why use the Result pattern](#why-use-the-result-pattern)
  - [Quick decisions](#quick-decisions)
  - [Example](#example)
    - [Service](#service)
    - [Component](#component)
  - [Testing guidance](#testing-guidance)
  - [References](#references)

## Why use the Result pattern

- Predictable control flow: streams always emit a `Result` value instead of throwing, so downstream code doesn't need try/catch or error callbacks.
- Strong typing: `Result<T, E>` provides compile-time guarantees about success vs failure shapes.
- Easier testing: returning `Result` values from services simplifies unit tests (no need to mock thrown errors or HTTP error handlers).
- Clear UX handling: helpers in `problem-details.ts` make validation vs network errors explicit so UI code can consistently map errors to forms or toasts.

## Quick decisions

- Use `toResult()` at the HTTP boundary (service layer).
- Keep `Result` values in Signals at the UI layer and derive messages using `computed()`.
- Use `getValidationErrors()` / `mapValidationErrorsToForm()` to map server validation errors to controls.

## Example

The example below shows a minimal `BudgetService` and a `BudgetCreateComponent` (both fully self-contained). These examples use only the public API implemented in the library.

### Service

```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { toResult } from 'shared-util/src/lib/operators/to-result';
import type { Result } from 'shared-util/src/lib/types/result';
import type { ApiError } from 'shared-util/src/lib/types/problem-details';

export interface CreateBudgetCommand { name: string; amount: number }
export interface BudgetDto { id: string; name: string; amount: number }

@Injectable({ providedIn: 'root' })
export class BudgetService {
  constructor(private readonly http: HttpClient) {}

  createBudget(cmd: CreateBudgetCommand): Observable<Result<BudgetDto, ApiError>> {
    // toResult() guarantees the stream emits Result<BudgetDto, ApiError>
    return this.http.post<BudgetDto>('/api/budgets', cmd).pipe(toResult());
  }
}
```

- `toResult()` wraps successful responses with `success(value)` and converts errors using `toApiError()` into `failure(error)`.

### Component

```typescript
import { Component, signal, computed } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { BudgetService, CreateBudgetCommand } from './budget.service';
import { isFailure } from 'shared-util/src/lib/types/result';
import { isProblemError, getErrorMessage, mapValidationErrorsToForm, type ApiError } from 'shared-util/src/lib/types/problem-details';
import type { Result } from 'shared-util/src/lib/types/result';

@Component({ selector: 'app-budget-create', template: `<!-- omitted for brevity -->` })
export class BudgetCreateComponent {
  readonly result = signal<Result<BudgetDto, ApiError> | null>(null);
  readonly loading = signal(false);

  readonly form = new FormGroup({ name: new FormControl(''), amount: new FormControl(0) });

  constructor(private readonly svc: BudgetService) {}

  submit(): void {
    const cmd: CreateBudgetCommand = { name: this.form.value.name!, amount: this.form.value.amount! };
    this.loading.set(true);
    this.svc.createBudget(cmd).subscribe(r => {
      this.result.set(r);
      this.loading.set(false);

      // Only inspect `error` when the Result is a failure
      if (isFailure(r) && isProblemError(r.error)) {
        // Map server validation errors (camel-casing is done by helper)
        mapValidationErrorsToForm(r.error, this.form);
      }
    });
  }

  readonly errorText = computed(() => {
    const r = this.result();
    if (!r) return '';
    return isFailure(r) ? getErrorMessage(r.error) : '';
  });
}
```

Explanation of the component example:

- We keep the latest `Result` in a signal (`result`).
- `errorText` derives a user-friendly message via `getErrorMessage()` (which internally prefers `problem.detail`, `problem.title`, etc.).
- When we receive a `Problem` `ApiError`, we call `mapValidationErrorsToForm()` to set control errors. This helper expects the same `FormGroup` API used in Angular and will camel-case server field names.

## Testing guidance

- Service tests: mock `HttpClient` to return an `of(mockDto)` and assert the observable emits a `Success` result.
- Also mock an HTTP error that contains `ProblemDetails` and assert the observable emits a `Failure` result with `kind: 'problem'`.
- Component tests: stub the `BudgetService.createBudget()` to emit specific `Result` objects (success, problem, network) and assert UI behaviour (control errors set, toast invoked, etc.).

## References

- API reference: [Angular Result Pattern API Reference](../reference/angular-result-pattern-api.md)
- Implementation plan & rationale: [Implementation plan](../requirements/angular-result-pattern/implementation.md)
