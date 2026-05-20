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
import { MnlAmountInputComponent } from '../amount-input';
import { MnlFormFieldComponent } from '../form-field';
import { MnlPanelComponent, type MnlPanelMode } from './panel.component';

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
  selector: 'lib-panel-story-preview',
  standalone: true,
  imports: [
    MnlAmountInputComponent,
    MnlButtonComponent,
    MnlFormFieldComponent,
    MnlInputComponent,
    MnlPanelComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-4xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Molecules
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Panel</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            The panel adapts between a mobile bottom sheet and a desktop dialog while keeping the
            same accessible, form-friendly API.
          </p>

          <div class="mt-5 flex flex-wrap gap-3">
            <mnl-button (pressed)="open.set(true)">Open panel</mnl-button>
            <mnl-button variant="secondary" (pressed)="toggleTheme()">
              Toggle {{ activeTheme() === 'light' ? 'dark' : 'light' }} mode
            </mnl-button>
          </div>
        </section>

        <section class="rounded-2xl bg-mnl-surface-alt/60 p-6 ring-1 ring-mnl-border">
          <div class="grid gap-4 md:grid-cols-2">
            <article class="rounded-2xl bg-mnl-surface p-5 ring-1 ring-mnl-border">
              <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-mnl-subtext">
                Mode
              </p>
              <p class="mt-3 mb-0 text-2xl font-bold tracking-tight capitalize">
                {{ mode() }}
              </p>
            </article>

            <article class="rounded-2xl bg-mnl-surface p-5 ring-1 ring-mnl-border">
              <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-mnl-subtext">
                Theme
              </p>
              <p class="mt-3 mb-0 text-2xl font-bold tracking-tight capitalize">
                {{ activeTheme() }}
              </p>
            </article>
          </div>
        </section>
      </div>

      <mnl-panel [mode]="mode()" [open]="open()" (closed)="open.set(false)">
        <div mnlPanelHeader>
          <div class="space-y-1">
            <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-mnl-accent">
              Budget form
            </p>
            <h2 class="m-0 text-2xl font-bold tracking-tight">Create line item</h2>
            <p class="m-0 text-sm leading-6 text-mnl-subtext">
              Use the same projected form content in sheet, dialog, or viewport-driven auto mode.
            </p>
          </div>
        </div>

        <form class="space-y-4" (submit)="handleSubmit($event)">
          <mnl-form-field inputId="story-panel-title" label="Title" required>
            <mnl-input id="story-panel-title" placeholder="School fees"></mnl-input>
          </mnl-form-field>

          <mnl-form-field inputId="story-panel-amount" label="Planned amount" required>
            <mnl-amount-input id="story-panel-amount" placeholder="0.00"></mnl-amount-input>
          </mnl-form-field>

          <mnl-form-field
            inputId="story-panel-notes"
            label="Notes"
            hint="Projected content stays scrollable when the sheet grows taller than the viewport."
          >
            <mnl-input id="story-panel-notes" placeholder="Optional household context"></mnl-input>
          </mnl-form-field>

          <div class="flex flex-col gap-3 pt-2 sm:flex-row sm:justify-end">
            <mnl-button variant="ghost" (pressed)="open.set(false)">Cancel</mnl-button>
            <mnl-button type="submit">Save item</mnl-button>
          </div>
        </form>
      </mnl-panel>
    </div>
  `,
})
class PanelStoryPreviewComponent {
  readonly mode = input<MnlPanelMode>('auto');
  readonly themeMode = input<'light' | 'dark'>('light');

  protected readonly activeTheme = signal<'light' | 'dark'>('light');
  protected readonly open = signal(true);

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

  protected handleSubmit(event: Event): void {
    event.preventDefault();
    this.open.set(false);
  }

  protected toggleTheme(): void {
    this.activeTheme.update((theme) => (theme === 'light' ? 'dark' : 'light'));
  }
}

const meta: Meta<PanelStoryPreviewComponent> = {
  title: 'Molecules/Panel',
  component: PanelStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
    viewport: {
      options: viewportOptions,
    },
  },
};

export default meta;

type Story = StoryObj<PanelStoryPreviewComponent>;

export const AutoMobile: Story = {
  args: {
    mode: 'auto',
    themeMode: 'light',
  },
  parameters: {
    viewport: {
      defaultViewport: 'mobile390',
    },
  },
};

export const AutoDesktop: Story = {
  args: {
    mode: 'auto',
    themeMode: 'light',
  },
  parameters: {
    viewport: {
      defaultViewport: 'desktop1440',
    },
  },
};

export const ForcedSheet: Story = {
  args: {
    mode: 'sheet',
    themeMode: 'light',
  },
  parameters: {
    viewport: {
      defaultViewport: 'desktop1440',
    },
  },
};

export const ForcedDialog: Story = {
  args: {
    mode: 'dialog',
    themeMode: 'light',
  },
  parameters: {
    viewport: {
      defaultViewport: 'mobile390',
    },
  },
};

export const DarkModeDialog: Story = {
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
