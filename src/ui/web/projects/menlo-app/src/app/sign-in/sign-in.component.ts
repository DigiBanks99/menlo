import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { AuthService } from '../core/auth/auth.service';

@Component({
  selector: 'app-sign-in',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="grid min-h-screen place-items-center px-4 py-8 bg-[--mnl-base]">
      <div
        class="w-full max-w-sm rounded-2xl border border-[--mnl-surface-2] bg-[--mnl-surface-1] p-10 text-center shadow-md"
      >
        <p class="mb-3 text-xs font-bold uppercase tracking-widest text-[--mnl-pink]">Menlo</p>
        <h1 class="mb-4 text-2xl font-bold text-[--mnl-text]">
          Sign in to your household workspace
        </h1>
        <p class="mb-6 text-sm leading-relaxed text-[--mnl-subtext]">
          Use your Microsoft account to access budgets, planning, and household insights in one
          secure place.
        </p>

        <button
          type="button"
          data-testid="sign-in-button"
          class="cursor-pointer rounded-full bg-gradient-to-br from-[#0078d4] to-[#0056b3] px-6 py-3.5 text-base font-semibold text-white transition-[filter] duration-200 hover:brightness-110 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[--mnl-pink] focus-visible:ring-offset-2"
          (click)="signIn()"
        >
          Sign in with Microsoft
        </button>
      </div>
    </section>
  `,
})
export class SignInComponent {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly returnUrl = computed(() => this.route.snapshot.queryParamMap.get('returnUrl') || '/');

  signIn(): void {
    this.authService.login(this.returnUrl());
  }
}
