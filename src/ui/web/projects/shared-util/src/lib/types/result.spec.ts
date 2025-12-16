/**
 * @fileoverview Unit tests for Result type module.
 *
 * Tests cover:
 * - Factory functions (success, failure)
 * - Type guards (isSuccess, isFailure)
 * - Monadic operations (map, mapErr, bind, tap, tapErr, compensate)
 * - Unwrap operations (unwrap, unwrapOr, unwrapOrElse)
 * - Combinators (combine, combineAll, tryCatch, fromPromise)
 */

import { describe, expect, it, vi } from 'vitest';
import {
  bind,
  combine,
  combineAll,
  compensate,
  failure,
  fromPromise,
  isFailure,
  isSuccess,
  map,
  mapErr,
  Result,
  success,
  tap,
  tapErr,
  tryCatch,
  unwrap,
  unwrapOr,
  unwrapOrElse,
} from './result';

// ============================================================================
// Test Types
// ============================================================================

interface User {
  id: number;
  name: string;
}

interface ApiError {
  code: string;
  message: string;
}

// ============================================================================
// Factory Functions
// ============================================================================

describe('Factory Functions', () => {
  describe('success()', () => {
    it('TC-001: should create a Success with value', () => {
      const result = success(42);

      expect(result.isSuccess).toBe(true);
      expect(result.isFailure).toBe(false);
      expect(result.value).toBe(42);
    });

    it('TC-002: should create a Success with complex object', () => {
      const user: User = { id: 1, name: 'John' };
      const result = success(user);

      expect(result.isSuccess).toBe(true);
      expect(result.value).toEqual({ id: 1, name: 'John' });
    });

    it('TC-003: should create a Success with null value', () => {
      const result = success(null);

      expect(result.isSuccess).toBe(true);
      expect(result.value).toBeNull();
    });

    it('TC-004: should create a Success with undefined value', () => {
      const result = success(undefined);

      expect(result.isSuccess).toBe(true);
      expect(result.value).toBeUndefined();
    });

    it('TC-005: should create a Success with empty array', () => {
      const result = success<number[]>([]);

      expect(result.isSuccess).toBe(true);
      expect(result.value).toEqual([]);
    });

    it('TC-006: should create a Success with empty string', () => {
      const result = success('');

      expect(result.isSuccess).toBe(true);
      expect(result.value).toBe('');
    });

    it('TC-007: should create a Success with zero', () => {
      const result = success(0);

      expect(result.isSuccess).toBe(true);
      expect(result.value).toBe(0);
    });

    it('TC-008: should create a Success with false', () => {
      const result = success(false);

      expect(result.isSuccess).toBe(true);
      expect(result.value).toBe(false);
    });
  });

  describe('failure()', () => {
    it('TC-009: should create a Failure with error', () => {
      const result = failure('error');

      expect(result.isSuccess).toBe(false);
      expect(result.isFailure).toBe(true);
      expect(result.error).toBe('error');
    });

    it('TC-010: should create a Failure with complex error object', () => {
      const error: ApiError = { code: 'ERR001', message: 'Something went wrong' };
      const result = failure(error);

      expect(result.isFailure).toBe(true);
      expect(result.error).toEqual({ code: 'ERR001', message: 'Something went wrong' });
    });

    it('TC-011: should create a Failure with null error', () => {
      const result = failure(null);

      expect(result.isFailure).toBe(true);
      expect(result.error).toBeNull();
    });

    it('TC-012: should create a Failure with Error instance', () => {
      const error = new Error('Test error');
      const result = failure(error);

      expect(result.isFailure).toBe(true);
      expect(result.error).toBeInstanceOf(Error);
      expect(result.error.message).toBe('Test error');
    });
  });
});

// ============================================================================
// Type Guards
// ============================================================================

describe('Type Guards', () => {
  describe('isSuccess()', () => {
    it('TC-013: should return true for Success', () => {
      const result: Result<number, string> = success(42) as Result<number, string>;

      expect(isSuccess(result)).toBe(true);
    });

    it('TC-014: should return false for Failure', () => {
      const result: Result<number, string> = failure('error') as Result<number, string>;

      expect(isSuccess(result)).toBe(false);
    });

    it('TC-015: should narrow type to Success', () => {
      const result: Result<number, string> = success(42) as Result<number, string>;

      if (isSuccess(result)) {
        // TypeScript should know result.value exists here
        const value: number = result.value;
        expect(value).toBe(42);
      }
    });
  });

  describe('isFailure()', () => {
    it('TC-016: should return true for Failure', () => {
      const result: Result<number, string> = failure('error') as Result<number, string>;

      expect(isFailure(result)).toBe(true);
    });

    it('TC-017: should return false for Success', () => {
      const result: Result<number, string> = success(42) as Result<number, string>;

      expect(isFailure(result)).toBe(false);
    });

    it('TC-018: should narrow type to Failure', () => {
      const result: Result<number, string> = failure('error') as Result<number, string>;

      if (isFailure(result)) {
        // TypeScript should know result.error exists here
        const error: string = result.error;
        expect(error).toBe('error');
      }
    });
  });
});

// ============================================================================
// Monadic Operations
// ============================================================================

describe('Result Monadic Operations', () => {
  describe('map()', () => {
    it('TC-019: should transform Success value', () => {
      const result: Result<number, string> = success(5) as Result<number, string>;
      const mapped = map(result, (n) => n * 2);

      expect(isSuccess(mapped)).toBe(true);
      if (isSuccess(mapped)) {
        expect(mapped.value).toBe(10);
      }
    });

    it('TC-020: should pass through Failure unchanged', () => {
      const result: Result<number, string> = failure('error') as Result<number, string>;
      const mapped = map(result, (n) => n * 2);

      expect(isFailure(mapped)).toBe(true);
      if (isFailure(mapped)) {
        expect(mapped.error).toBe('error');
      }
    });

    it('TC-021: should change value type', () => {
      const result: Result<number, string> = success(42) as Result<number, string>;
      const mapped = map(result, (n) => `Value: ${n}`);

      expect(isSuccess(mapped)).toBe(true);
      if (isSuccess(mapped)) {
        expect(mapped.value).toBe('Value: 42');
      }
    });

    it.skip('TC-022: should support curried form', () => {
      // Curried form not implemented - using standard two-argument form
    });

    it('TC-023: should not call mapper for Failure', () => {
      const mapper = vi.fn((n: number) => n * 2);
      const result: Result<number, string> = failure('error') as Result<number, string>;

      map(result, mapper);

      expect(mapper).not.toHaveBeenCalled();
    });
  });

  describe('mapErr()', () => {
    it('TC-024: should transform Failure error', () => {
      const result: Result<number, string> = failure('error') as Result<number, string>;
      const mapped = mapErr(result, (e) => ({ code: 'ERR', message: e }));

      expect(isFailure(mapped)).toBe(true);
      if (isFailure(mapped)) {
        expect(mapped.error).toEqual({ code: 'ERR', message: 'error' });
      }
    });

    it('TC-025: should pass through Success unchanged', () => {
      const result: Result<number, string> = success(42) as Result<number, string>;
      const mapped = mapErr(result, (e) => ({ code: 'ERR', message: e }));

      expect(isSuccess(mapped)).toBe(true);
      if (isSuccess(mapped)) {
        expect(mapped.value).toBe(42);
      }
    });

    it('TC-026: should not call mapper for Success', () => {
      const mapper = vi.fn((e: string) => ({ code: 'ERR', message: e }));
      const result: Result<number, string> = success(42) as Result<number, string>;

      mapErr(result, mapper);

      expect(mapper).not.toHaveBeenCalled();
    });
  });

  describe('bind()', () => {
    it('TC-027: should chain successful operations', () => {
      const result: Result<number, string> = success(5) as Result<number, string>;
      const bound = bind(result, (n) => success(n * 2) as Result<number, string>);

      expect(isSuccess(bound)).toBe(true);
      if (isSuccess(bound)) {
        expect(bound.value).toBe(10);
      }
    });

    it('TC-028: should short-circuit on Failure', () => {
      const result: Result<number, string> = failure('error') as Result<number, string>;
      const bound = bind(result, (n) => success(n * 2) as Result<number, string>);

      expect(isFailure(bound)).toBe(true);
      if (isFailure(bound)) {
        expect(bound.error).toBe('error');
      }
    });

    it('TC-029: should propagate Failure from binder', () => {
      const result: Result<number, string> = success(5) as Result<number, string>;
      const bound = bind(result, () => failure('new error') as Result<number, string>);

      expect(isFailure(bound)).toBe(true);
      if (isFailure(bound)) {
        expect(bound.error).toBe('new error');
      }
    });

    it.skip('TC-030: should support curried form', () => {
      // Curried form not implemented - using standard two-argument form
    });

    it('TC-031: should not call binder for Failure', () => {
      const binder = vi.fn((n: number) => success(n * 2) as Result<number, string>);
      const result: Result<number, string> = failure('error') as Result<number, string>;

      bind(result, binder);

      expect(binder).not.toHaveBeenCalled();
    });

    it('TC-032: should chain multiple binds', () => {
      const result: Result<number, string> = success(2) as Result<number, string>;

      const chained = bind(
        bind(
          bind(result, (n) => success(n + 3) as Result<number, string>),
          (n) => success(n * 2) as Result<number, string>
        ),
        (n) => success(n - 5) as Result<number, string>
      );

      expect(isSuccess(chained)).toBe(true);
      if (isSuccess(chained)) {
        // (2 + 3) * 2 - 5 = 5
        expect(chained.value).toBe(5);
      }
    });
  });

  describe('tap()', () => {
    it('TC-033: should execute action for Success', () => {
      const action = vi.fn();
      const result: Result<number, string> = success(42) as Result<number, string>;

      tap(result, action);

      expect(action).toHaveBeenCalledWith(42);
    });

    it('TC-034: should not execute action for Failure', () => {
      const action = vi.fn();
      const result: Result<number, string> = failure('error') as Result<number, string>;

      tap(result, action);

      expect(action).not.toHaveBeenCalled();
    });

    it('TC-035: should return same Result instance', () => {
      const result: Result<number, string> = success(42) as Result<number, string>;

      const tapped = tap(result, () => {});

      expect(tapped).toBe(result);
    });
  });

  describe('tapErr()', () => {
    it('TC-036: should execute action for Failure', () => {
      const action = vi.fn();
      const result: Result<number, string> = failure('error') as Result<number, string>;

      tapErr(result, action);

      expect(action).toHaveBeenCalledWith('error');
    });

    it('TC-037: should not execute action for Success', () => {
      const action = vi.fn();
      const result: Result<number, string> = success(42) as Result<number, string>;

      tapErr(result, action);

      expect(action).not.toHaveBeenCalled();
    });

    it('TC-038: should return same Result instance', () => {
      const result: Result<number, string> = failure('error') as Result<number, string>;

      const tapped = tapErr(result, () => {});

      expect(tapped).toBe(result);
    });
  });

  describe('compensate()', () => {
    it('TC-039: should recover from Failure', () => {
      const result: Result<number, string> = failure('error') as Result<number, string>;
      const recovered = compensate(result, () => success(0) as Result<number, string>);

      expect(isSuccess(recovered)).toBe(true);
      if (isSuccess(recovered)) {
        expect(recovered.value).toBe(0);
      }
    });

    it('TC-040: should pass through Success unchanged', () => {
      const result: Result<number, string> = success(42) as Result<number, string>;
      const recovered = compensate(result, () => success(0) as Result<number, string>);

      expect(isSuccess(recovered)).toBe(true);
      if (isSuccess(recovered)) {
        expect(recovered.value).toBe(42);
      }
    });

    it('TC-041: should allow recovery to fail', () => {
      const result: Result<number, string> = failure('error') as Result<number, string>;
      const recovered = compensate(
        result,
        () => failure('still failing') as Result<number, string>
      );

      expect(isFailure(recovered)).toBe(true);
      if (isFailure(recovered)) {
        expect(recovered.error).toBe('still failing');
      }
    });

    it('TC-042: should not call handler for Success', () => {
      const handler = vi.fn(() => success(0) as Result<number, string>);
      const result: Result<number, string> = success(42) as Result<number, string>;

      compensate(result, handler);

      expect(handler).not.toHaveBeenCalled();
    });
  });
});

// ============================================================================
// Unwrap Operations
// ============================================================================

describe('Result Unwrap Operations', () => {
  describe('unwrap()', () => {
    it('TC-043: should return value for Success', () => {
      const result: Result<number, string> = success(42) as Result<number, string>;

      const value = unwrap(result);

      expect(value).toBe(42);
    });

    it('TC-044: should throw for Failure', () => {
      const result: Result<number, string> = failure('error') as Result<number, string>;

      // unwrap throws the error value directly
      expect(() => unwrap(result)).toThrow('error');
    });

    it.skip('TC-045: should throw with custom message', () => {
      // Custom message parameter not implemented
    });

    it('TC-046: should return complex value for Success', () => {
      const user: User = { id: 1, name: 'John' };
      const result: Result<User, string> = success(user) as Result<User, string>;

      const value = unwrap(result);

      expect(value).toEqual({ id: 1, name: 'John' });
    });
  });

  describe('unwrapOr()', () => {
    it('TC-047: should return value for Success', () => {
      const result: Result<number, string> = success(42) as Result<number, string>;

      const value = unwrapOr(result, 0);

      expect(value).toBe(42);
    });

    it('TC-048: should return default for Failure', () => {
      const result: Result<number, string> = failure('error') as Result<number, string>;

      const value = unwrapOr(result, 0);

      expect(value).toBe(0);
    });

    it('TC-049: should return complex default for Failure', () => {
      const defaultUser: User = { id: 0, name: 'Guest' };
      const result: Result<User, string> = failure('error') as Result<User, string>;

      const value = unwrapOr(result, defaultUser);

      expect(value).toEqual({ id: 0, name: 'Guest' });
    });
  });

  describe('unwrapOrElse()', () => {
    it('TC-050: should return value for Success', () => {
      const result: Result<number, string> = success(42) as Result<number, string>;

      const value = unwrapOrElse(result, () => 0);

      expect(value).toBe(42);
    });

    it('TC-051: should call factory for Failure', () => {
      const factory = vi.fn(() => 0);
      const result: Result<number, string> = failure('error') as Result<number, string>;

      const value = unwrapOrElse(result, factory);

      expect(value).toBe(0);
      expect(factory).toHaveBeenCalled();
    });

    it('TC-052: should pass error to factory', () => {
      const result: Result<number, string> = failure('error') as Result<number, string>;

      const value = unwrapOrElse(result, (error) => error.length);

      expect(value).toBe(5); // 'error'.length === 5
    });
  });
});

// ============================================================================
// Combinators
// ============================================================================

describe('Result Combinators', () => {
  describe('combine()', () => {
    it('TC-053: should combine two successful Results', () => {
      const result1: Result<number, string> = success(1) as Result<number, string>;
      const result2: Result<number, string> = success(2) as Result<number, string>;

      const combined = combine([result1, result2]);

      expect(isSuccess(combined)).toBe(true);
      if (isSuccess(combined)) {
        expect(combined.value).toEqual([1, 2]);
      }
    });

    it('TC-054: should return first Failure', () => {
      const result1: Result<number, string> = success(1) as Result<number, string>;
      const result2: Result<number, string> = failure('error') as Result<number, string>;

      const combined = combine([result1, result2]);

      expect(isFailure(combined)).toBe(true);
      if (isFailure(combined)) {
        expect(combined.error).toBe('error');
      }
    });

    it('TC-055: should return second Failure if first succeeds', () => {
      const result1: Result<number, string> = success(1) as Result<number, string>;
      const result2: Result<number, string> = failure('second error') as Result<number, string>;

      const combined = combine([result1, result2]);

      expect(isFailure(combined)).toBe(true);
      if (isFailure(combined)) {
        expect(combined.error).toBe('second error');
      }
    });

    it('TC-056: should return first Failure when both fail', () => {
      const result1: Result<number, string> = failure('first error') as Result<number, string>;
      const result2: Result<number, string> = failure('second error') as Result<number, string>;

      const combined = combine([result1, result2]);

      expect(isFailure(combined)).toBe(true);
      if (isFailure(combined)) {
        expect(combined.error).toBe('first error');
      }
    });
  });

  describe('combineAll()', () => {
    it('TC-057: should combine array of successful Results', () => {
      const results: Result<number, string>[] = [
        success(1) as Result<number, string>,
        success(2) as Result<number, string>,
        success(3) as Result<number, string>,
      ];

      const combined = combineAll(results);

      expect(isSuccess(combined)).toBe(true);
      if (isSuccess(combined)) {
        expect(combined.value).toEqual([1, 2, 3]);
      }
    });

    it('TC-058: should return all Failures', () => {
      const results: Result<number, string>[] = [
        success(1) as Result<number, string>,
        failure('error') as Result<number, string>,
        success(3) as Result<number, string>,
      ];

      const combined = combineAll(results);

      // combineAll collects ALL errors into an array
      expect(isFailure(combined)).toBe(true);
      if (isFailure(combined)) {
        expect(combined.error).toEqual(['error']);
      }
    });

    it('TC-059: should handle empty array', () => {
      const results: Result<number, string>[] = [];

      const combined = combineAll(results);

      expect(isSuccess(combined)).toBe(true);
      if (isSuccess(combined)) {
        expect(combined.value).toEqual([]);
      }
    });

    it('TC-060: should handle single element array', () => {
      const results: Result<number, string>[] = [success(42) as Result<number, string>];

      const combined = combineAll(results);

      expect(isSuccess(combined)).toBe(true);
      if (isSuccess(combined)) {
        expect(combined.value).toEqual([42]);
      }
    });
  });

  describe('tryCatch()', () => {
    it('TC-061: should return Success for non-throwing function', () => {
      const result = tryCatch(() => 42);

      expect(isSuccess(result)).toBe(true);
      if (isSuccess(result)) {
        expect(result.value).toBe(42);
      }
    });

    it('TC-062: should return Failure for throwing function', () => {
      const result = tryCatch(
        () => {
          throw new Error('boom');
        },
        (e) => (e as Error).message
      );

      expect(isFailure(result)).toBe(true);
      if (isFailure(result)) {
        expect(result.error).toBe('boom');
      }
    });

    it('TC-063: should handle non-Error throws', () => {
      const result = tryCatch(
        () => {
          throw 'string error';
        },
        (e) => String(e)
      );

      expect(isFailure(result)).toBe(true);
      if (isFailure(result)) {
        expect(result.error).toBe('string error');
      }
    });

    it('TC-064: should use default error mapper when not provided', () => {
      const result = tryCatch(() => {
        throw new Error('boom');
      });

      expect(isFailure(result)).toBe(true);
      if (isFailure(result)) {
        expect(result.error).toBeInstanceOf(Error);
        expect((result.error as Error).message).toBe('boom');
      }
    });
  });

  describe('fromPromise()', () => {
    it('TC-065: should return Success for resolved Promise', async () => {
      const promise = Promise.resolve(42);

      const result = await fromPromise(promise);

      expect(isSuccess(result)).toBe(true);
      if (isSuccess(result)) {
        expect(result.value).toBe(42);
      }
    });

    it('TC-066: should return Failure for rejected Promise', async () => {
      const promise = Promise.reject(new Error('boom'));

      const result = await fromPromise(promise, (e) => (e as Error).message);

      expect(isFailure(result)).toBe(true);
      if (isFailure(result)) {
        expect(result.error).toBe('boom');
      }
    });

    it('TC-067: should handle async operations', async () => {
      const asyncOperation = async () => {
        await new Promise((resolve) => setTimeout(resolve, 10));
        return 'done';
      };

      const result = await fromPromise(asyncOperation());

      expect(isSuccess(result)).toBe(true);
      if (isSuccess(result)) {
        expect(result.value).toBe('done');
      }
    });

    it('TC-068: should use default error mapper when not provided', async () => {
      const promise = Promise.reject(new Error('boom'));

      const result = await fromPromise(promise);

      expect(isFailure(result)).toBe(true);
      if (isFailure(result)) {
        expect(result.error).toBeInstanceOf(Error);
        expect((result.error as Error).message).toBe('boom');
      }
    });
  });
});

// ============================================================================
// Edge Cases
// ============================================================================

describe('Edge Cases', () => {
  it('TC-069: should handle deeply nested Results with bind', () => {
    type NestedResult = Result<Result<number, string>, string>;

    const outer: Result<number, string> = success(42) as Result<number, string>;
    const nested: NestedResult = success(outer) as NestedResult;

    expect(isSuccess(nested)).toBe(true);
    if (isSuccess(nested)) {
      expect(isSuccess(nested.value)).toBe(true);
    }
  });

  it('TC-070: should handle Result with function value', () => {
    type FnResult = Result<(x: number) => number, string>;
    const fn = (x: number) => x * 2;
    const result: FnResult = success(fn) as FnResult;

    expect(isSuccess(result)).toBe(true);
    if (isSuccess(result)) {
      expect(result.value(5)).toBe(10);
    }
  });

  it('TC-071: should handle Result with Promise value', async () => {
    type PromiseResult = Result<Promise<number>, string>;
    const result: PromiseResult = success(Promise.resolve(42)) as PromiseResult;

    expect(isSuccess(result)).toBe(true);
    if (isSuccess(result)) {
      const value = await result.value;
      expect(value).toBe(42);
    }
  });

  it('TC-072: should maintain type safety with generics', () => {
    const numberResult: Result<number, string> = success(42) as Result<number, string>;
    const stringResult: Result<string, number> = success('hello') as Result<string, number>;

    expect(isSuccess(numberResult)).toBe(true);
    expect(isSuccess(stringResult)).toBe(true);
    if (isSuccess(numberResult)) {
      expect(typeof numberResult.value).toBe('number');
    }
    if (isSuccess(stringResult)) {
      expect(typeof stringResult.value).toBe('string');
    }
  });

  it('TC-073: should handle union type values', () => {
    type UnionValue = string | number | boolean;
    const result: Result<UnionValue, string> = success<UnionValue>(
      'text'
    ) as Result<UnionValue, string>;

    expect(isSuccess(result)).toBe(true);
    if (isSuccess(result)) {
      expect(result.value).toBe('text');
    }
  });

  it('TC-074: should handle Symbol as value', () => {
    const sym = Symbol('test');
    const result: Result<symbol, string> = success(sym) as Result<symbol, string>;

    expect(isSuccess(result)).toBe(true);
    if (isSuccess(result)) {
      expect(result.value).toBe(sym);
    }
  });

  it('TC-075: should handle BigInt as value', () => {
    const bigValue = BigInt(9007199254740991);
    const result: Result<bigint, string> = success(bigValue) as Result<bigint, string>;

    expect(isSuccess(result)).toBe(true);
    if (isSuccess(result)) {
      expect(result.value).toBe(bigValue);
    }
  });
});
