import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { type Meta, type StoryObj } from '@storybook/angular';

import { MnlButtonComponent } from '../../atoms/button';
import { MnlInputComponent } from '../../atoms/input';
import { MnlSelectComponent } from '../../atoms/select';
import { MnlAmountInputComponent } from '../../molecules/amount-input';
import { MnlFormFieldComponent } from '../../molecules/form-field';
import { MnlPanelComponent, type MnlPanelMode } from '../../molecules/panel';
import { MnlFormLayoutComponent } from './form-layout.component';

const viewportOptions = {
  desktop1440: {
    name: 'Desktop 1440',
    styles: {
      height: '1024px',
      width: '1440px',
    },
    type: 'desktop',
  },
  mobile390: {
    name: 'Mobile 390',
    styles: {
      height: '844px',
      width: '390px',
    },
    type: 'mobile',
  },
} as const;

@Component({
  selector: 'lib-form-layout-story-preview',
  standalone: true,
  imports: [
    MnlAmountInputComponent,
    MnlButtonComponent,
    MnlFormFieldComponent,
    MnlFormLayoutComponent,
    MnlInputComponent,
    MnlPanelComponent,
    MnlSelectComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-4xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Organisms
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Form Layout</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            The layout keeps projected sections readable, lets consumers opt into desktop grids, and
            pins the action bar to the panel edge while the form body scrolls.
          </p>

          <div class="mt-5 flex flex-wrap gap-3">
            <mnl-button (pressed)="open.set(true)">Open form</mnl-button>
            <mnl-button variant="secondary" (pressed)="toggleTheme()">
              Toggle {{ activeTheme() === 'light' ? 'dark' : 'light' }} mode
            </mnl-button>
          </div>
        </section>
      </div>

      <mnl-panel [mode]="mode()" [open]="open()" (closed)="open.set(false)">
        <div mnlPanelHeader>
          <div class="space-y-1">
            <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-mnl-accent">
              Budget details
            </p>
            <h2 class="m-0 text-2xl font-bold tracking-tight">Create budget item</h2>
            <p class="m-0 text-sm leading-6 text-mnl-subtext">
              Preview the shared form structure inside a responsive panel.
            </p>
          </div>
        </div>

        <form class="min-h-full" (submit)="handleSubmit($event)">
          <mnl-form-layout>
            <div class="space-y-2" mnlFormTitle>
              <h3 class="m-0 text-xl font-semibold">Monthly household expense</h3>
              <p class="m-0 text-sm leading-6 text-mnl-subtext">
                Validation copy, grouped amount entry, and sticky actions stay consistent across
                both themes.
              </p>
            </div>

            <section class="space-y-4">
              <mnl-form-field inputId="story-budget-item-title" label="Title" [required]="true">
                <mnl-input id="story-budget-item-title" placeholder="School fees"></mnl-input>
              </mnl-form-field>

              <mnl-form-field
                inputId="story-budget-item-notes"
                label="Notes"
                hint="Optional family context for the monthly plan."
              >
                <mnl-input
                  id="story-budget-item-notes"
                  placeholder="Uniform payment due before term starts"
                ></mnl-input>
              </mnl-form-field>
            </section>

            <section class="space-y-4">
              <div class="space-y-1">
                <h4 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  Finance details
                </h4>
                <p class="m-0 text-sm leading-6 text-mnl-subtext">
                  Consumers can opt into a responsive two-column grid for shorter fields.
                </p>
              </div>

              <div class="grid gap-4 md:grid-cols-2">
                <mnl-form-field
                  inputId="story-budget-item-amount"
                  label="Planned amount"
                  [required]="true"
                >
                  <mnl-amount-input
                    id="story-budget-item-amount"
                    error="Enter a valid monthly amount"
                    placeholder="0.00"
                  ></mnl-amount-input>
                </mnl-form-field>

                <mnl-form-field
                  inputId="story-budget-item-category"
                  label="Category"
                  [required]="true"
                >
                  <mnl-select
                    id="story-budget-item-category"
                    [options]="categoryOptions"
                    placeholder="Choose a category"
                  ></mnl-select>
                </mnl-form-field>
              </div>
            </section>

            <section class="space-y-4">
              <div class="space-y-1">
                <h4 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  Allocation
                </h4>
                <p class="m-0 text-sm leading-6 text-mnl-subtext">
                  Additional projected sections stack with consistent spacing on mobile and desktop.
                </p>
              </div>

              <div class="grid gap-4 md:grid-cols-2">
                <mnl-form-field inputId="story-budget-item-frequency" label="Frequency">
                  <mnl-select
                    id="story-budget-item-frequency"
                    [options]="frequencyOptions"
                  ></mnl-select>
                </mnl-form-field>

                <mnl-form-field
                  inputId="story-budget-item-owner"
                  label="Household owner"
                  hint="Used for attribution and reminders."
                >
                  <mnl-input id="story-budget-item-owner" placeholder="Mom"></mnl-input>
                </mnl-form-field>
              </div>
            </section>

            <div mnlFormActions>
              <mnl-button variant="ghost" type="button" (pressed)="handleClear()">
                Clear
              </mnl-button>
              <mnl-button type="submit">Save item</mnl-button>
            </div>
          </mnl-form-layout>
        </form>
      </mnl-panel>
    </div>
  `,
})
class FormLayoutStoryPreviewComponent {
  readonly mode = input<MnlPanelMode>('dialog');
  readonly themeMode = input<'light' | 'dark'>('light');

  protected readonly activeTheme = signal<'light' | 'dark'>('light');
  protected readonly open = signal(true);
  protected readonly categoryOptions = [
    { value: 'school', label: 'School' },
    { value: 'groceries', label: 'Groceries' },
    { value: 'transport', label: 'Transport' },
  ] as const;
  protected readonly frequencyOptions = [
    { value: 'monthly', label: 'Monthly' },
    { value: 'quarterly', label: 'Quarterly' },
    { value: 'annual', label: 'Annual' },
  ] as const;

  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly previousDarkMode = this.document.documentElement.classList.contains('dark');

  constructor() {
    effect(
      () => {
        this.activeTheme.set(this.themeMode());
      },
      { allowSignalWrites: true },
    );

    effect(() => {
      this.document.documentElement.classList.toggle('dark', this.activeTheme() === 'dark');
    });

    this.destroyRef.onDestroy(() => {
      this.document.documentElement.classList.toggle('dark', this.previousDarkMode);
    });
  }

  protected handleClear(): void {
    this.open.set(true);
  }

  protected handleSubmit(event: Event): void {
    event.preventDefault();
    this.open.set(false);
  }

  protected toggleTheme(): void {
    this.activeTheme.update((theme) => (theme === 'light' ? 'dark' : 'light'));
  }
}

const meta: Meta<FormLayoutStoryPreviewComponent> = {
  title: 'Organisms/Form Layout',
  component: FormLayoutStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
    viewport: {
      options: viewportOptions,
    },
  },
};

export default meta;

type Story = StoryObj<FormLayoutStoryPreviewComponent>;

export const DialogLight: Story = {
  args: {
    mode: 'dialog',
    themeMode: 'light',
  },
  parameters: {
    viewport: {
      defaultViewport: 'desktop1440',
    },
  },
};

export const DialogDark: Story = {
  args: {
    mode: 'dialog',
    themeMode: 'dark',
  },
  parameters: {
    viewport: {
      defaultViewport: 'desktop1440',
    },
  },
};

export const SheetMobile: Story = {
  args: {
    mode: 'sheet',
    themeMode: 'light',
  },
  parameters: {
    viewport: {
      defaultViewport: 'mobile390',
    },
  },
};
