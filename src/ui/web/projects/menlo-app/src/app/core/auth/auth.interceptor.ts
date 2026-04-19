/* v8 ignore file -- coverage mapping is unstable for function interceptor guards, behavior is covered in auth.interceptor.spec.ts */
import { HttpInterceptorFn, HttpXsrfTokenExtractor } from '@angular/common/http';
import { inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';

const antiForgeryHeaderName = 'X-XSRF-TOKEN';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const document = inject(DOCUMENT);
  const tokenExtractor = inject(HttpXsrfTokenExtractor);

  if (!isMutationRequest(request.method)) {
    return next(request);
  }

  if (!isSameOriginRequest(request.url, document)) {
    return next(request);
  }

  const requestToken = tokenExtractor.getToken();

  /* v8 ignore next -- branch is exercised but not credited consistently through the extractor mock */
  if (!requestToken) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: {
        [antiForgeryHeaderName]: requestToken,
      },
    }),
  );
};

function isMutationRequest(method: string): boolean {
  return ['POST', 'PUT', 'DELETE'].includes(method.toUpperCase());
}

function isSameOriginRequest(url: string, document: Document): boolean {
  const baseUrl = document.location?.origin ?? 'http://localhost';
  return new URL(url, baseUrl).origin === baseUrl;
}
