/**
 * @fileoverview Unit tests for ProblemDetails and ApiError module.
 *
 * Tests cover:
 * - Factory functions (problemError, networkError, unknownError)
 * - Type guards (isProblemError, isNetworkError, isUnknownError)
 * - Conversion function (toApiError)
 * - Helper functions (hasValidationErrors, getErrorMessage, getErrorStatus, getValidationErrors)
 */

import { describe, expect, it, vi } from 'vitest';
import {
  ApiError,
  NetworkApiError,
  ProblemDetails,
  getErrorMessage,
  getErrorStatus,
  getValidationErrors,
  hasValidationErrors,
  isNetworkError,
  isProblemError,
  isUnknownError,
  mapValidationErrorsToForm,
  networkError,
  problemError,
  toApiError,
  unknownError
} from './problem-details';

// ============================================================================
// Test Data
// ============================================================================

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

  constructor(init: {
    status?: number;
    statusText?: string;
    error?: unknown;
    url?: string;
  }) {
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
  type: 'https://example.com/probs/validation-error',
  title: 'Validation Error',
  status: 400,
  detail: 'One or more validation errors occurred.',
  instance: '/api/users',
  traceId: 'abc123',
  ...overrides,
});

const createHttpErrorResponse = (
  status: number,
  body: unknown,
  statusText = 'Error'
): MockHttpErrorResponse =>
  new MockHttpErrorResponse({
    status,
    statusText,
    error: body,
    url: 'https://api.example.com/test',
  });

// ============================================================================
// Factory Functions
// ============================================================================

describe('ApiError Factory Functions', () => {
  describe('problemError()', () => {
    it('TC-101: should create ProblemApiError with problem details', () => {
      const problem = createProblemDetails();
      const error = problemError(problem);

      expect(error.kind).toBe('problem');
      expect(error.problem).toBe(problem);
      expect(error.status).toBe(400);
    });

    it('TC-102: should use provided status over problem status', () => {
      const problem = createProblemDetails({ status: 400 });
      const error = problemError(problem, 422);

      expect(error.status).toBe(422);
    });

    it('TC-103: should default status from problem when not provided', () => {
      const problem = createProblemDetails({ status: 500 });
      const error = problemError(problem);

      expect(error.status).toBe(500);
    });

    it('TC-104: should handle problem without status', () => {
      const problem: ProblemDetails = { title: 'Error' };
      const error = problemError(problem);

      expect(error.status).toBeUndefined();
    });
  });

  describe('networkError()', () => {
    it('TC-105: should create NetworkApiError with all fields', () => {
      const original = new Error('Connection refused');
      const error = networkError(0, 'Network error occurred', original);

      expect(error.kind).toBe('network');
      expect(error.status).toBe(0);
      expect(error.message).toBe('Network error occurred');
      expect(error.originalError).toBe(original);
    });

    it('TC-106: should use default message when not provided', () => {
      const error = networkError();

      expect(error.message).toBe('Network error');
    });

    it('TC-107: should handle undefined original error', () => {
      const error = networkError(500, 'Server error');

      expect(error.originalError).toBeUndefined();
    });
  });

  describe('unknownError()', () => {
    it('TC-108: should create UnknownApiError with all fields', () => {
      const original = { foo: 'bar' };
      const error = unknownError('Unknown error occurred', original);

      expect(error.kind).toBe('unknown');
      expect(error.message).toBe('Unknown error occurred');
      expect(error.originalError).toBe(original);
    });

    it('TC-109: should use default message when not provided', () => {
      const error = unknownError();

      expect(error.message).toBe('An unexpected error occurred');
    });
  });
});

// ============================================================================
// Type Guards
// ============================================================================

describe('ApiError Type Guards', () => {
  describe('isProblemError()', () => {
    it('TC-110: should return true for ProblemApiError', () => {
      const error = problemError(createProblemDetails());

      expect(isProblemError(error)).toBe(true);
    });

    it('TC-111: should return false for NetworkApiError', () => {
      const error = networkError(500, 'Error');

      expect(isProblemError(error)).toBe(false);
    });

    it('TC-112: should return false for UnknownApiError', () => {
      const error = unknownError('Error');

      expect(isProblemError(error)).toBe(false);
    });

    it('TC-113: should narrow type correctly', () => {
      const error: ApiError = problemError(createProblemDetails());

      if (isProblemError(error)) {
        // TypeScript should know error.problem exists
        expect(error.problem).toBeDefined();
      }
    });
  });

  describe('isNetworkError()', () => {
    it('TC-114: should return true for NetworkApiError', () => {
      const error = networkError(500, 'Error');

      expect(isNetworkError(error)).toBe(true);
    });

    it('TC-115: should return false for ProblemApiError', () => {
      const error = problemError(createProblemDetails());

      expect(isNetworkError(error)).toBe(false);
    });

    it('TC-116: should return false for UnknownApiError', () => {
      const error = unknownError('Error');

      expect(isNetworkError(error)).toBe(false);
    });
  });

  describe('isUnknownError()', () => {
    it('TC-117: should return true for UnknownApiError', () => {
      const error = unknownError('Error');

      expect(isUnknownError(error)).toBe(true);
    });

    it('TC-118: should return false for ProblemApiError', () => {
      const error = problemError(createProblemDetails());

      expect(isUnknownError(error)).toBe(false);
    });

    it('TC-119: should return false for NetworkApiError', () => {
      const error = networkError(500, 'Error');

      expect(isUnknownError(error)).toBe(false);
    });
  });
});

// ============================================================================
// toApiError Conversion
// ============================================================================

describe('toApiError()', () => {
  describe('HttpErrorResponse handling', () => {
    it('TC-120: should convert HttpErrorResponse with ProblemDetails to ProblemApiError', () => {
      const problem = createProblemDetails();
      const httpError = createHttpErrorResponse(400, problem);

      const result = toApiError(httpError);

      expect(isProblemError(result)).toBe(true);
      if (isProblemError(result)) {
        expect(result.problem.title).toBe('Validation Error');
        expect(result.status).toBe(400);
      }
    });

    it('TC-121: should convert status 0 to NetworkApiError', () => {
      const httpError = createHttpErrorResponse(0, null, 'Unknown Error');

      const result = toApiError(httpError);

      expect(isNetworkError(result)).toBe(true);
      if (isNetworkError(result)) {
        expect(result.status).toBe(0);
        expect(result.message).toContain('Unable to connect');
      }
    });

    it('TC-122: should convert HttpErrorResponse without ProblemDetails to NetworkApiError', () => {
      const httpError = createHttpErrorResponse(500, 'Internal Server Error');

      const result = toApiError(httpError);

      expect(isNetworkError(result)).toBe(true);
      if (isNetworkError(result)) {
        expect(result.status).toBe(500);
      }
    });

    it('TC-123: should parse stringified ProblemDetails in error body', () => {
      const problem = createProblemDetails();
      const httpError = createHttpErrorResponse(400, JSON.stringify(problem));

      const result = toApiError(httpError);

      expect(isProblemError(result)).toBe(true);
      if (isProblemError(result)) {
        expect(result.problem.title).toBe('Validation Error');
      }
    });

    it('TC-124: should handle ProblemDetails with validation errors', () => {
      const problem = createProblemDetails({
        errors: {
          Name: ['Name is required'],
          Email: ['Invalid email format'],
        },
      });
      const httpError = createHttpErrorResponse(400, problem);

      const result = toApiError(httpError);

      expect(isProblemError(result)).toBe(true);
      if (isProblemError(result)) {
        expect(result.problem.errors?.['Name']).toContain('Name is required');
        expect(result.problem.errors?.['Email']).toContain('Invalid email format');
      }
    });

    it('TC-125: should handle HttpErrorResponse with null error body', () => {
      const httpError = createHttpErrorResponse(404, null);

      const result = toApiError(httpError);

      expect(isNetworkError(result)).toBe(true);
      if (isNetworkError(result)) {
        expect(result.status).toBe(404);
      }
    });

    it('TC-126: should handle HttpErrorResponse with undefined error body', () => {
      const httpError = createHttpErrorResponse(404, undefined);

      const result = toApiError(httpError);

      expect(isNetworkError(result)).toBe(true);
    });

    it('TC-127: should handle HttpErrorResponse with array error body', () => {
      const httpError = createHttpErrorResponse(400, ['Error 1', 'Error 2']);

      const result = toApiError(httpError);

      expect(isNetworkError(result)).toBe(true);
    });

    it('TC-128: should recognize minimal ProblemDetails with only type', () => {
      const httpError = createHttpErrorResponse(400, {
        type: 'https://example.com/probs/error',
      });

      const result = toApiError(httpError);

      expect(isProblemError(result)).toBe(true);
    });

    it('TC-129: should recognize minimal ProblemDetails with only title', () => {
      const httpError = createHttpErrorResponse(400, {
        title: 'Bad Request',
      });

      const result = toApiError(httpError);

      expect(isProblemError(result)).toBe(true);
    });

    it('TC-130: should recognize minimal ProblemDetails with only status', () => {
      const httpError = createHttpErrorResponse(400, {
        status: 400,
      });

      const result = toApiError(httpError);

      expect(isProblemError(result)).toBe(true);
    });

    it('TC-131: should recognize minimal ProblemDetails with only errors', () => {
      const httpError = createHttpErrorResponse(400, {
        errors: { Field: ['Error'] },
      });

      const result = toApiError(httpError);

      expect(isProblemError(result)).toBe(true);
    });
  });

  describe('Non-HttpErrorResponse handling', () => {
    it('TC-132: should convert Error to UnknownApiError', () => {
      const error = new Error('Something went wrong');

      const result = toApiError(error);

      expect(isUnknownError(result)).toBe(true);
      if (isUnknownError(result)) {
        expect(result.message).toBe('Something went wrong');
        expect(result.originalError).toBe(error);
      }
    });

    it('TC-133: should convert string to UnknownApiError', () => {
      const result = toApiError('String error');

      expect(isUnknownError(result)).toBe(true);
      if (isUnknownError(result)) {
        expect(result.message).toBe('String error');
      }
    });

    it('TC-134: should convert null to UnknownApiError', () => {
      const result = toApiError(null);

      expect(isUnknownError(result)).toBe(true);
      if (isUnknownError(result)) {
        expect(result.message).toBe('An unexpected error occurred');
      }
    });

    it('TC-135: should convert undefined to UnknownApiError', () => {
      const result = toApiError(undefined);

      expect(isUnknownError(result)).toBe(true);
    });

    it('TC-136: should convert object with message to UnknownApiError', () => {
      const result = toApiError({ message: 'Object error' });

      expect(isUnknownError(result)).toBe(true);
      if (isUnknownError(result)) {
        expect(result.message).toBe('Object error');
      }
    });

    it('TC-137: should convert object without message to UnknownApiError', () => {
      const result = toApiError({ code: 'ERR' });

      expect(isUnknownError(result)).toBe(true);
      if (isUnknownError(result)) {
        expect(result.message).toBe('An unexpected error occurred');
      }
    });

    it('TC-138: should convert number to UnknownApiError', () => {
      const result = toApiError(42);

      expect(isUnknownError(result)).toBe(true);
    });
  });
});

// ============================================================================
// Helper Functions
// ============================================================================

describe('Helper Functions', () => {
  describe('hasValidationErrors()', () => {
    it('TC-139: should return true for ProblemApiError with errors', () => {
      const error = problemError(
        createProblemDetails({
          errors: { Name: ['Required'] },
        })
      );

      expect(hasValidationErrors(error)).toBe(true);
    });

    it('TC-140: should return false for ProblemApiError without errors', () => {
      const error = problemError(createProblemDetails({ errors: undefined }));

      expect(hasValidationErrors(error)).toBe(false);
    });

    it('TC-141: should return false for ProblemApiError with empty errors', () => {
      const error = problemError(createProblemDetails({ errors: {} }));

      expect(hasValidationErrors(error)).toBe(false);
    });

    it('TC-142: should return false for NetworkApiError', () => {
      const error = networkError(500, 'Error');

      expect(hasValidationErrors(error)).toBe(false);
    });

    it('TC-143: should return false for UnknownApiError', () => {
      const error = unknownError('Error');

      expect(hasValidationErrors(error)).toBe(false);
    });
  });

  describe('getErrorMessage()', () => {
    it('TC-144: should return detail for ProblemApiError', () => {
      const error = problemError(
        createProblemDetails({
          detail: 'Detailed error message',
          title: 'Title',
        })
      );

      expect(getErrorMessage(error)).toBe('Detailed error message');
    });

    it('TC-145: should fallback to title when detail is empty', () => {
      const error = problemError(
        createProblemDetails({
          detail: '',
          title: 'Error Title',
        })
      );

      expect(getErrorMessage(error)).toBe('Error Title');
    });

    it('TC-146: should fallback to type when detail and title are empty', () => {
      const error = problemError(
        createProblemDetails({
          detail: '',
          title: '',
          type: 'https://example.com/error',
        })
      );

      expect(getErrorMessage(error)).toBe('https://example.com/error');
    });

    it('TC-147: should return default message when all fields empty', () => {
      const error = problemError({
        detail: '',
        title: '',
        type: '',
      });

      expect(getErrorMessage(error, 'Custom default')).toBe('Custom default');
    });

    it('TC-148: should return message for NetworkApiError', () => {
      const error = networkError(500, 'Network failed');

      expect(getErrorMessage(error)).toBe('Network failed');
    });

    it('TC-149: should return message for UnknownApiError', () => {
      const error = unknownError('Unknown failure');

      expect(getErrorMessage(error)).toBe('Unknown failure');
    });

    it('TC-150: should use default message for empty NetworkApiError message', () => {
      const error: NetworkApiError = {
        kind: 'network',
        message: '',
      };

      expect(getErrorMessage(error, 'Default')).toBe('Default');
    });
  });

  describe('getErrorStatus()', () => {
    it('TC-151: should return status from ProblemApiError', () => {
      const error = problemError(createProblemDetails({ status: 400 }), 400);

      expect(getErrorStatus(error)).toBe(400);
    });

    it('TC-152: should return status from problem when error status undefined', () => {
      const error = problemError(createProblemDetails({ status: 422 }));

      expect(getErrorStatus(error)).toBe(422);
    });

    it('TC-153: should return status from NetworkApiError', () => {
      const error = networkError(503, 'Service unavailable');

      expect(getErrorStatus(error)).toBe(503);
    });

    it('TC-154: should return undefined for UnknownApiError', () => {
      const error = unknownError('Error');

      expect(getErrorStatus(error)).toBeUndefined();
    });

    it('TC-155: should return undefined when status not set', () => {
      const error = networkError(undefined, 'Error');

      expect(getErrorStatus(error)).toBeUndefined();
    });
  });

  describe('getValidationErrors()', () => {
    it('TC-156: should convert PascalCase to camelCase field names', () => {
      const error = problemError(
        createProblemDetails({
          errors: {
            UserName: ['Required'],
            EmailAddress: ['Invalid'],
          },
        })
      );

      const errors = getValidationErrors(error);

      expect(errors['userName']).toEqual(['Required']);
      expect(errors['emailAddress']).toEqual(['Invalid']);
    });

    it('TC-157: should return empty object for NetworkApiError', () => {
      const error = networkError(500, 'Error');

      expect(getValidationErrors(error)).toEqual({});
    });

    it('TC-158: should return empty object for UnknownApiError', () => {
      const error = unknownError('Error');

      expect(getValidationErrors(error)).toEqual({});
    });

    it('TC-159: should return empty object when no errors', () => {
      const error = problemError(createProblemDetails({ errors: undefined }));

      expect(getValidationErrors(error)).toEqual({});
    });

    it('TC-160: should preserve multiple error messages per field', () => {
      const error = problemError(
        createProblemDetails({
          errors: {
            Password: ['Too short', 'Must contain number'],
          },
        })
      );

      const errors = getValidationErrors(error);

      expect(errors['password']).toEqual(['Too short', 'Must contain number']);
    });
  });

  describe('mapValidationErrorsToForm()', () => {
    it('TC-161: should set errors on form controls', () => {
      const setErrorsMock = vi.fn();
      const markAsTouchedMock = vi.fn();
      const mockForm = {
        get: (path: string) =>
          path === 'userName'
            ? { setErrors: setErrorsMock, markAsTouched: markAsTouchedMock }
            : null,
      };

      const error = problemError(
        createProblemDetails({
          errors: { UserName: ['Required', 'Too short'] },
        })
      );

      mapValidationErrorsToForm(error, mockForm);

      expect(setErrorsMock).toHaveBeenCalledWith({ api: 'Required, Too short' });
      expect(markAsTouchedMock).toHaveBeenCalled();
    });

    it('TC-162: should skip controls that do not exist', () => {
      const setErrorsMock = vi.fn();
      const mockForm = {
        get: () => null,
      };

      const error = problemError(
        createProblemDetails({
          errors: { NonExistent: ['Error'] },
        })
      );

      mapValidationErrorsToForm(error, mockForm);

      expect(setErrorsMock).not.toHaveBeenCalled();
    });

    it('TC-163: should respect markAsTouched option', () => {
      const setErrorsMock = vi.fn();
      const markAsTouchedMock = vi.fn();
      const mockForm = {
        get: () => ({ setErrors: setErrorsMock, markAsTouched: markAsTouchedMock }),
      };

      const error = problemError(
        createProblemDetails({
          errors: { Field: ['Error'] },
        })
      );

      mapValidationErrorsToForm(error, mockForm, { markAsTouched: false });

      expect(setErrorsMock).toHaveBeenCalled();
      expect(markAsTouchedMock).not.toHaveBeenCalled();
    });

    it('TC-164: should do nothing for non-problem errors', () => {
      const setErrorsMock = vi.fn();
      const mockForm = {
        get: () => ({ setErrors: setErrorsMock, markAsTouched: vi.fn() }),
      };

      const error = networkError(500, 'Error');

      mapValidationErrorsToForm(error, mockForm);

      expect(setErrorsMock).not.toHaveBeenCalled();
    });
  });
});

// ============================================================================
// Edge Cases
// ============================================================================

describe('Edge Cases', () => {
  it('TC-165: should handle ProblemDetails with extension members', () => {
    const problem: ProblemDetails = {
      title: 'Error',
      customField: 'custom value',
      nested: { foo: 'bar' },
    };
    const error = problemError(problem);

    expect(error.problem['customField']).toBe('custom value');
    expect(error.problem['nested']).toEqual({ foo: 'bar' });
  });

  it('TC-166: should handle very long error messages', () => {
    const longMessage = 'x'.repeat(10000);
    const error = unknownError(longMessage);

    expect(getErrorMessage(error)).toBe(longMessage);
  });

  it('TC-167: should handle special characters in error messages', () => {
    const specialMessage = '<script>alert("xss")</script>';
    const error = unknownError(specialMessage);

    expect(getErrorMessage(error)).toBe(specialMessage);
  });

  it('TC-168: should handle unicode in error messages', () => {
    const unicodeMessage = '错误消息 🚫 Ошибка';
    const error = unknownError(unicodeMessage);

    expect(getErrorMessage(error)).toBe(unicodeMessage);
  });

  it('TC-169: should handle circular reference in original error', () => {
    const circularObj: Record<string, unknown> = { name: 'test' };
    circularObj['self'] = circularObj;

    // Should not throw when creating error
    const error = unknownError('Error', circularObj);

    expect(error.originalError).toBe(circularObj);
  });

  it('TC-170: should handle empty string status codes gracefully', () => {
    // HttpErrorResponse always has numeric status, but test robustness
    const httpError = createHttpErrorResponse(0, null);

    const result = toApiError(httpError);

    expect(isNetworkError(result)).toBe(true);
  });
});
