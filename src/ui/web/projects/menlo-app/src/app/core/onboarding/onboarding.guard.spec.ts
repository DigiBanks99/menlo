import { signal, WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../auth/auth.service';
import { noReenterOnboardingGuard, onboardingGuard } from './onboarding.guard';
import { OnboardingService } from './onboarding.service';

describe('Onboarding guards', () => {
  let authService: {
    isLoading: WritableSignal<boolean>;
    isAuthenticated: WritableSignal<boolean>;
    loadUser: ReturnType<typeof vi.fn>;
  };
  let onboardingService: {
    isOnboardingComplete: WritableSignal<boolean>;
  };
  let router: {
    createUrlTree: ReturnType<typeof vi.fn>;
  };
  let redirectTree: { redirected: boolean };

  beforeEach(() => {
    authService = {
      isLoading: signal(false),
      isAuthenticated: signal(true),
      loadUser: vi.fn(async () => undefined),
    };

    onboardingService = {
      isOnboardingComplete: signal(false),
    };

    redirectTree = { redirected: true };
    router = {
      createUrlTree: vi.fn(() => redirectTree),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: OnboardingService, useValue: onboardingService },
        { provide: Router, useValue: router },
      ],
    });
  });

  it('should load the user when auth state is still loading', async () => {
    authService.isLoading.set(true);
    authService.isAuthenticated.set(true);
    onboardingService.isOnboardingComplete.set(true);

    const result = await TestBed.runInInjectionContext(() =>
      onboardingGuard({} as never, { url: '/budgets' } as never),
    );

    expect(authService.loadUser).toHaveBeenCalledOnce();
    expect(result).toBe(true);
  });

  it('should redirect unauthenticated users to sign-in', async () => {
    authService.isAuthenticated.set(false);

    const result = await TestBed.runInInjectionContext(() =>
      onboardingGuard({} as never, { url: '/budgets' } as never),
    );

    expect(router.createUrlTree).toHaveBeenCalledWith(['/sign-in'], {
      queryParams: { returnUrl: '/budgets' },
    });
    expect(result).toBe(redirectTree);
  });

  it('should redirect incomplete users to onboarding', async () => {
    onboardingService.isOnboardingComplete.set(false);

    const result = await TestBed.runInInjectionContext(() =>
      onboardingGuard({} as never, { url: '/analytics' } as never),
    );

    expect(router.createUrlTree).toHaveBeenCalledWith(['/onboarding'], {
      queryParams: { returnUrl: '/analytics' },
    });
    expect(result).toBe(redirectTree);
  });

  it('should allow onboarded users through protected routes', async () => {
    onboardingService.isOnboardingComplete.set(true);

    const result = await TestBed.runInInjectionContext(() =>
      onboardingGuard({} as never, { url: '/analytics' } as never),
    );

    expect(router.createUrlTree).not.toHaveBeenCalled();
    expect(result).toBe(true);
  });

  it('should redirect onboarded users away from the onboarding page', () => {
    onboardingService.isOnboardingComplete.set(true);

    const result = TestBed.runInInjectionContext(() =>
      noReenterOnboardingGuard({} as never, { url: '/onboarding' } as never),
    );

    expect(router.createUrlTree).toHaveBeenCalledWith(['/']);
    expect(result).toBe(redirectTree);
  });

  it('should allow incomplete users to stay on the onboarding page', () => {
    onboardingService.isOnboardingComplete.set(false);

    const result = TestBed.runInInjectionContext(() =>
      noReenterOnboardingGuard({} as never, { url: '/onboarding' } as never),
    );

    expect(result).toBe(true);
  });
});
