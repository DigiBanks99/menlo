/*
 * Public API Surface of shared-util
 */

export * from './lib/shared-util';

// Result Pattern Types and Functions
export {
    Failure,
    Result,
    // Types
    Success, bind,
    // Combinators
    combine,
    combineAll, compensate, failure, fromPromise, isFailure,
    // Type guards
    isSuccess,
    // Monadic functions
    map,
    mapErr,
    // Factory functions
    success, tap,
    tapErr, tryCatch,
    // Unwrap functions
    unwrap,
    unwrapOr,
    unwrapOrElse
} from './lib/types/result';

// ProblemDetails and ApiError Types
export {
    ApiError, NetworkApiError, ProblemApiError,
    // Types
    ProblemDetails, UnknownApiError, getErrorMessage,
    getErrorStatus,
    getValidationErrors,
    // Helper functions
    hasValidationErrors, isNetworkError,
    // Type guards
    isProblemError, isUnknownError, mapValidationErrorsToForm, networkError,
    // Factory functions
    problemError,
    // Conversion functions
    toApiError, unknownError
} from './lib/types/problem-details';

// RxJS Operators
export {
    filterFailure, filterSuccess, toResult,
    toResultWith,
    unwrapResult
} from './lib/operators/to-result';

