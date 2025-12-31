import { HttpInterceptorFn } from '@angular/common/http';

import { environment } from '../../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.url.startsWith(environment.apiBaseUrl)) {
    return next(
      request.clone({
        withCredentials: true,
      })
    );
  }

  return next(request);
};
