import { DOCUMENT } from '@angular/common';
import { HttpRequest, HttpXsrfTokenExtractor } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { authInterceptor } from './auth.interceptor';

describe('AuthInterceptor', () => {
  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should add the anti-forgery header to mutation requests when the cookie is present', () => {
    const document = createDocument();
    const request = new HttpRequest('POST', '/api/budgets/2026');

    const next = vi.fn((req: HttpRequest<unknown>) => {
      expect(req).not.toBe(request);
      expect(req.headers.get('X-XSRF-TOKEN')).toBe('csrf-token');
      expect(req.withCredentials).toBe(false);
      return undefined as never;
    });

    runInInjectionContext(document, 'csrf-token', () => authInterceptor(request, next));

    expect(next).toHaveBeenCalledOnce();
  });

  it('should not add the anti-forgery header to safe requests', () => {
    const document = createDocument();
    const request = new HttpRequest('GET', '/api/weatherforecast');

    const next = vi.fn((req: HttpRequest<unknown>) => {
      expect(req).toBe(request);
      expect(req.headers.has('X-XSRF-TOKEN')).toBe(false);
      expect(req.withCredentials).toBe(false);
      return undefined as never;
    });

    runInInjectionContext(document, 'csrf-token', () => authInterceptor(request, next));

    expect(next).toHaveBeenCalledOnce();
  });

  it('should forward mutation requests unchanged when the anti-forgery cookie is absent', () => {
    const document = createDocument();
    const request = new HttpRequest('DELETE', '/api/budgets/2026');

    const next = vi.fn((req: HttpRequest<unknown>) => {
      expect(req).toBe(request);
      expect(req.headers.has('X-XSRF-TOKEN')).toBe(false);
      return undefined as never;
    });

    runInInjectionContext(document, null, () => authInterceptor(request, next));

    expect(next).toHaveBeenCalledOnce();
  });

  it('should forward mutation requests unchanged when the token extractor returns an empty string', () => {
    const document = createDocument();
    const request = new HttpRequest('POST', '/api/budgets/2026');

    const next = vi.fn((req: HttpRequest<unknown>) => {
      expect(req).toBe(request);
      expect(req.headers.has('X-XSRF-TOKEN')).toBe(false);
      return undefined as never;
    });

    runInInjectionContext(document, '', () => authInterceptor(request, next));

    expect(next).toHaveBeenCalledOnce();
  });

  it('should not send the anti-forgery header to cross-origin mutation requests', () => {
    const document = createDocument();
    const request = new HttpRequest('POST', 'https://example.com/api/budgets');

    const next = vi.fn((req: HttpRequest<unknown>) => {
      expect(req).toBe(request);
      expect(req.headers.has('X-XSRF-TOKEN')).toBe(false);
      return undefined as never;
    });

    runInInjectionContext(document, 'csrf-token', () => authInterceptor(request, next));

    expect(next).toHaveBeenCalledOnce();
  });

  it('should fall back to localhost when the document has no location origin', () => {
    const document = createDocument(undefined);
    const request = new HttpRequest('PUT', '/api/budgets/2026');

    const next = vi.fn((req: HttpRequest<unknown>) => {
      expect(req.headers.get('X-XSRF-TOKEN')).toBe('csrf-token');
      return undefined as never;
    });

    runInInjectionContext(document, 'csrf-token', () => authInterceptor(request, next));

    expect(next).toHaveBeenCalledOnce();
  });
});

function runInInjectionContext(document: Document, token: string | null, action: () => void): void {
  TestBed.configureTestingModule({
    providers: [
      { provide: DOCUMENT, useValue: document },
      {
        provide: HttpXsrfTokenExtractor,
        useValue: {
          getToken: () => token,
        } satisfies Pick<HttpXsrfTokenExtractor, 'getToken'>,
      },
    ],
  });

  TestBed.runInInjectionContext(action);
}

function createDocument(origin: string | undefined = 'https://localhost:4200'): Document {
  return (
    origin === undefined
      ? {}
      : {
          location: {
            origin,
          } as Location,
        }
  ) as Document;
}
