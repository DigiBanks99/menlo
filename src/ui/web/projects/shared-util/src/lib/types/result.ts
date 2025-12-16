/**
 * @fileoverview Result type implementation for Angular applications.
 *
 * A lightweight, framework-native Result pattern using TypeScript discriminated unions.
 * Provides type-safe error handling without throwing exceptions across component/service boundaries.
 *
 * @example
 * ```typescript
 * const result = success(42);
 * if (isSuccess(result)) {
 *   console.log(result.value); // 42
 * }
 * ```
 */

// ============================================================================
// Types
// ============================================================================

/**
 * Represents a successful result containing a value.
 * @template T - The type of the success value
 */
export interface Success<T> {
  readonly isSuccess: true;
  readonly isFailure: false;
  readonly value: T;
}

/**
 * Represents a failed result containing an error.
 * @template E - The type of the error
 */
export interface Failure<E> {
  readonly isSuccess: false;
  readonly isFailure: true;
  readonly error: E;
}

/**
 * A discriminated union representing either success or failure.
 * Use type guards `isSuccess()` or `isFailure()` to narrow the type.
 *
 * @template T - The type of the success value
 * @template E - The type of the error (defaults to Error)
 *
 * @example
 * ```typescript
 * function divide(a: number, b: number): Result<number, string> {
 *   if (b === 0) {
 *     return failure('Division by zero');
 *   }
 *   return success(a / b);
 * }
 * ```
 */
export type Result<T, E = Error> = Success<T> | Failure<E>;

// ============================================================================
// Factory Functions
// ============================================================================

/**
 * Creates a successful Result containing the given value.
 *
 * @template T - The type of the success value
 * @param value - The value to wrap in a success Result
 * @returns A Success Result containing the value
 *
 * @example
 * ```typescript
 * const result = success(42);
 * // result.isSuccess === true
 * // result.value === 42
 * ```
 */
export function success<T>(value: T): Success<T> {
  return {
    isSuccess: true,
    isFailure: false,
    value,
  };
}

/**
 * Creates a failed Result containing the given error.
 *
 * @template E - The type of the error
 * @param error - The error to wrap in a failure Result
 * @returns A Failure Result containing the error
 *
 * @example
 * ```typescript
 * const result = failure(new Error('Something went wrong'));
 * // result.isFailure === true
 * // result.error.message === 'Something went wrong'
 * ```
 */
export function failure<E>(error: E): Failure<E> {
  return {
    isSuccess: false,
    isFailure: true,
    error,
  };
}

// ============================================================================
// Type Guards
// ============================================================================

/**
 * Type guard that checks if a Result is a Success.
 * Narrows the type to Success<T> when true.
 *
 * @template T - The type of the success value
 * @template E - The type of the error
 * @param result - The Result to check
 * @returns True if the result is a Success, false otherwise
 *
 * @example
 * ```typescript
 * const result = getUser(id);
 * if (isSuccess(result)) {
 *   // TypeScript knows result.value exists here
 *   console.log(result.value.name);
 * }
 * ```
 */
export function isSuccess<T, E>(result: Result<T, E>): result is Success<T> {
  return result.isSuccess;
}

/**
 * Type guard that checks if a Result is a Failure.
 * Narrows the type to Failure<E> when true.
 *
 * @template T - The type of the success value
 * @template E - The type of the error
 * @param result - The Result to check
 * @returns True if the result is a Failure, false otherwise
 *
 * @example
 * ```typescript
 * const result = getUser(id);
 * if (isFailure(result)) {
 *   // TypeScript knows result.error exists here
 *   console.error(result.error);
 * }
 * ```
 */
export function isFailure<T, E>(result: Result<T, E>): result is Failure<E> {
  return result.isFailure;
}

// ============================================================================
// Monadic Functions
// ============================================================================

/**
 * Transforms the success value using the provided function.
 * If the Result is a Failure, returns it unchanged.
 *
 * @template T - The type of the original success value
 * @template U - The type of the transformed success value
 * @template E - The type of the error
 * @param result - The Result to transform
 * @param fn - The transformation function
 * @returns A new Result with the transformed value or the original error
 *
 * @example
 * ```typescript
 * const result = success(5);
 * const doubled = map(result, x => x * 2);
 * // doubled.value === 10
 * ```
 */
export function map<T, U, E>(
  result: Result<T, E>,
  fn: (value: T) => U
): Result<U, E> {
  if (isSuccess(result)) {
    try {
      return success(fn(result.value));
    } catch (error) {
      return failure(error as E);
    }
  }
  return result;
}

/**
 * Transforms the error using the provided function.
 * If the Result is a Success, returns it unchanged.
 *
 * @template T - The type of the success value
 * @template E - The type of the original error
 * @template F - The type of the transformed error
 * @param result - The Result to transform
 * @param fn - The error transformation function
 * @returns A new Result with the transformed error or the original value
 *
 * @example
 * ```typescript
 * const result = failure('error');
 * const mapped = mapErr(result, e => new Error(e));
 * // mapped.error instanceof Error
 * ```
 */
export function mapErr<T, E, F>(
  result: Result<T, E>,
  fn: (error: E) => F
): Result<T, F> {
  if (isFailure(result)) {
    try {
      return failure(fn(result.error));
    } catch (error) {
      return failure(error as F);
    }
  }
  return result;
}

/**
 * Chains Result-returning operations (flatMap/andThen).
 * If the Result is a Success, applies the function and returns its Result.
 * If the Result is a Failure, returns it unchanged.
 *
 * @template T - The type of the original success value
 * @template U - The type of the new success value
 * @template E - The type of the error
 * @param result - The Result to chain
 * @param fn - The function returning a new Result
 * @returns The Result from the function or the original error
 *
 * @example
 * ```typescript
 * const parseNumber = (s: string): Result<number, string> =>
 *   isNaN(Number(s)) ? failure('Not a number') : success(Number(s));
 *
 * const result = success('42');
 * const parsed = bind(result, parseNumber);
 * // parsed.value === 42
 * ```
 */
export function bind<T, U, E>(
  result: Result<T, E>,
  fn: (value: T) => Result<U, E>
): Result<U, E> {
  if (isSuccess(result)) {
    try {
      return fn(result.value);
    } catch (error) {
      return failure(error as E);
    }
  }
  return result;
}

/**
 * Executes a side effect on success without modifying the Result.
 * Useful for logging, analytics, or other side effects.
 *
 * @template T - The type of the success value
 * @template E - The type of the error
 * @param result - The Result to tap
 * @param fn - The side effect function
 * @returns The original Result unchanged
 *
 * @example
 * ```typescript
 * const result = success(42);
 * const tapped = tap(result, value => console.log('Value:', value));
 * // Logs: "Value: 42"
 * // tapped === result
 * ```
 */
export function tap<T, E>(
  result: Result<T, E>,
  fn: (value: T) => void
): Result<T, E> {
  if (isSuccess(result)) {
    try {
      fn(result.value);
    } catch {
      // Side effects should not affect the result
    }
  }
  return result;
}

/**
 * Executes a side effect on failure without modifying the Result.
 * Useful for logging errors or analytics.
 *
 * @template T - The type of the success value
 * @template E - The type of the error
 * @param result - The Result to tap
 * @param fn - The side effect function
 * @returns The original Result unchanged
 *
 * @example
 * ```typescript
 * const result = failure(new Error('Oops'));
 * const tapped = tapErr(result, error => console.error('Error:', error));
 * // Logs: "Error: Error: Oops"
 * // tapped === result
 * ```
 */
export function tapErr<T, E>(
  result: Result<T, E>,
  fn: (error: E) => void
): Result<T, E> {
  if (isFailure(result)) {
    try {
      fn(result.error);
    } catch {
      // Side effects should not affect the result
    }
  }
  return result;
}

/**
 * Attempts to recover from a failure by providing an alternative Result.
 * If the Result is a Success, returns it unchanged.
 *
 * @template T - The type of the success value
 * @template E - The type of the error
 * @param result - The Result to potentially recover
 * @param fn - The recovery function
 * @returns The original success or the recovery Result
 *
 * @example
 * ```typescript
 * const result = failure('File not found');
 * const recovered = compensate(result, error => {
 *   console.log('Recovering from:', error);
 *   return success('default content');
 * });
 * // recovered.value === 'default content'
 * ```
 */
export function compensate<T, E>(
  result: Result<T, E>,
  fn: (error: E) => Result<T, E>
): Result<T, E> {
  if (isFailure(result)) {
    try {
      return fn(result.error);
    } catch (error) {
      return failure(error as E);
    }
  }
  return result;
}

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Extracts the value from a Success Result or throws the error from a Failure.
 *
 * ⚠️ **Warning**: Use sparingly. Prefer `unwrapOr` or `unwrapOrElse` for safer extraction.
 * Only use when you are certain the Result is a Success (e.g., after a type guard).
 *
 * @template T - The type of the success value
 * @template E - The type of the error
 * @param result - The Result to unwrap
 * @returns The success value
 * @throws The error if the Result is a Failure
 *
 * @example
 * ```typescript
 * const result = success(42);
 * const value = unwrap(result); // 42
 *
 * const errorResult = failure(new Error('Oops'));
 * const value2 = unwrap(errorResult); // throws Error: Oops
 * ```
 */
export function unwrap<T, E>(result: Result<T, E>): T {
  if (isSuccess(result)) {
    return result.value;
  }
  throw result.error;
}

/**
 * Extracts the value from a Success Result or returns the provided default value.
 *
 * @template T - The type of the success value
 * @template E - The type of the error
 * @param result - The Result to unwrap
 * @param defaultValue - The value to return if the Result is a Failure
 * @returns The success value or the default value
 *
 * @example
 * ```typescript
 * const result = failure('error');
 * const value = unwrapOr(result, 0); // 0
 *
 * const successResult = success(42);
 * const value2 = unwrapOr(successResult, 0); // 42
 * ```
 */
export function unwrapOr<T, E>(result: Result<T, E>, defaultValue: T): T {
  if (isSuccess(result)) {
    return result.value;
  }
  return defaultValue;
}

/**
 * Extracts the value from a Success Result or computes a default using the error.
 *
 * @template T - The type of the success value
 * @template E - The type of the error
 * @param result - The Result to unwrap
 * @param fn - The function to compute a default value from the error
 * @returns The success value or the computed default
 *
 * @example
 * ```typescript
 * const result = failure('missing');
 * const value = unwrapOrElse(result, error => `Default for: ${error}`);
 * // value === 'Default for: missing'
 * ```
 */
export function unwrapOrElse<T, E>(
  result: Result<T, E>,
  fn: (error: E) => T
): T {
  if (isSuccess(result)) {
    return result.value;
  }
  return fn(result.error);
}

// ============================================================================
// Combinator Functions
// ============================================================================

/**
 * Combines multiple Results into a single Result containing an array of values.
 * If any Result is a Failure, returns the first Failure encountered.
 *
 * @template T - The type of the success values
 * @template E - The type of the error
 * @param results - The array of Results to combine
 * @returns A Result containing an array of all success values or the first error
 *
 * @example
 * ```typescript
 * const results = [success(1), success(2), success(3)];
 * const combined = combine(results);
 * // combined.value === [1, 2, 3]
 *
 * const withError = [success(1), failure('error'), success(3)];
 * const combinedError = combine(withError);
 * // combinedError.error === 'error'
 * ```
 */
export function combine<T, E>(results: Result<T, E>[]): Result<T[], E> {
  const values: T[] = [];

  for (const result of results) {
    if (isFailure(result)) {
      return result;
    }
    values.push(result.value);
  }

  return success(values);
}

/**
 * Combines multiple Results, collecting all errors instead of short-circuiting.
 *
 * @template T - The type of the success values
 * @template E - The type of the errors
 * @param results - The array of Results to combine
 * @returns A Result containing all success values or all errors
 *
 * @example
 * ```typescript
 * const results = [failure('e1'), success(2), failure('e2')];
 * const combined = combineAll(results);
 * // combined.error === ['e1', 'e2']
 * ```
 */
export function combineAll<T, E>(results: Result<T, E>[]): Result<T[], E[]> {
  const values: T[] = [];
  const errors: E[] = [];

  for (const result of results) {
    if (isFailure(result)) {
      errors.push(result.error);
    } else {
      values.push(result.value);
    }
  }

  if (errors.length > 0) {
    return failure(errors);
  }

  return success(values);
}

/**
 * Creates a Result from a function that might throw.
 *
 * @template T - The type of the success value
 * @template E - The type of the error (defaults to unknown)
 * @param fn - The function to execute
 * @param errorMapper - Optional function to map caught errors to the error type
 * @returns A Success with the return value or a Failure with the thrown error
 *
 * @example
 * ```typescript
 * const result = tryCatch(() => JSON.parse('{"valid": true}'));
 * // result.value === { valid: true }
 *
 * const errorResult = tryCatch(() => JSON.parse('invalid'));
 * // errorResult.error instanceof SyntaxError
 *
 * // With error mapper
 * const mapped = tryCatch(
 *   () => { throw new Error('boom'); },
 *   (e) => (e as Error).message
 * );
 * // mapped.error === 'boom'
 * ```
 */
export function tryCatch<T, E = unknown>(
  fn: () => T,
  errorMapper?: (error: unknown) => E
): Result<T, E> {
  try {
    return success(fn());
  } catch (error) {
    return failure(errorMapper ? errorMapper(error) : (error as E));
  }
}

/**
 * Creates a Result from a Promise.
 *
 * @template T - The type of the success value
 * @template E - The type of the error (defaults to unknown)
 * @param promise - The Promise to convert
 * @param errorMapper - Optional function to map rejected errors to the error type
 * @returns A Promise that resolves to a Result
 *
 * @example
 * ```typescript
 * const result = await fromPromise(fetch('/api/data').then(r => r.json()));
 * if (isSuccess(result)) {
 *   console.log(result.value);
 * }
 *
 * // With error mapper
 * const mapped = await fromPromise(
 *   Promise.reject(new Error('boom')),
 *   (e) => (e as Error).message
 * );
 * // mapped.error === 'boom'
 * ```
 */
export async function fromPromise<T, E = unknown>(
  promise: Promise<T>,
  errorMapper?: (error: unknown) => E
): Promise<Result<T, E>> {
  try {
    const value = await promise;
    return success(value);
  } catch (error) {
    return failure(errorMapper ? errorMapper(error) : (error as E));
  }
}
