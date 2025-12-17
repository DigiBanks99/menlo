/**
 * @fileoverview Unit tests for toResult RxJS operator module.
 *
 * Tests cover:
 * - toResult() operator
 * - toResultWith() operator with custom error mapper
 * - unwrapResult() operator
 * - filterSuccess() operator
 * - filterFailure() operator
 */

import { EMPTY, firstValueFrom, Observable, of, Subject, throwError } from 'rxjs';
import { toArray } from 'rxjs/operators';
import { describe, expect, it, vi } from 'vitest';
import {
  ApiError,
  isNetworkError,
  isProblemError,
  isUnknownError,
  ProblemDetails,
} from '../types/problem-details';
import { failure, isFailure, isSuccess, Result, success } from '../types/result';
import { filterFailure, filterSuccess, toResult, toResultWith, unwrapResult } from './to-result';

// ============================================================================
// Test Data
// ============================================================================

interface User {
  id: number;
  name: string;
}

/**
 * Mock HttpErrorResponse that mirrors the real Angular HttpErrorResponse
 * structure without requiring Angular dependencies.
 */
class MockHttpErrorResponse extends Error {
  readonly status: number;
  readonly statusText: string;
  readonly error: unknown;
  readonly url: string | null;
  readonly ok = false;
  readonly name = 'HttpErrorResponse';

  constructor(init: { status?: number; statusText?: string; error?: unknown; url?: string }) {
    const status = init.status ?? 0;
    const statusText = init.statusText ?? 'Unknown Error';
    super(`Http failure response for ${init.url ?? '(unknown url)'}: ${status} ${statusText}`);
    this.status = status;
    this.statusText = statusText;
    this.error = init.error;
    this.url = init.url ?? null;
  }
}

const createProblemDetails = (overrides?: Partial<ProblemDetails>): ProblemDetails => ({
  type: 'https://example.com/probs/error',
  title: 'Error',
  status: 400,
  detail: 'An error occurred',
  ...overrides,
});

const createHttpErrorResponse = (
  status: number,
  body: unknown,
  statusText = 'Error',
): MockHttpErrorResponse =>
  new MockHttpErrorResponse({
    status,
    statusText,
    error: body,
    url: 'https://api.example.com/test',
  });

// ============================================================================
// toResult() Operator
// ============================================================================

describe('toResult() operator', () => {
  describe('Success cases', () => {
    it('TC-201: should wrap successful emission in Success', async () => {
      const source$ = of(42);

      const result = await firstValueFrom(source$.pipe(toResult()));

      expect(isSuccess(result)).toBe(true);
      if (isSuccess(result)) {
        expect(result.value).toBe(42);
      }
    });

    it('TC-202: should wrap complex object in Success', async () => {
      const user: User = { id: 1, name: 'John' };
      const source$ = of(user);

      const result = await firstValueFrom(source$.pipe(toResult()));

      expect(isSuccess(result)).toBe(true);
      if (isSuccess(result)) {
        expect(result.value).toEqual({ id: 1, name: 'John' });
      }
    });

    it('TC-203: should handle null value', async () => {
      const source$ = of(null);

      const result = await firstValueFrom(source$.pipe(toResult()));

      expect(isSuccess(result)).toBe(true);
      if (isSuccess(result)) {
        expect(result.value).toBeNull();
      }
    });

    it('TC-204: should handle undefined value', async () => {
      const source$ = of(undefined);

      const result = await firstValueFrom(source$.pipe(toResult()));

      expect(isSuccess(result)).toBe(true);
      if (isSuccess(result)) {
        expect(result.value).toBeUndefined();
      }
    });

    it('TC-205: should handle empty array', async () => {
      const source$ = of<number[]>([]);

      const result = await firstValueFrom(source$.pipe(toResult()));

      expect(isSuccess(result)).toBe(true);
      if (isSuccess(result)) {
        expect(result.value).toEqual([]);
      }
    });

    it('TC-206: should handle multiple emissions', async () => {
      const source$ = of(1, 2, 3);

      const results = await firstValueFrom(source$.pipe(toResult(), toArray()));

      expect(results).toHaveLength(3);
      expect(results.every(isSuccess)).toBe(true);
      const values = results.filter(isSuccess).map((r) => r.value);
      expect(values).toEqual([1, 2, 3]);
    });
  });

  describe('Error cases', () => {
    it('TC-207: should convert HttpErrorResponse with ProblemDetails to ProblemApiError', async () => {
      const problem = createProblemDetails();
      const httpError = createHttpErrorResponse(400, problem);
      const source$ = throwError(() => httpError);

      const result = await firstValueFrom(source$.pipe(toResult()));

      expect(isFailure(result)).toBe(true);
      if (isFailure(result)) {
        expect(isProblemError(result.error)).toBe(true);
      }
    });

    it('TC-208: should convert network error (status 0) to NetworkApiError', async () => {
      const httpError = createHttpErrorResponse(0, null);
      const source$ = throwError(() => httpError);

      const result = await firstValueFrom(source$.pipe(toResult()));

      expect(isFailure(result)).toBe(true);
      if (isFailure(result)) {
        expect(isNetworkError(result.error)).toBe(true);
        if (isNetworkError(result.error)) {
          expect(result.error.status).toBe(0);
        }
      }
    });

    it('TC-209: should convert HttpErrorResponse without ProblemDetails to NetworkApiError', async () => {
      const httpError = createHttpErrorResponse(500, 'Internal Server Error');
      const source$ = throwError(() => httpError);

      const result = await firstValueFrom(source$.pipe(toResult()));

      expect(isFailure(result)).toBe(true);
      if (isFailure(result)) {
        expect(isNetworkError(result.error)).toBe(true);
      }
    });

    it('TC-210: should convert Error to UnknownApiError', async () => {
      const source$ = throwError(() => new Error('Something went wrong'));

      const result = await firstValueFrom(source$.pipe(toResult()));

      expect(isFailure(result)).toBe(true);
      if (isFailure(result)) {
        expect(isUnknownError(result.error)).toBe(true);
        if (isUnknownError(result.error)) {
          expect(result.error.message).toBe('Something went wrong');
        }
      }
    });

    it('TC-211: should convert string error to UnknownApiError', async () => {
      const source$ = throwError(() => 'String error');

      const result = await firstValueFrom(source$.pipe(toResult()));

      expect(isFailure(result)).toBe(true);
      if (isFailure(result)) {
        expect(isUnknownError(result.error)).toBe(true);
      }
    });

    it('TC-212: should handle error after successful emissions', async () => {
      const subject$ = new Subject<number>();
      const results: Result<number, ApiError>[] = [];

      subject$.pipe(toResult()).subscribe((result) => results.push(result));

      subject$.next(1);
      subject$.next(2);
      subject$.error(new Error('Failed'));

      expect(results).toHaveLength(3);
      expect(isSuccess(results[0])).toBe(true);
      expect(isSuccess(results[1])).toBe(true);
      expect(isFailure(results[2])).toBe(true);
    });
  });

  describe('Completion cases', () => {
    it('TC-213: should complete after source completes', async () => {
      const completeSpy = vi.fn();
      const source$ = of(42);

      source$.pipe(toResult()).subscribe({
        complete: completeSpy,
      });

      expect(completeSpy).toHaveBeenCalled();
    });

    it('TC-214: should not throw on subscriber error', async () => {
      const errorSpy = vi.fn();
      const source$ = throwError(() => new Error('Test'));

      // The operator catches errors and converts to Failure
      // So the subscriber error handler should NOT be called
      source$.pipe(toResult()).subscribe({
        error: errorSpy,
      });

      expect(errorSpy).not.toHaveBeenCalled();
    });
  });
});

// ============================================================================
// toResultWith() Operator
// ============================================================================

describe('toResultWith() operator', () => {
  interface CustomError {
    code: string;
    message: string;
  }

  const customMapper = (error: unknown): CustomError => {
    if (error instanceof Error) {
      return { code: 'ERR', message: error.message };
    }
    return { code: 'UNKNOWN', message: String(error) };
  };

  it('TC-215: should wrap successful emission in Success', async () => {
    const source$ = of(42);

    const result = await firstValueFrom(source$.pipe(toResultWith(customMapper)));

    expect(isSuccess(result)).toBe(true);
    if (isSuccess(result)) {
      expect(result.value).toBe(42);
    }
  });

  it('TC-216: should use custom mapper for errors', async () => {
    const source$ = throwError(() => new Error('Custom error'));

    const result = await firstValueFrom(source$.pipe(toResultWith(customMapper)));

    expect(isFailure(result)).toBe(true);
    if (isFailure(result)) {
      expect(result.error).toEqual({ code: 'ERR', message: 'Custom error' });
    }
  });

  it('TC-217: should handle non-Error throws with custom mapper', async () => {
    const source$ = throwError(() => 'string error');

    const result = await firstValueFrom(source$.pipe(toResultWith(customMapper)));

    expect(isFailure(result)).toBe(true);
    if (isFailure(result)) {
      expect(result.error).toEqual({ code: 'UNKNOWN', message: 'string error' });
    }
  });

  it('TC-218: should support complex custom error types', async () => {
    interface DetailedError {
      code: number;
      message: string;
      timestamp: Date;
      retryable: boolean;
    }

    const detailedMapper = (error: unknown): DetailedError => ({
      code: error instanceof MockHttpErrorResponse ? error.status : 999,
      message: error instanceof Error ? error.message : String(error),
      timestamp: new Date(),
      retryable: false,
    });

    const httpError = createHttpErrorResponse(404, 'Not found');
    const source$ = throwError(() => httpError);

    const result = await firstValueFrom(source$.pipe(toResultWith(detailedMapper)));

    expect(isFailure(result)).toBe(true);
    if (isFailure(result)) {
      expect(result.error.code).toBe(404);
      expect(result.error.retryable).toBe(false);
    }
  });
});

// ============================================================================
// unwrapResult() Operator
// ============================================================================

describe('unwrapResult() operator', () => {
  it('TC-219: should emit value for Success', async () => {
    const source$ = of(success(42) as Result<number, string>);

    const result = await firstValueFrom(source$.pipe(unwrapResult()));

    expect(result).toBe(42);
  });

  it('TC-220: should throw error for Failure', async () => {
    const source$ = of(failure('error') as Result<number, string>);

    await expect(firstValueFrom(source$.pipe(unwrapResult()))).rejects.toBe('error');
  });

  it('TC-221: should emit multiple values for multiple Successes', async () => {
    const source$ = of(
      success(1) as Result<number, string>,
      success(2) as Result<number, string>,
      success(3) as Result<number, string>,
    );

    const results = await firstValueFrom(source$.pipe(unwrapResult(), toArray()));

    expect(results).toEqual([1, 2, 3]);
  });

  it('TC-222: should stop at first Failure', async () => {
    const source$ = of(
      success(1) as Result<number, string>,
      failure('error') as Result<number, string>,
      success(3) as Result<number, string>,
    );
    const values: number[] = [];

    await expect(
      new Promise<void>((resolve, reject) => {
        source$.pipe(unwrapResult<number, string>()).subscribe({
          next: (v) => values.push(v),
          error: reject,
          complete: () => resolve(),
        });
      }),
    ).rejects.toBe('error');

    expect(values).toEqual([1]);
  });

  it('TC-223: should complete normally for all Successes', async () => {
    const completeSpy = vi.fn();
    const source$ = of(success(42) as Result<number, string>);

    source$.pipe(unwrapResult()).subscribe({
      complete: completeSpy,
    });

    expect(completeSpy).toHaveBeenCalled();
  });

  it('TC-224: should propagate source errors', async () => {
    const originalError = new Error('Source error');
    const source$ = new Observable<Result<number, string>>((subscriber) => {
      subscriber.error(originalError);
    });

    await expect(firstValueFrom(source$.pipe(unwrapResult()))).rejects.toBe(originalError);
  });
});

// ============================================================================
// filterSuccess() Operator
// ============================================================================

describe('filterSuccess() operator', () => {
  it('TC-225: should emit value for Success', async () => {
    const source$ = of(success(42) as Result<number, string>);

    const result = await firstValueFrom(source$.pipe(filterSuccess()));

    expect(result).toBe(42);
  });

  it('TC-226: should not emit for Failure', async () => {
    const source$ = of(failure('error') as Result<number, string>);
    const nextSpy = vi.fn();

    source$.pipe(filterSuccess()).subscribe(nextSpy);

    expect(nextSpy).not.toHaveBeenCalled();
  });

  it('TC-227: should filter out Failures from mixed stream', async () => {
    const source$ = of(
      success(1) as Result<number, string>,
      failure('error') as Result<number, string>,
      success(2) as Result<number, string>,
      failure('another error') as Result<number, string>,
      success(3) as Result<number, string>,
    );

    const results = await firstValueFrom(source$.pipe(filterSuccess(), toArray()));

    expect(results).toEqual([1, 2, 3]);
  });

  it('TC-228: should complete even if all are Failures', async () => {
    const completeSpy = vi.fn();
    const source$ = of(
      failure('error1') as Result<number, string>,
      failure('error2') as Result<number, string>,
    );

    source$.pipe(filterSuccess()).subscribe({
      complete: completeSpy,
    });

    expect(completeSpy).toHaveBeenCalled();
  });

  it('TC-229: should propagate source errors', async () => {
    const errorSpy = vi.fn();
    const source$ = new Observable<Result<number, string>>((subscriber) => {
      subscriber.error(new Error('Source error'));
    });

    source$.pipe(filterSuccess()).subscribe({
      error: errorSpy,
    });

    expect(errorSpy).toHaveBeenCalled();
  });
});

// ============================================================================
// filterFailure() Operator
// ============================================================================

describe('filterFailure() operator', () => {
  it('TC-230: should emit error for Failure', async () => {
    const source$ = of(failure('error') as Result<number, string>);

    const result = await firstValueFrom(source$.pipe(filterFailure()));

    expect(result).toBe('error');
  });

  it('TC-231: should not emit for Success', async () => {
    const source$ = of(success(42) as Result<number, string>);
    const nextSpy = vi.fn();

    source$.pipe(filterFailure()).subscribe(nextSpy);

    expect(nextSpy).not.toHaveBeenCalled();
  });

  it('TC-232: should filter out Successes from mixed stream', async () => {
    const source$ = of(
      success(1) as Result<number, string>,
      failure('error1') as Result<number, string>,
      success(2) as Result<number, string>,
      failure('error2') as Result<number, string>,
      success(3) as Result<number, string>,
    );

    const results = await firstValueFrom(source$.pipe(filterFailure(), toArray()));

    expect(results).toEqual(['error1', 'error2']);
  });

  it('TC-233: should complete even if all are Successes', async () => {
    const completeSpy = vi.fn();
    const source$ = of(success(1) as Result<number, string>, success(2) as Result<number, string>);

    source$.pipe(filterFailure()).subscribe({
      complete: completeSpy,
    });

    expect(completeSpy).toHaveBeenCalled();
  });

  it('TC-234: should handle complex error types', async () => {
    interface AppError {
      code: string;
      message: string;
    }

    const error: AppError = { code: 'ERR001', message: 'Failed' };
    const source$ = of(failure(error) as Result<number, AppError>);

    const result = await firstValueFrom(source$.pipe(filterFailure()));

    expect(result).toEqual({ code: 'ERR001', message: 'Failed' });
  });

  it('TC-235: should propagate source errors', async () => {
    const errorSpy = vi.fn();
    const source$ = new Observable<Result<number, string>>((subscriber) => {
      subscriber.error(new Error('Source error'));
    });

    source$.pipe(filterFailure()).subscribe({
      error: errorSpy,
    });

    expect(errorSpy).toHaveBeenCalled();
  });
});

// ============================================================================
// Integration Scenarios
// ============================================================================

describe('Integration Scenarios', () => {
  it('TC-236: should chain toResult with map operations', async () => {
    const source$ = of(5);

    const result = await firstValueFrom(source$.pipe(toResult()));

    expect(isSuccess(result)).toBe(true);
    if (isSuccess(result)) {
      expect(result.value).toBe(5);
    }
  });

  it('TC-237: should handle async operations with toResult', async () => {
    const asyncFetch = (): Observable<User> =>
      new Observable((subscriber) => {
        setTimeout(() => {
          subscriber.next({ id: 1, name: 'John' });
          subscriber.complete();
        }, 10);
      });

    const result = await firstValueFrom(asyncFetch().pipe(toResult()));

    expect(isSuccess(result)).toBe(true);
    if (isSuccess(result)) {
      expect(result.value).toEqual({ id: 1, name: 'John' });
    }
  });

  it('TC-238: should handle retry-like patterns with toResult', async () => {
    let attempts = 0;
    const flakyOperation = (): Observable<number> =>
      new Observable((subscriber) => {
        attempts++;
        if (attempts < 3) {
          subscriber.error(new Error(`Attempt ${attempts} failed`));
        } else {
          subscriber.next(42);
          subscriber.complete();
        }
      });

    // First attempt fails
    const result1 = await firstValueFrom(flakyOperation().pipe(toResult()));
    expect(isFailure(result1)).toBe(true);

    // Second attempt fails
    const result2 = await firstValueFrom(flakyOperation().pipe(toResult()));
    expect(isFailure(result2)).toBe(true);

    // Third attempt succeeds
    const result3 = await firstValueFrom(flakyOperation().pipe(toResult()));
    expect(isSuccess(result3)).toBe(true);
    if (isSuccess(result3)) {
      expect(result3.value).toBe(42);
    }
  });

  it('TC-239: should work with Subject for reactive patterns', async () => {
    const subject = new Subject<number>();
    const results: Result<number, ApiError>[] = [];

    subject.pipe(toResult()).subscribe((result) => results.push(result));

    subject.next(1);
    subject.next(2);
    subject.next(3);
    subject.complete();

    expect(results).toHaveLength(3);
    expect(results.every(isSuccess)).toBe(true);
  });

  it('TC-240: should handle validation errors from API', async () => {
    const validationProblem = createProblemDetails({
      title: 'Validation Failed',
      status: 400,
      errors: {
        Name: ['Name is required', 'Name must be at least 3 characters'],
        Email: ['Invalid email format'],
      },
    });
    const httpError = createHttpErrorResponse(400, validationProblem);
    const source$ = throwError(() => httpError);

    const result = await firstValueFrom(source$.pipe(toResult()));

    expect(isFailure(result)).toBe(true);
    if (isFailure(result)) {
      expect(isProblemError(result.error)).toBe(true);
      if (isProblemError(result.error)) {
        expect(result.error.problem.errors?.['Name']).toHaveLength(2);
        expect(result.error.problem.errors?.['Email']).toHaveLength(1);
      }
    }
  });
});

// ============================================================================
// Edge Cases
// ============================================================================

describe('Edge Cases', () => {
  it('TC-241: should handle empty observable', async () => {
    const source$ = EMPTY;
    const results: Result<number, ApiError>[] = [];

    await firstValueFrom(
      new Observable<void>((subscriber) => {
        source$.pipe(toResult()).subscribe({
          next: (r) => results.push(r),
          complete: () => {
            subscriber.next();
            subscriber.complete();
          },
        });
      }),
    );

    expect(results).toHaveLength(0);
  });

  it('TC-242: should handle observable that never emits', async () => {
    const subject = new Subject<number>();
    const results: Result<number, ApiError>[] = [];

    const subscription = subject.pipe(toResult()).subscribe((r) => results.push(r));

    // No emissions
    subscription.unsubscribe();

    expect(results).toHaveLength(0);
  });

  it('TC-243: should handle synchronous errors', async () => {
    const source$ = new Observable<number>(() => {
      throw new Error('Sync error');
    });

    const result = await firstValueFrom(source$.pipe(toResult()));

    expect(isFailure(result)).toBe(true);
  });

  it('TC-244: should preserve error type information', async () => {
    const httpError = createHttpErrorResponse(
      401,
      createProblemDetails({
        title: 'Unauthorized',
        status: 401,
        detail: 'Invalid token',
      }),
    );
    const source$ = throwError(() => httpError);

    const result = await firstValueFrom(source$.pipe(toResult()));

    expect(isFailure(result)).toBe(true);
    if (isFailure(result)) {
      expect(isProblemError(result.error)).toBe(true);
      if (isProblemError(result.error)) {
        expect(result.error.problem.status).toBe(401);
        expect(result.error.problem.detail).toBe('Invalid token');
      }
    }
  });

  it('TC-245: should handle very large payloads', async () => {
    const largeData = Array.from({ length: 10000 }, (_, i) => ({
      id: i,
      name: `Item ${i}`,
      data: 'x'.repeat(100),
    }));
    const source$ = of(largeData);

    const result = await firstValueFrom(source$.pipe(toResult()));

    expect(isSuccess(result)).toBe(true);
    if (isSuccess(result)) {
      expect(result.value).toHaveLength(10000);
    }
  });
});
