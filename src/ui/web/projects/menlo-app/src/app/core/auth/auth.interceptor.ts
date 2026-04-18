import { HttpInterceptorFn } from '@angular/common/http';

import { environment } from '../../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const isApiRequest = environment.apiBaseUrl
    ? request.url.startsWith(environment.apiBaseUrl)
    : request.url.startsWith('/api') || request.url.startsWith('/auth');

  if (isApiRequest) {
    return next(
      request.clone({
        withCredentials: true,
      }),
    );
  }

  return next(request);
};
