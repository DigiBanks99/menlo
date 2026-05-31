import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { OnboardingService } from '../../core/onboarding/onboarding.service';

@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="onboarding-shell">
      <div class="onboarding-card">
        <p class="eyebrow">Menlo</p>
        <h1 data-testid="onboarding-title">Welcome to Menlo</h1>
        <p class="intro">
          Select a household or create a new one to unlock budgets, analytics, and planning tools.
        </p>

        @if (onboardingService.$error(); as error) {
          <div class="error-banner" data-testid="onboarding-error">
            <span>{{ error }}</span>
            <button type="button" class="secondary-button" (click)="onboardingService.clearError()">
              Dismiss
            </button>
          </div>
        }

        <div class="onboarding-grid">
          <section class="section" data-testid="join-household-section">
            <div class="section-header">
              <h2>Join Existing Household</h2>
              <button
                type="button"
                class="secondary-button"
                [disabled]="onboardingService.$isLoading()"
                (click)="onboardingService.loadHouseholds()"
              >
                Refresh
              </button>
            </div>

            @if (onboardingService.$isLoading()) {
              <p class="status-message" data-testid="loading-households">Loading households...</p>
            }

            @if (onboardingService.$households().length > 0) {
              <ul class="household-list" data-testid="household-list">
                @for (household of onboardingService.$households(); track household.id) {
                  <li>
                    <div>
                      <p class="household-name">{{ household.name }}</p>
                      <p class="household-id">{{ household.id }}</p>
                    </div>
                    <button
                      type="button"
                      class="primary-button"
                      [disabled]="onboardingService.$isLoading()"
                      [attr.data-testid]="'join-household-' + household.id"
                      (click)="joinHousehold(household.id)"
                    >
                      Join
                    </button>
                  </li>
                }
              </ul>
            } @else if (!onboardingService.$isLoading()) {
              <p class="status-message" data-testid="household-empty-state">
                No existing households are available yet.
              </p>
            }
          </section>

          <section class="section" data-testid="create-household-section">
            <h2>Create New Household</h2>
            <form [formGroup]="createForm" (ngSubmit)="createHousehold()" data-testid="create-household-form">
              <label for="householdName">Household Name</label>
              <input
                id="householdName"
                data-testid="household-name-input"
                type="text"
                formControlName="name"
                placeholder="e.g. Smith Family"
                [readOnly]="onboardingService.$isLoading()"
                [attr.aria-disabled]="onboardingService.$isLoading()"
              />

              @if (nameControl.invalid && (nameControl.dirty || nameControl.touched)) {
                <p class="field-error" data-testid="household-name-error">
                  Household name is required and must be 1-100 characters long.
                </p>
              }

              <button
                type="submit"
                class="primary-button submit-button"
                data-testid="create-household-button"
                [disabled]="createForm.invalid || onboardingService.$isLoading()"
              >
                {{ onboardingService.$isLoading() ? 'Creating...' : 'Create Household' }}
              </button>
            </form>
          </section>
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .onboarding-shell {
        min-height: 100vh;
        display: grid;
        place-items: center;
        padding: 2rem 1rem;
        background: linear-gradient(180deg, rgba(241, 236, 255, 0.75), rgba(255, 255, 255, 0.98));
      }

      .onboarding-card {
        width: min(100%, 72rem);
        border-radius: 1.5rem;
        border: 1px solid #e6deff;
        background: #ffffff;
        padding: 2rem;
        box-shadow: 0 20px 45px rgba(78, 43, 130, 0.12);
      }

      .eyebrow {
        margin: 0 0 0.5rem;
        color: #a21caf;
        font-size: 0.75rem;
        font-weight: 700;
        letter-spacing: 0.24em;
        text-transform: uppercase;
      }

      h1 {
        margin: 0;
        color: #22143d;
        font-size: clamp(2rem, 5vw, 2.75rem);
      }

      .intro {
        max-width: 42rem;
        margin: 0.75rem 0 0;
        color: #5f5672;
        line-height: 1.6;
      }

      .error-banner {
        margin-top: 1.5rem;
        padding: 1rem 1.25rem;
        border-radius: 1rem;
        border: 1px solid #fecaca;
        background: #fef2f2;
        color: #991b1b;
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 1rem;
      }

      .onboarding-grid {
        display: grid;
        gap: 1.5rem;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        margin-top: 2rem;
      }

      .section {
        border-radius: 1.25rem;
        border: 1px solid #ede9fe;
        background: #faf8ff;
        padding: 1.5rem;
      }

      .section-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 1rem;
      }

      .section h2 {
        margin: 0 0 1rem;
        color: #31224d;
      }

      .household-list {
        list-style: none;
        padding: 0;
        margin: 0;
        display: grid;
        gap: 0.75rem;
      }

      .household-list li {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 1rem;
        padding: 1rem;
        border-radius: 1rem;
        background: #ffffff;
        border: 1px solid #e9ddff;
      }

      .household-name {
        margin: 0;
        color: #22143d;
        font-weight: 700;
      }

      .household-id {
        margin: 0.25rem 0 0;
        color: #7c7490;
        font-size: 0.75rem;
        word-break: break-all;
      }

      form {
        display: grid;
        gap: 0.75rem;
      }

      label {
        color: #31224d;
        font-weight: 600;
      }

      input {
        width: 100%;
        border: 1px solid #d4c8ef;
        border-radius: 0.85rem;
        padding: 0.85rem 1rem;
        font: inherit;
      }

      .primary-button,
      .secondary-button {
        border: none;
        border-radius: 999px;
        padding: 0.75rem 1.1rem;
        font: inherit;
        font-weight: 700;
        cursor: pointer;
      }

      .primary-button {
        background: linear-gradient(135deg, #7c3aed, #c026d3);
        color: white;
      }

      .secondary-button {
        background: #f3e8ff;
        color: #7c3aed;
      }

      .submit-button {
        justify-self: start;
      }

      .primary-button:disabled,
      .secondary-button:disabled,
      input:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      .field-error {
        margin: 0;
        color: #b91c1c;
        font-size: 0.875rem;
      }

      .status-message {
        margin: 0;
        color: #5f5672;
      }

      @media (max-width: 760px) {
        .onboarding-card {
          padding: 1.5rem;
        }

        .onboarding-grid {
          grid-template-columns: 1fr;
        }

        .error-banner,
        .household-list li,
        .section-header {
          align-items: flex-start;
          flex-direction: column;
        }
      }
    `,
  ],
})
export class OnboardingComponent implements OnInit {
  readonly onboardingService = inject(OnboardingService);
  private readonly fb = inject(FormBuilder);

  readonly createForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(100)]],
  });

  get nameControl() {
    return this.createForm.controls.name;
  }

  ngOnInit(): void {
    this.onboardingService.loadHouseholds();
  }

  joinHousehold(householdId: string): void {
    this.onboardingService.joinHousehold(householdId);
  }

  createHousehold(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const name = this.nameControl.getRawValue().trim();
    if (!name) {
      this.nameControl.setErrors({ required: true });
      this.nameControl.markAsTouched();
      return;
    }

    this.onboardingService.createHousehold(name);
  }
}
