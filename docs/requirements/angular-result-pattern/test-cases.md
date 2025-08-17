# Angular Result Pattern — Test Cases

Traceability: This set of tests validates the functional requirements in `specifications.md`.

## Unit tests — utilities

- Result core
  - ok/err constructors produce correct shapes
  - isOk/isErr type guards narrow correctly
  - map/mapErr transform success or error branches only
  - unwrapOr returns fallback on error

- Error conversion
  - toApiError maps ProblemDetails payloads to `problem`
  - toApiError maps HTTP/network errors to `network` with status
  - toApiError maps unknown to `unknown`

- RxJS operator
  - toResult wraps success as ok(value)
  - toResult wraps thrown HttpErrorResponse as err(ApiError)

## Service tests

- create() success → Observable emits ok(dto)
- create() validation problem → err({ kind: 'problem', problem.errors has field keys })
- create() 409 conflict → err({ kind: 'problem' }) or network depending on server response
- get() not found → err(kind: 'problem') with status 404 if ProblemDetails provided

## Component tests

- Disabled submit when form invalid or pending
- On success result → success branch executed (e.g., navigate)
- On problem with validation errors → setErrors applied to mapped controls
- On network/unknown → friendly error surfaced to the user via toast/snackbar

## Non-functional

- No PII logged to console
- Utilities are tree-shakeable (no side effects)
- Signals-based state updates do not trigger excessive change detection
