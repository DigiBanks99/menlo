import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from './auth.service';
import { roleGuard } from './role.guard';

describe('RoleGuard', () => {
  const unauthorizedTree = {} as UrlTree;

  let authService: {
    isLoading: ReturnType<typeof signal<boolean>>;
    isAuthenticated: ReturnType<typeof signal<boolean>>;
    hasRole: ReturnType<typeof vi.fn>;
    loadUser: ReturnType<typeof vi.fn>;
    login: ReturnType<typeof vi.fn>;
  };

  let router: {
    url: string;
    createUrlTree: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    authService = {
      isLoading: signal(false),
      isAuthenticated: signal(true),
      hasRole: vi.fn(() => false),
      loadUser: vi.fn(async () => undefined),
      login: vi.fn(),
    };

    router = {
      url: '/protected',
      createUrlTree: vi.fn(() => unauthorizedTree),
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

    const result = await TestBed.runInInjectionContext(() =>
      roleGuard({ data: { roles: ['Menlo.Admin'] } } as never, {} as never)
    );

    expect(authService.loadUser).toHaveBeenCalledOnce();
    expect(result).toBe(unauthorizedTree);
  });

  it('should redirect to login when not authenticated', async () => {
    authService.isAuthenticated.set(false);

    const result = await TestBed.runInInjectionContext(() =>
      roleGuard({ data: { roles: ['Menlo.Admin'] } } as never, {} as never)
    );

    expect(authService.login).toHaveBeenCalledWith('/protected');
    expect(result).toBe(false);
  });

  it('should allow navigation when no roles are required', async () => {
    const result = await TestBed.runInInjectionContext(() =>
      roleGuard({ data: {} } as never, {} as never)
    );

    expect(result).toBe(true);
  });

  it('should allow navigation when user has required role', async () => {
    authService.hasRole.mockReturnValue(true);

    const result = await TestBed.runInInjectionContext(() =>
      roleGuard({ data: { roles: ['Menlo.Admin'] } } as never, {} as never)
    );

    expect(authService.hasRole).toHaveBeenCalledWith('Menlo.Admin');
    expect(result).toBe(true);
  });

  it('should return unauthorized UrlTree when user lacks required role', async () => {
    authService.hasRole.mockReturnValue(false);

    const result = await TestBed.runInInjectionContext(() =>
      roleGuard({ data: { roles: ['Menlo.Admin'] } } as never, {} as never)
    );

    expect(router.createUrlTree).toHaveBeenCalledWith(['/unauthorized']);
    expect(result).toBe(unauthorizedTree);
  });
});
