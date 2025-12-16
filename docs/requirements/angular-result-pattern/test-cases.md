# Angular Result Pattern — Test Cases

Traceability: This set of tests validates the functional requirements in [specifications.md](./specifications.md) and the implementation plan in [implementation.md](./implementation.md).

## Test Organization

Tests are co-located with implementation files using the `.spec.ts` suffix and use Vitest for fast unit testing.

## Unit Tests — Result Type (`result.spec.ts`)

### Factory Functions

- **TC-001**: `success()` creates result with `isSuccess: true`, `isFailure: false`, and correct `value`
- **TC-002**: `failure()` creates result with `isSuccess: false`, `isFailure: true`, and correct `error`
- **TC-003**: `success()` with `null` value creates valid success result
- **TC-004**: `success()` with `undefined` value creates valid success result
- **TC-005**: `failure()` with `null` error creates valid failure result

### Type Guards

- **TC-010**: `isSuccess()` returns `true` for success result and narrows type to `Success<T>`
- **TC-011**: `isSuccess()` returns `false` for failure result
- **TC-012**: `isFailure()` returns `true` for failure result and narrows type to `Failure<E>`
- **TC-013**: `isFailure()` returns `false` for success result
- **TC-014**: Type narrowing allows access to `value` after `isSuccess()` guard
- **TC-015**: Type narrowing allows access to `error` after `isFailure()` guard

### Monadic Functions

#### map()

- **TC-020**: `map()` transforms success value with provided function
- **TC-021**: `map()` leaves failure unchanged
- **TC-022**: `map()` handles identity function correctly
- **TC-023**: `map()` chains multiple transformations correctly
- **TC-024**: `map()` handles exceptions thrown in mapping function (captures as failure)

#### mapErr()

- **TC-030**: `mapErr()` transforms failure error with provided function
- **TC-031**: `mapErr()` leaves success unchanged
- **TC-032**: `mapErr()` chains multiple error transformations correctly
- **TC-033**: `mapErr()` handles exceptions thrown in mapping function

#### bind()

- **TC-040**: `bind()` chains success to new success result
- **TC-041**: `bind()` chains success to new failure result
- **TC-042**: `bind()` short-circuits failure (doesn't call function)
- **TC-043**: `bind()` chains multiple operations correctly
- **TC-044**: `bind()` handles exceptions thrown in binding function

#### tap()

- **TC-050**: `tap()` executes side effect on success and returns original result
- **TC-051**: `tap()` skips side effect on failure
- **TC-052**: `tap()` handles exceptions in side effect function (doesn't affect result)
- **TC-053**: `tap()` called multiple times executes all side effects in order

#### tapErr()

- **TC-060**: `tapErr()` executes side effect on failure and returns original result
- **TC-061**: `tapErr()` skips side effect on success
- **TC-062**: `tapErr()` handles exceptions in side effect function
- **TC-063**: `tapErr()` called multiple times executes all side effects in order

#### compensate()

- **TC-070**: `compensate()` replaces failure with recovery success
- **TC-071**: `compensate()` replaces failure with different failure
- **TC-072**: `compensate()` skips recovery on success
- **TC-073**: `compensate()` handles exceptions in recovery function
- **TC-074**: `compensate()` chains multiple recovery attempts correctly

### Utility Functions

#### unwrap()

- **TC-080**: `unwrap()` returns value from success result
- **TC-081**: `unwrap()` throws error from failure result
- **TC-082**: `unwrap()` thrown error contains original error information
- **TC-083**: `unwrap()` handles `null` and `undefined` values correctly

#### unwrapOr()

- **TC-090**: `unwrapOr()` returns value from success result
- **TC-091**: `unwrapOr()` returns default value from failure result
- **TC-092**: `unwrapOr()` handles `null` default value correctly
- **TC-093**: `unwrapOr()` handles `undefined` default value correctly

#### unwrapOrElse()

- **TC-100**: `unwrapOrElse()` returns value from success result without calling function
- **TC-101**: `unwrapOrElse()` calls function and returns result for failure
- **TC-102**: `unwrapOrElse()` passes error to fallback function
- **TC-103**: `unwrapOrElse()` handles exceptions in fallback function

## Unit Tests — ProblemDetails & ApiError (`problem-details.spec.ts`)

### ProblemDetails Type

- **TC-200**: ProblemDetails accepts all RFC 7807 standard fields
- **TC-201**: ProblemDetails accepts ASP.NET Core extensions (`errors`, `traceId`)
- **TC-202**: ProblemDetails accepts custom extension properties

### ApiError Factory Functions

- **TC-210**: `problemError()` creates `problem` kind with correct structure
- **TC-211**: `networkError()` creates `network` kind with status and message
- **TC-212**: `unknownError()` creates `unknown` kind with message
- **TC-213**: Factory functions handle missing optional parameters correctly

### toApiError() Conversion

#### HttpErrorResponse with ProblemDetails

- **TC-220**: Converts 400 Bad Request with ProblemDetails to `problem` ApiError
- **TC-221**: Extracts validation errors from `errors` field
- **TC-222**: Preserves `traceId` from ProblemDetails
- **TC-223**: Handles ProblemDetails with minimal fields (only `type` and `title`)
- **TC-224**: Handles ProblemDetails with custom extension properties

#### HttpErrorResponse without ProblemDetails

- **TC-230**: Converts 404 Not Found to `network` ApiError with status 404
- **TC-231**: Converts 500 Internal Server Error to `network` ApiError
- **TC-232**: Handles plain text error responses
- **TC-233**: Handles JSON error responses that aren't ProblemDetails
- **TC-234**: Handles empty response body

#### Network Errors

- **TC-240**: Converts status 0 (network connectivity) to `network` ApiError
- **TC-241**: Preserves original error for debugging in `originalError`
- **TC-242**: Provides meaningful message for network errors

#### Unknown Errors

- **TC-250**: Converts generic Error to `unknown` ApiError
- **TC-251**: Converts string error to `unknown` ApiError
- **TC-252**: Converts `null` to `unknown` ApiError
- **TC-253**: Converts `undefined` to `unknown` ApiError
- **TC-254**: Handles malformed objects as `unknown` ApiError

### Helper Functions

#### hasValidationErrors()

- **TC-260**: Returns `true` for `problem` with populated `errors` field
- **TC-261**: Returns `false` for `problem` without `errors` field
- **TC-262**: Returns `false` for `problem` with empty `errors` object
- **TC-263**: Returns `false` for `network` and `unknown` ApiErrors

#### getErrorMessage()

- **TC-270**: Extracts `detail` from ProblemDetails when present
- **TC-271**: Falls back to `title` when `detail` is missing
- **TC-272**: Falls back to `type` when both `detail` and `title` are missing
- **TC-273**: Uses provided default message when all fields missing
- **TC-274**: Extracts `message` from `network` ApiError
- **TC-275**: Extracts `message` from `unknown` ApiError
- **TC-276**: Handles multi-line error details correctly

## Unit Tests — RxJS Operator (`to-result.spec.ts`)

### Success Path

- **TC-300**: Wraps successful emission in `success()` Result
- **TC-301**: Wraps multiple emissions in separate `success()` Results
- **TC-302**: Handles `null` emission as valid success
- **TC-303**: Handles `undefined` emission as valid success
- **TC-304**: Preserves value types correctly (strings, numbers, objects, arrays)

### Error Path

- **TC-310**: Catches HttpErrorResponse and wraps in `failure()` with `problem` ApiError
- **TC-311**: Catches HttpErrorResponse and wraps in `failure()` with `network` ApiError
- **TC-312**: Catches generic Error and wraps in `failure()` with `unknown` ApiError
- **TC-313**: Never re-throws errors (Observable completes normally)
- **TC-314**: Converts error immediately (doesn't delay or buffer)

### Observable Behavior

- **TC-320**: Completes after source Observable completes
- **TC-321**: Unsubscribes from source when result Observable unsubscribed
- **TC-322**: Handles cold Observables correctly
- **TC-323**: Handles hot Observables correctly
- **TC-324**: Works with `shareReplay()` and other operators

### Operator Composition

- **TC-330**: Composes with other RxJS operators (`map`, `filter`, etc.)
- **TC-331**: Can be used multiple times in pipeline
- **TC-332**: Integrates with `retry()` operator
- **TC-333**: Integrates with `timeout()` operator

## Integration Tests — Service Layer

### HTTP Success Scenarios

- **TC-400**: `createBudget()` with valid data returns `Observable<Result<BudgetDto, ApiError>>` with success
- **TC-401**: `getBudget()` with existing ID returns success result
- **TC-402**: `updateBudget()` with valid data returns success result
- **TC-403**: `deleteBudget()` with existing ID returns success result
- **TC-404**: Service methods use typed DTOs correctly

### HTTP Validation Error Scenarios

- **TC-410**: `createBudget()` with invalid data returns failure with `problem` kind
- **TC-411**: Validation errors in `problem.errors` use correct field names (PascalCase from backend)
- **TC-412**: Multiple validation errors on same field combined in array
- **TC-413**: Multiple validation errors across different fields all present

### HTTP Client Error Scenarios

- **TC-420**: `getBudget()` with non-existent ID returns failure with 404 status
- **TC-421**: `updateBudget()` with stale data returns failure with 409 Conflict
- **TC-422**: Unauthorized request (401) returns failure with `network` kind
- **TC-423**: Forbidden request (403) returns failure with appropriate error

### HTTP Server Error Scenarios

- **TC-430**: 500 Internal Server Error returns failure with `network` kind
- **TC-431**: 503 Service Unavailable returns failure with appropriate message
- **TC-432**: Timeout error returns failure with timeout indication

### Network Connectivity Scenarios

- **TC-440**: Network disconnection (status 0) returns failure with `network` kind
- **TC-441**: DNS resolution failure returns appropriate error
- **TC-442**: CORS error returns appropriate error

## Integration Tests — Component with Forms

### Form State Management

- **TC-500**: Submit button disabled when form invalid
- **TC-501**: Submit button disabled when `pending` signal is `true`
- **TC-502**: Submit button enabled when form valid and not pending
- **TC-503**: Form fields disabled during pending operation

### Success Flow

- **TC-510**: Successful creation sets `result` signal to success Result
- **TC-511**: Successful creation clears `pending` signal
- **TC-512**: Successful creation triggers navigation (if applicable)
- **TC-513**: Successful creation shows success notification (if applicable)
- **TC-514**: Successful creation resets form (if applicable)

### Validation Error Flow

- **TC-520**: Validation errors mapped to correct form controls using `setErrors()`
- **TC-521**: Form controls marked as touched after validation error
- **TC-522**: Inline error messages displayed for each invalid field
- **TC-523**: Error key `api` used to distinguish server errors from client validation
- **TC-524**: Field name mapping handles PascalCase to camelCase conversion
- **TC-525**: Unmapped validation errors handled gracefully (logged or displayed as general error)

### Non-Validation Error Flow

- **TC-530**: Network error triggers toast/snackbar notification
- **TC-531**: Unknown error triggers toast/snackbar notification
- **TC-532**: Error message extracted using `getErrorMessage()` helper
- **TC-533**: Toast/snackbar is accessible (uses `role="alert"`)
- **TC-534**: Toast/snackbar is dismissible

### Signal Reactivity

- **TC-540**: `error` computed signal updates when `result` changes
- **TC-541**: `errorMessage` computed signal derives from `error` signal
- **TC-542**: Template reactively shows/hides error messages
- **TC-543**: No excessive change detection cycles triggered
- **TC-544**: Signals update synchronously within same microtask

## Integration Tests — Multiple Components

- **TC-600**: Multiple components can use Result pattern simultaneously
- **TC-601**: Results are independent between components (no shared state)
- **TC-602**: Concurrent API calls from different components handled correctly

## Accessibility Tests

### Inline Validation Errors

- **TC-700**: Form controls have `aria-describedby` linking to error messages
- **TC-701**: Invalid form controls have `aria-invalid="true"`
- **TC-702**: Error messages have sufficient color contrast (WCAG AA)
- **TC-703**: Error messages announced to screen readers

### Toast/Snackbar Notifications

- **TC-710**: Toast has `role="alert"` for screen reader announcement
- **TC-711**: Toast is keyboard dismissible
- **TC-712**: Toast doesn't trap focus
- **TC-713**: Toast content readable by screen readers

## Performance Tests

- **TC-800**: Result utilities have no runtime overhead (compile-time only)
- **TC-801**: `toResult()` operator adds minimal memory overhead
- **TC-802**: Signal updates don't trigger unnecessary re-renders
- **TC-803**: Bundle size impact < 2KB gzipped

## Security Tests

- **TC-900**: PII not logged to console in error handling
- **TC-901**: Error messages rendered as text content (not HTML)
- **TC-902**: Stack traces not exposed to users in production
- **TC-903**: Sensitive data in errors handled securely

## Non-Functional Tests

### Tree-Shaking

- **TC-1000**: Unused Result functions eliminated in production build
- **TC-1001**: Unused ApiError factories eliminated in production build
- **TC-1002**: Utilities have no side effects (pure functions)

### TypeScript Strictness

- **TC-1010**: All code passes `strict: true` TypeScript checks
- **TC-1011**: No `any` types used (except where necessary for error handling)
- **TC-1012**: Discriminated union narrowing works correctly
- **TC-1013**: Type guards provide proper type narrowing

### Documentation

- **TC-1020**: All public APIs have JSDoc comments
- **TC-1021**: Usage examples compile without errors
- **TC-1022**: IDE autocomplete works for all exports

## Test Execution Summary

### Unit Tests Coverage Target

- **Line Coverage**: > 95%
- **Branch Coverage**: > 90%
- **Function Coverage**: > 95%

### Integration Tests Coverage Target

- **Critical Paths**: 100% (success, validation error, network error)
- **Edge Cases**: > 80%

### Manual Testing

- [ ] Create budget with valid data → success notification
- [ ] Create budget with invalid data → inline validation errors
- [ ] Create budget with network disconnected → toast error
- [ ] Multiple concurrent creates handled correctly
- [ ] Accessibility verified with screen reader
- [ ] Keyboard navigation works correctly

## Acceptance Criteria

- [ ] All unit tests passing (TC-001 through TC-333)
- [ ] All integration tests passing (TC-400 through TC-602)
- [ ] All accessibility tests passing (TC-700 through TC-713)
- [ ] Performance tests within acceptable thresholds (TC-800 through TC-803)
- [ ] Security tests passing (TC-900 through TC-903)
- [ ] Code coverage targets met
- [ ] No TypeScript errors or warnings
- [ ] No linter errors or warnings
- [ ] Manual testing checklist completed
- [ ] Peer review approved
