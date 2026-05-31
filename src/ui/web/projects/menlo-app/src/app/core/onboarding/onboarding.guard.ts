import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../auth/auth.service';
import { OnboardingService } from './onboarding.service';

export const onboardingGuard: CanActivateFn = async (_route, state) => {
  const authService = inject(AuthService);
  const onboardingService = inject(OnboardingService);
  const router = inject(Router);

  if (authService.isLoading()) {
    await authService.loadUser();
  }

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/sign-in'], {
      queryParams: {
        returnUrl: state.url || '/',
      },
    });
  }

  if (onboardingService.isOnboardingComplete()) {
    return true;
  }

  return router.createUrlTree(['/onboarding'], {
    queryParams: {
      returnUrl: state.url || '/',
    },
  });
};

export const noReenterOnboardingGuard: CanActivateFn = (_route, _state) => {
  const onboardingService = inject(OnboardingService);
  const router = inject(Router);

  if (onboardingService.isOnboardingComplete()) {
    return router.createUrlTree(['/']);
  }

  return true;
};
