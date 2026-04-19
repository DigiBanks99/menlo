import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { AuthService } from '../core/auth/auth.service';

@Component({
  selector: 'app-sign-in',
  template: `
    <section class="sign-in-page">
      <div class="sign-in-card">
        <p class="eyebrow">Menlo</p>
        <h1>Sign in to your household workspace</h1>
        <p class="description">
          Use your Microsoft account to access budgets, planning, and household insights in one
          secure place.
        </p>

        <button
          type="button"
          class="sign-in-button"
          data-testid="sign-in-button"
          (click)="signIn()"
        >
          Sign in with Microsoft
        </button>
      </div>
    </section>
  `,
  styles: [
    `
      .sign-in-page {
        min-height: 100%;
        display: grid;
        place-items: center;
        padding: 2rem 1rem;
      }

      .sign-in-card {
        max-width: 32rem;
        padding: 2.5rem;
        border-radius: 1rem;
        background: white;
        border: 1px solid #dee2e6;
        box-shadow: 0 16px 40px rgba(44, 62, 80, 0.08);
        text-align: center;
      }

      .eyebrow {
        margin: 0 0 0.75rem;
        font-size: 0.85rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: #007bff;
      }

      h1 {
        margin: 0 0 1rem;
        color: #2c3e50;
      }

      .description {
        margin: 0 0 1.5rem;
        color: #495057;
        line-height: 1.6;
      }

      .sign-in-button {
        border: none;
        border-radius: 999px;
        padding: 0.85rem 1.5rem;
        font-size: 1rem;
        font-weight: 600;
        color: white;
        background: linear-gradient(135deg, #0078d4, #0056b3);
        cursor: pointer;
      }

      .sign-in-button:hover {
        filter: brightness(1.05);
      }
    `,
  ],
})
export class SignInComponent {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly returnUrl = computed(() => this.route.snapshot.queryParamMap.get('returnUrl') || '/');

  signIn(): void {
    this.authService.login(this.returnUrl());
  }
}
