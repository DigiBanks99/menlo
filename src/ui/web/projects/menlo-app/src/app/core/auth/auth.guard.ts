import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isLoading()) {
    await authService.loadUser();
  }

  if (authService.isAuthenticated()) {
    return true;
  }

  authService.login(router.url);
  return false;
};
