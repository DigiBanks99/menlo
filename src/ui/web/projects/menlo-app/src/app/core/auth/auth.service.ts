import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, firstValueFrom, of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { UserProfile } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);

  private readonly userSignal = signal<UserProfile | null>(null);
  private readonly loadingSignal = signal<boolean>(true);
  private readonly errorSignal = signal<string | null>(null);

  readonly user = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.userSignal() !== null);
  readonly isLoading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly roles = computed(() => this.userSignal()?.roles ?? []);

  async loadUser(): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    try {
      const user = await firstValueFrom(
        this.http.get<UserProfile>(`${environment.apiBaseUrl}/auth/user`).pipe(
          catchError(() => of(null))
        )
      );

      this.userSignal.set(user);
    } finally {
      this.loadingSignal.set(false);
    }
  }

  login(returnUrl?: string): void {
    const encodedReturnUrl = encodeURIComponent(returnUrl ?? this.router.url);
    const url = `${environment.apiBaseUrl}/auth/login?returnUrl=${encodedReturnUrl}`;

    // Avoid trying to "navigate" via the SPA router; this is a backend (BFF) redirect.
    this.document.defaultView?.location.assign(url);
  }

  async logout(): Promise<void> {
    await firstValueFrom(this.http.post<void>(`${environment.apiBaseUrl}/auth/logout`, {}));
    this.userSignal.set(null);
  }

  hasRole(...requiredRoles: string[]): boolean {
    if (requiredRoles.length === 0) {
      return false;
    }

    const userRoles = this.roles();
    return requiredRoles.some(role => userRoles.includes(role));
  }
}
