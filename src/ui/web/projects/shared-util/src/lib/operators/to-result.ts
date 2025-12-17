/**
 * @fileoverview RxJS operator to convert Observable errors into Result types.
 *
 * Provides the `toResult()` operator that catches errors from Observable streams
 * (particularly HttpClient responses) and converts them into typed Result objects.
 *
 * @example
 * ```typescript
 * this.http.get<User>('/api/users/1').pipe(
 *   toResult()
 * ).subscribe(result => {
 *   if (isSuccess(result)) {
 *     console.log('User:', result.value);
 *   } else {
 *     console.log('Error:', getErrorMessage(result.error));
 *   }
 * });
 * ```
 */

import { catchError, map, Observable, of, OperatorFunction } from 'rxjs';
import { ApiError, toApiError } from '../types/problem-details';
import { failure, Result, success } from '../types/result';

/**
 * Creates an RxJS operator that transforms an Observable<T> into Observable<Result<T, ApiError>>.
 *
 * - Successful emissions are wrapped in `success(value)`
 * - Errors are converted via `toApiError()` and wrapped in `failure(error)`
 *
 * The operator handles:
 * - HttpErrorResponse with ProblemDetails body → ProblemApiError
 * - HttpErrorResponse without ProblemDetails → NetworkApiError
 * - Network connectivity errors (status 0) → NetworkApiError
 * - Generic Error objects → UnknownApiError
 * - Unknown error types → UnknownApiError
 *
 * @typeParam T - The type of successful values
 * @returns An OperatorFunction that transforms the stream
 *
 * @example
 * ```typescript
 * // Basic usage
 * this.http.get<User>('/api/users/1').pipe(
 *   toResult()
 * ).subscribe(result => {
 *   if (isSuccess(result)) {
 *     this.user.set(result.value);
 *   } else {
 *     this.errorMessage.set(getErrorMessage(result.error));
 *   }
 * });
 * ```
 *
 * @example
 * ```typescript
 * // With signal state management
 * export class UserService {
 *   private readonly http = inject(HttpClient);
 *   private readonly _user = signal<User | null>(null);
 *   private readonly _loading = signal(false);
 *   private readonly _error = signal<ApiError | null>(null);
 *
 *   loadUser(id: number): void {
 *     this._loading.set(true);
 *     this._error.set(null);
 *
 *     this.http.get<User>(`/api/users/${id}`).pipe(
 *       toResult()
 *     ).subscribe(result => {
 *       this._loading.set(false);
 *       if (isSuccess(result)) {
 *         this._user.set(result.value);
 *       } else {
 *         this._error.set(result.error);
 *       }
 *     });
 *   }
 * }
 * ```
 *
 * @example
 * ```typescript
 * // Chaining with map operations
 * this.http.get<User[]>('/api/users').pipe(
 *   toResult(),
 *   map(result =>
 *     isSuccess(result)
 *       ? success(result.value.length)
 *       : result
 *   )
 * ).subscribe(countResult => {
 *   // countResult is Result<number, ApiError>
 * });
 * ```
 */
export function toResult<T>(): OperatorFunction<T, Result<T, ApiError>> {
  return (source: Observable<T>): Observable<Result<T, ApiError>> =>
    source.pipe(
      map((value: T) => success(value) as Result<T, ApiError>),
      catchError((error: unknown) => of(failure(toApiError(error)) as Result<T, ApiError>)),
    );
}

/**
 * Creates an RxJS operator that transforms an Observable<T> into Observable<Result<T, E>>
 * using a custom error mapper.
 *
 * Use this when you need to transform errors into a custom error type instead of ApiError.
 *
 * @typeParam T - The type of successful values
 * @typeParam E - The custom error type
 * @param errorMapper - Function to transform caught errors into type E
 * @returns An OperatorFunction that transforms the stream
 *
 * @example
 * ```typescript
 * // Custom error type
 * type AppError = { code: string; message: string };
 *
 * const mapToAppError = (error: unknown): AppError => {
 *   const apiError = toApiError(error);
 *   return {
 *     code: isProblemError(apiError) ? apiError.problem.type ?? 'UNKNOWN' : 'NETWORK',
 *     message: getErrorMessage(apiError)
 *   };
 * };
 *
 * this.http.get<User>('/api/users/1').pipe(
 *   toResultWith(mapToAppError)
 * ).subscribe(result => {
 *   // result is Result<User, AppError>
 * });
 * ```
 */
export function toResultWith<T, E>(
  errorMapper: (error: unknown) => E,
): OperatorFunction<T, Result<T, E>> {
  return (source: Observable<T>): Observable<Result<T, E>> =>
    source.pipe(
      map((value: T) => success(value) as Result<T, E>),
      catchError((error: unknown) => of(failure(errorMapper(error)) as Result<T, E>)),
    );
}

/**
 * Creates an RxJS operator that unwraps a Result, emitting the value on success
 * or throwing the error on failure.
 *
 * Use this to convert a Result stream back into a standard Observable for
 * compatibility with code that expects traditional error handling.
 *
 * ⚠️ WARNING: This operator will cause the Observable to error if the Result
 * is a failure. Use with caution and ensure proper error handling downstream.
 *
 * @typeParam T - The type of successful values
 * @typeParam E - The error type
 * @returns An OperatorFunction that unwraps Result values
 *
 * @example
 * ```typescript
 * // Convert Result stream back to regular Observable
 * resultStream$.pipe(
 *   unwrapResult()
 * ).subscribe({
 *   next: value => console.log('Success:', value),
 *   error: err => console.log('Error:', err)
 * });
 * ```
 */
export function unwrapResult<T, E>(): OperatorFunction<Result<T, E>, T> {
  return (source: Observable<Result<T, E>>): Observable<T> =>
    new Observable<T>((subscriber) => {
      return source.subscribe({
        next: (result) => {
          if (result.isSuccess) {
            subscriber.next(result.value);
          } else {
            subscriber.error(result.error);
          }
        },
        error: (err) => subscriber.error(err),
        complete: () => subscriber.complete(),
      });
    });
}

/**
 * Creates an RxJS operator that filters a Result stream to only emit successful values.
 * Failed Results are silently filtered out.
 *
 * Use this when you only care about successful values and want to ignore failures.
 *
 * @typeParam T - The type of successful values
 * @typeParam E - The error type (not used in output)
 * @returns An OperatorFunction that filters to successful values only
 *
 * @example
 * ```typescript
 * // Only process successful results
 * resultStream$.pipe(
 *   filterSuccess()
 * ).subscribe(value => {
 *   // value is of type T, not Result<T, E>
 *   console.log('Success:', value);
 * });
 * ```
 */
export function filterSuccess<T, E>(): OperatorFunction<Result<T, E>, T> {
  return (source: Observable<Result<T, E>>): Observable<T> =>
    new Observable<T>((subscriber) => {
      return source.subscribe({
        next: (result) => {
          if (result.isSuccess) {
            subscriber.next(result.value);
          }
        },
        error: (err) => subscriber.error(err),
        complete: () => subscriber.complete(),
      });
    });
}

/**
 * Creates an RxJS operator that filters a Result stream to only emit error values.
 * Successful Results are silently filtered out.
 *
 * Use this when you only want to handle errors (e.g., for logging or analytics).
 *
 * @typeParam T - The type of successful values (not used in output)
 * @typeParam E - The error type
 * @returns An OperatorFunction that filters to errors only
 *
 * @example
 * ```typescript
 * // Log all errors to analytics
 * resultStream$.pipe(
 *   filterFailure()
 * ).subscribe(error => {
 *   // error is of type E, not Result<T, E>
 *   analyticsService.trackError(error);
 * });
 * ```
 */
export function filterFailure<T, E>(): OperatorFunction<Result<T, E>, E> {
  return (source: Observable<Result<T, E>>): Observable<E> =>
    new Observable<E>((subscriber) => {
      return source.subscribe({
        next: (result) => {
          if (!result.isSuccess) {
            subscriber.next(result.error);
          }
        },
        error: (err) => subscriber.error(err),
        complete: () => subscriber.complete(),
      });
    });
}
