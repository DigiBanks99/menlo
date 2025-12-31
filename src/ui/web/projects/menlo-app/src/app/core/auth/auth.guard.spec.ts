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
    login: ReturnType<typeof vi.fn>;
  };

  let router: {
    url: string;
  };

  beforeEach(() => {
    authService = {
      isLoading: signal(false),
      isAuthenticated: signal(false),
      loadUser: vi.fn(async () => undefined),
      login: vi.fn(),
    };

    router = {
      url: '/protected',
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

    const result = await TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(authService.loadUser).toHaveBeenCalledOnce();
    expect(result).toBe(true);
  });

  it('should allow navigation when authenticated', async () => {
    authService.isAuthenticated.set(true);

    const result = await TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(authService.login).not.toHaveBeenCalled();
    expect(result).toBe(true);
  });

  it('should redirect to login when not authenticated', async () => {
    authService.isAuthenticated.set(false);

    const result = await TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(authService.login).toHaveBeenCalledWith('/protected');
    expect(result).toBe(false);
  });
});
