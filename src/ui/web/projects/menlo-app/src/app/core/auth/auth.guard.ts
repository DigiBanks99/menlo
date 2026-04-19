import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isLoading()) {
    await authService.loadUser();
  }

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/sign-in'], {
    queryParams: {
      returnUrl: state.url || '/',
    },
  });
};
