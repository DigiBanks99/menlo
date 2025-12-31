import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

export const roleGuard: CanActivateFn = async (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isLoading()) {
    await authService.loadUser();
  }

  if (!authService.isAuthenticated()) {
    authService.login(router.url);
    return false;
  }

  const requiredRoles = route.data['roles'] as string[] | undefined;
  if (!requiredRoles || requiredRoles.length === 0) {
    return true;
  }

  if (authService.hasRole(...requiredRoles)) {
    return true;
  }

  return router.createUrlTree(['/unauthorized']);
};
