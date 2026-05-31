import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { HouseholdApiService, HouseholdDto } from 'data-access-menlo-api';
import { AuthService } from '../auth/auth.service';

@Injectable({
  providedIn: 'root',
})
export class OnboardingService {
  private readonly authService = inject(AuthService);
  private readonly householdService = inject(HouseholdApiService);
  private readonly router = inject(Router);

  private readonly households = signal<HouseholdDto[]>([]);
  private readonly isLoading = signal(false);
  private readonly error = signal<string | null>(null);

  readonly isOnboardingComplete = computed(() => this.authService.user()?.onboarding?.isComplete ?? false);
  readonly pendingTasks = computed(() => this.authService.user()?.onboarding?.pendingTasks ?? []);
  readonly $households = this.households.asReadonly();
  readonly $isLoading = this.isLoading.asReadonly();
  readonly $error = this.error.asReadonly();

  loadHouseholds(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.householdService
      .listHouseholds()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => this.households.set(response.households),
        error: (error) => {
          this.households.set([]);
          this.handleError(error, 'Failed to load households');
        },
      });
  }

  createHousehold(name: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.householdService
      .createHousehold({ name })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: async (household) => {
          await this.refreshUserAndNavigate();

          if (!this.isOnboardingComplete()) {
            this.households.update((existing) => {
              if (existing.some((item) => item.id === household.id)) {
                return existing;
              }

              return [...existing, household];
            });
          }
        },
        error: (error) => this.handleError(error, 'Failed to create household'),
      });
  }

  joinHousehold(householdId: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.householdService
      .joinHousehold(householdId)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: async () => {
          await this.refreshUserAndNavigate();
        },
        error: (error) => this.handleError(error, 'Failed to join household'),
      });
  }

  clearError(): void {
    this.error.set(null);
  }

  private async refreshUserAndNavigate(): Promise<void> {
    await this.authService.loadUser();

    if (!this.isOnboardingComplete()) {
      return;
    }

    await this.router.navigateByUrl(this.completionReturnUrl());
  }

  private completionReturnUrl(): string {
    const returnUrl = this.router.parseUrl(this.router.url).queryParams['returnUrl'];

    return typeof returnUrl === 'string' && returnUrl.startsWith('/') ? returnUrl : '/';
  }

  private handleError(error: { status?: number; error?: { error?: string } }, fallback: string): void {
    if (error.status === 401) {
      void this.router.navigate(['/sign-in'], {
        queryParams: {
          returnUrl: this.router.url || '/',
        },
      });
      return;
    }

    const errorMessage = error.error?.error ?? this.mapStatusToMessage(error.status) ?? fallback;
    this.error.set(errorMessage);
  }

  private mapStatusToMessage(status?: number): string | null {
    switch (status) {
      case 403:
        return 'You are not authorized to join this household';
      case 404:
        return 'Household not found';
      case 409:
        return 'A household with this name already exists';
      default:
        return null;
    }
  }
}
