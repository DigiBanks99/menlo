import { signal, WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { HouseholdApiService, HouseholdDto } from 'data-access-menlo-api';
import { OnboardingService } from './onboarding.service';
import { AuthService } from '../auth/auth.service';
import { UserProfile } from '../auth/auth.models';

describe('OnboardingService', () => {
  let service: OnboardingService;
  let userSignal: WritableSignal<UserProfile>;
  let authService: {
    user: WritableSignal<UserProfile>;
    loadUser: ReturnType<typeof vi.fn>;
  };
  let householdService: {
    listHouseholds: ReturnType<typeof vi.fn>;
    createHousehold: ReturnType<typeof vi.fn>;
    joinHousehold: ReturnType<typeof vi.fn>;
  };
  let router: {
    url: string;
    parseUrl: ReturnType<typeof vi.fn>;
    navigate: ReturnType<typeof vi.fn>;
    navigateByUrl: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    userSignal = signal({
      id: 'user-1',
      email: 'user@example.com',
      displayName: 'Test User',
      roles: ['Menlo.User'],
      onboarding: {
        isComplete: false,
        pendingTasks: ['SelectHousehold' as const],
      },
    });

    authService = {
      user: userSignal,
      loadUser: vi.fn(async () => undefined),
    };

    householdService = {
      listHouseholds: vi.fn(),
      createHousehold: vi.fn(),
      joinHousehold: vi.fn(),
    };

    router = {
      url: '/onboarding?returnUrl=%2Fanalytics',
      parseUrl: vi.fn((url: string) => {
        const parsed = new URL(url, 'https://menlo.local');
        return {
          queryParams: Object.fromEntries(parsed.searchParams.entries()),
        };
      }),
      navigate: vi.fn(async () => true),
      navigateByUrl: vi.fn(async () => true),
    };

    TestBed.configureTestingModule({
      providers: [
        OnboardingService,
        { provide: AuthService, useValue: authService },
        { provide: HouseholdApiService, useValue: householdService },
        { provide: Router, useValue: router },
      ],
    });

    service = TestBed.inject(OnboardingService);
  });

  it('should load households into signal state', () => {
    const households: HouseholdDto[] = [{ id: 'household-1', name: 'Family Home' }];
    householdService.listHouseholds.mockReturnValue(of({ households }));

    service.loadHouseholds();

    expect(householdService.listHouseholds).toHaveBeenCalledOnce();
    expect(service.$households()).toEqual(households);
    expect(service.$isLoading()).toBe(false);
    expect(service.$error()).toBeNull();
  });

  it('should create household, refresh the user, and navigate to the return url when onboarding completes', async () => {
    householdService.createHousehold.mockReturnValue(of({ id: 'household-2', name: 'New Family' }));
    authService.loadUser.mockImplementation(async () => {
      userSignal.update((user) => ({
        ...user,
        onboarding: {
          isComplete: true,
          pendingTasks: [],
        },
      }));
    });

    service.createHousehold('New Family');
    await flushMicrotasks();

    expect(householdService.createHousehold).toHaveBeenCalledWith({ name: 'New Family' });
    expect(authService.loadUser).toHaveBeenCalledOnce();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/analytics');
    expect(service.isOnboardingComplete()).toBe(true);
  });

  it('should join household and navigate home when no return url is present', async () => {
    router.url = '/onboarding';
    householdService.joinHousehold.mockReturnValue(of(void 0));
    authService.loadUser.mockImplementation(async () => {
      userSignal.update((user) => ({
        ...user,
        onboarding: {
          isComplete: true,
          pendingTasks: [],
        },
      }));
    });

    service.joinHousehold('household-3');
    await flushMicrotasks();

    expect(householdService.joinHousehold).toHaveBeenCalledWith('household-3');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('should surface mapped API errors when joining a household fails', () => {
    householdService.joinHousehold.mockReturnValue(throwError(() => ({ status: 404 })));

    service.joinHousehold('missing-household');

    expect(service.$error()).toBe('Household not found');
    expect(service.$isLoading()).toBe(false);
  });

  it('should redirect to sign-in when the session expires during a request', () => {
    householdService.listHouseholds.mockReturnValue(throwError(() => ({ status: 401 })));

    service.loadHouseholds();

    expect(router.navigate).toHaveBeenCalledWith(['/sign-in'], {
      queryParams: {
        returnUrl: '/onboarding?returnUrl=%2Fanalytics',
      },
    });
    expect(service.$error()).toBeNull();
  });

  it('should clear errors', () => {
    householdService.listHouseholds.mockReturnValue(
      throwError(() => ({ error: { error: 'Network error' } })),
    );

    service.loadHouseholds();
    expect(service.$error()).toBe('Network error');

    service.clearError();

    expect(service.$error()).toBeNull();
  });
});

async function flushMicrotasks(): Promise<void> {
  await Promise.resolve();
  await Promise.resolve();
}
