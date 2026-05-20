import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';
import {
  BellDot,
  ChevronRight,
  CreditCard,
  FolderTree,
  LucideAngularModule,
  ReceiptText,
} from 'lucide-angular';

import { MnlAvatarComponent } from '../../atoms/avatar';
import { MnlBadgeComponent } from '../../atoms/badge';
import { MnlButtonComponent } from '../../atoms/button';
import { foundationThemes } from '../../foundations/foundation-data';
import { MnlListItemComponent } from './list-item.component';

@Component({
  selector: 'lib-list-item-story-preview',
  standalone: true,
  imports: [
    LucideAngularModule,
    MnlAvatarComponent,
    MnlBadgeComponent,
    MnlButtonComponent,
    MnlListItemComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Molecules
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">List Item</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-list-item standardises icon-or-avatar list rows so categories, settings, and
            transactions line up with a shared hover and selection treatment.
          </p>
        </section>

        <div class="grid gap-6 xl:grid-cols-2">
          @for (theme of themes; track theme.mode) {
            <section
              [attr.style]="theme.previewStyle"
              class="space-y-6 rounded-2xl p-6 shadow-sm ring-1 ring-mnl-border"
            >
              <div class="flex items-center justify-between gap-4">
                <div>
                  <h2 class="m-0 text-2xl font-semibold">{{ theme.label }}</h2>
                  <p class="mt-2 mb-0 text-sm text-mnl-subtext">
                    Rows stay readable whether they carry icons, avatars, badges, or lightweight
                    trailing actions.
                  </p>
                </div>

                <span
                  class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                >
                  {{ theme.mode }}
                </span>
              </div>

              <article class="rounded-2xl bg-mnl-surface shadow-sm ring-1 ring-mnl-border">
                <mnl-list-item [interactive]="true">
                  <lucide-icon
                    [img]="folderTreeIcon"
                    class="size-5"
                    mnlListItemLeading
                  ></lucide-icon>
                  <div class="space-y-1">
                    <p class="m-0 text-sm font-semibold text-mnl-text">Budget categories</p>
                    <p class="m-0 text-sm text-mnl-subtext">
                      Keep groceries, utilities, and fuel tidy.
                    </p>
                  </div>
                  <div class="flex items-center gap-2" mnlListItemTrailing>
                    <mnl-badge size="sm" variant="info">12 groups</mnl-badge>
                    <lucide-icon [img]="chevronRightIcon" class="size-4"></lucide-icon>
                  </div>
                </mnl-list-item>

                <mnl-list-item [href]="'#transaction-1'" [selected]="true">
                  <lucide-icon [img]="receiptIcon" class="size-5" mnlListItemLeading></lucide-icon>
                  <div class="space-y-1">
                    <p class="m-0 text-sm font-semibold text-mnl-text">Card payment</p>
                    <p class="m-0 text-sm text-mnl-subtext">Pick n Pay · Approved 2 minutes ago</p>
                  </div>
                  <div class="flex items-center gap-2" mnlListItemTrailing>
                    <span class="text-sm font-semibold text-mnl-text">R 842</span>
                    <mnl-badge size="sm" variant="success">Synced</mnl-badge>
                  </div>
                </mnl-list-item>

                <mnl-list-item>
                  <mnl-avatar fallback="Noluthando B" size="md" mnlListItemLeading></mnl-avatar>
                  <div class="space-y-1">
                    <p class="m-0 text-sm font-semibold text-mnl-text">Notifications</p>
                    <p class="m-0 text-sm text-mnl-subtext">
                      Budget alerts and weekly recap emails.
                    </p>
                  </div>
                  <div class="flex items-center gap-2" mnlListItemTrailing>
                    <mnl-button size="sm" variant="secondary">Manage</mnl-button>
                  </div>
                </mnl-list-item>
              </article>

              <article class="rounded-2xl bg-mnl-surface p-4 shadow-sm ring-1 ring-mnl-border">
                <h3 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  Compact compositions
                </h3>

                <div class="mt-4 rounded-2xl bg-mnl-bg/60 shadow-inner ring-1 ring-mnl-border/50">
                  <mnl-list-item [interactive]="true">
                    <lucide-icon
                      [img]="creditCardIcon"
                      class="size-5"
                      mnlListItemLeading
                    ></lucide-icon>
                    <div class="space-y-1">
                      <p class="m-0 text-sm font-semibold text-mnl-text">Debit card ending 4002</p>
                      <p class="m-0 text-sm text-mnl-subtext">
                        Tap to update the repayment account.
                      </p>
                    </div>
                    <lucide-icon
                      [img]="chevronRightIcon"
                      class="size-4"
                      mnlListItemTrailing
                    ></lucide-icon>
                  </mnl-list-item>

                  <mnl-list-item [interactive]="true">
                    <lucide-icon [img]="bellIcon" class="size-5" mnlListItemLeading></lucide-icon>
                    <div class="space-y-1">
                      <p class="m-0 text-sm font-semibold text-mnl-text">
                        Spending threshold alert
                      </p>
                      <p class="m-0 text-sm text-mnl-subtext">Warn me once groceries pass 85%.</p>
                    </div>
                    <mnl-badge leadingDot size="sm" variant="warning" mnlListItemTrailing>
                      Active
                    </mnl-badge>
                  </mnl-list-item>
                </div>
              </article>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class ListItemStoryPreviewComponent {
  protected readonly bellIcon = BellDot;
  protected readonly chevronRightIcon = ChevronRight;
  protected readonly creditCardIcon = CreditCard;
  protected readonly folderTreeIcon = FolderTree;
  protected readonly receiptIcon = ReceiptText;
  protected readonly themes = foundationThemes;
}

const meta: Meta<ListItemStoryPreviewComponent> = {
  title: 'Molecules/List Item',
  component: ListItemStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<ListItemStoryPreviewComponent>;

export const Overview: Story = {};
