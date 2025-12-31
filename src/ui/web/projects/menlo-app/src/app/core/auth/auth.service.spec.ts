import { DOCUMENT } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { UserProfile } from './auth.models';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let authService: AuthService;
  let httpController: HttpTestingController;
  let locationAssign: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    locationAssign = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: DOCUMENT,
          useValue: {
            defaultView: {
              location: {
                assign: locationAssign,
              },
            },
          },
        },
        {
          provide: Router,
          useValue: {
            url: '/current',
          },
        },
      ],
    });

    authService = TestBed.inject(AuthService);
    httpController = TestBed.inject(HttpTestingController);
  });

  it('should load user', async () => {
    const loadPromise = authService.loadUser();

    const request = httpController.expectOne(`${environment.apiBaseUrl}/auth/user`);
    request.flush(createUserProfile());

    await loadPromise;

    expectLoadingIs(authService, false);
    expectUserIsPresent(authService);
  });

  it('should clear user when load fails', async () => {
    const loadPromise = authService.loadUser();

    const request = httpController.expectOne(`${environment.apiBaseUrl}/auth/user`);
    request.error(new ProgressEvent('error'));

    await loadPromise;

    expectLoadingIs(authService, false);
    expectUserIsNull(authService);
  });

  it('should clear user on logout', async () => {
    await loadUser(authService, httpController, createUserProfile());

    const logoutPromise = authService.logout();

    const request = httpController.expectOne(`${environment.apiBaseUrl}/auth/logout`);
    expect(request.request.method).toBe('POST');
    request.flush(null);

    await logoutPromise;

    expectUserIsNull(authService);
  });

  it('should redirect to backend login with explicit returnUrl', () => {
    authService.login('/somewhere');

    expect(locationAssign).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}/auth/login?returnUrl=${encodeURIComponent('/somewhere')}`
    );
  });

  it('should use router url when returnUrl is not provided', () => {
    authService.login();

    expect(locationAssign).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}/auth/login?returnUrl=${encodeURIComponent('/current')}`
    );
  });

  it('should not throw if document has no defaultView', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: DOCUMENT,
          useValue: {
            defaultView: null,
          },
        },
        {
          provide: Router,
          useValue: {
            url: '/current',
          },
        },
      ],
    });

    const service = TestBed.inject(AuthService);

    expect(() => service.login('/somewhere')).not.toThrow();
  });

  it('should return false when checking roles without any role arguments', () => {
    expect(authService.hasRole()).toBe(false);
  });

  it('should return false when no user is loaded and roles are required', () => {
    expect(authService.hasRole('Menlo.User')).toBe(false);
  });

  it('should return true when user has any required role', async () => {
    await loadUser(authService, httpController, {
      ...createUserProfile(),
      roles: ['Menlo.User'],
    });

    expect(authService.hasRole('Menlo.Admin', 'Menlo.User')).toBe(true);
  });

  it('should return false when user has none of the required roles', async () => {
    await loadUser(authService, httpController, {
      ...createUserProfile(),
      roles: ['Menlo.Reader'],
    });

    expect(authService.hasRole('Menlo.Admin', 'Menlo.User')).toBe(false);
  });

  function expectLoadingIs(service: AuthService, expected: boolean): void {
    expect(service.isLoading()).toBe(expected);
  }

  function expectUserIsPresent(service: AuthService): void {
    expect(service.user()).not.toBeNull();
    expect(service.isAuthenticated()).toBe(true);
  }

  function expectUserIsNull(service: AuthService): void {
    expect(service.user()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  }

  function createUserProfile(): UserProfile {
    return {
      id: 'test-user',
      email: 'test@example.com',
      displayName: 'Test User',
      roles: ['Menlo.User'],
    };
  }

  async function loadUser(
    service: AuthService,
    controller: HttpTestingController,
    profile: UserProfile
  ): Promise<void> {
    const loadPromise = service.loadUser();

    const request = controller.expectOne(`${environment.apiBaseUrl}/auth/user`);
    request.flush(profile);

    await loadPromise;
  }
});
