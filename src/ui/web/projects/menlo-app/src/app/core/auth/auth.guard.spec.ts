import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('AuthGuard', () => {
  let authService: {
    isLoading: ReturnType<typeof signal<boolean>>;
    isAuthenticated: ReturnType<typeof signal<boolean>>;
    loadUser: ReturnType<typeof vi.fn>;
  };

  let router: {
    createUrlTree: ReturnType<typeof vi.fn>;
  };
  let redirectTree: { redirected: boolean };

  beforeEach(() => {
    authService = {
      isLoading: signal(false),
      isAuthenticated: signal(false),
      loadUser: vi.fn(async () => undefined),
    };
    redirectTree = { redirected: true };

    router = {
      createUrlTree: vi.fn(() => redirectTree),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
      ],
    });
  });

  it('should load user when currently loading', async () => {
    authService.isLoading.set(true);
    authService.isAuthenticated.set(true);

    const result = await TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/protected' } as never),
    );

    expect(authService.loadUser).toHaveBeenCalledOnce();
    expect(result).toBe(true);
  });

  it('should allow navigation when authenticated', async () => {
    authService.isAuthenticated.set(true);

    const result = await TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/protected' } as never),
    );

    expect(router.createUrlTree).not.toHaveBeenCalled();
    expect(result).toBe(true);
  });

  it('should redirect to sign-in when not authenticated', async () => {
    authService.isAuthenticated.set(false);

    const result = await TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/protected' } as never),
    );

    expect(router.createUrlTree).toHaveBeenCalledWith(['/sign-in'], {
      queryParams: { returnUrl: '/protected' },
    });
    expect(result).toBe(redirectTree);
  });

  it('should fall back to the home return url when the target url is empty', async () => {
    authService.isAuthenticated.set(false);

    await TestBed.runInInjectionContext(() => authGuard({} as never, { url: '' } as never));

    expect(router.createUrlTree).toHaveBeenCalledWith(['/sign-in'], {
      queryParams: { returnUrl: '/' },
    });
  });
});
