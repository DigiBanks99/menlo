import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';
import {
  ArrowRight,
  Bell,
  Calendar,
  Check,
  CircleAlert,
  CreditCard,
  DollarSign,
  HandCoins,
  Home,
  House,
  Landmark,
  ListTodo,
  LucideAngularModule,
  PiggyBank,
  Receipt,
  Search,
  Settings,
  Target,
  TrendingDown,
  TrendingUp,
  User,
  Users,
  Wallet,
  X,
} from 'lucide-angular';

import { foundationThemes } from './foundation-data';

const iconEntries = [
  { name: 'Home', icon: Home },
  { name: 'House', icon: House },
  { name: 'Wallet', icon: Wallet },
  { name: 'PiggyBank', icon: PiggyBank },
  { name: 'Landmark', icon: Landmark },
  { name: 'DollarSign', icon: DollarSign },
  { name: 'HandCoins', icon: HandCoins },
  { name: 'Receipt', icon: Receipt },
  { name: 'Calendar', icon: Calendar },
  { name: 'Target', icon: Target },
  { name: 'TrendingUp', icon: TrendingUp },
  { name: 'TrendingDown', icon: TrendingDown },
  { name: 'Bell', icon: Bell },
  { name: 'CreditCard', icon: CreditCard },
  { name: 'Search', icon: Search },
  { name: 'Settings', icon: Settings },
  { name: 'User', icon: User },
  { name: 'Users', icon: Users },
  { name: 'ListTodo', icon: ListTodo },
  { name: 'CircleAlert', icon: CircleAlert },
  { name: 'Check', icon: Check },
  { name: 'X', icon: X },
  { name: 'ArrowRight', icon: ArrowRight },
] as const;

@Component({
  selector: 'lib-foundations-icons-story',
  standalone: true,
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Foundations
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Icons</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            Menlo uses Lucide as the shared icon library. Each icon here is documented with the
            symbol name and import path for quick copy/paste into component work.
          </p>
        </section>

        <div class="grid gap-6 xl:grid-cols-2">
          @for (theme of themes; track theme.mode) {
            <section
              [attr.style]="theme.previewStyle"
              class="rounded-2xl p-6 shadow-sm ring-1 ring-mnl-border"
            >
              <div class="flex items-center justify-between gap-4">
                <div>
                  <h2 class="m-0 text-2xl font-semibold">{{ theme.label }}</h2>
                  <p class="mt-2 mb-0 text-sm text-mnl-subtext">
                    Commonly-used Lucide icons for home, budget, and feedback flows.
                  </p>
                </div>

                <span
                  class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                >
                  import &#123; icon &#125; from 'lucide-angular'
                </span>
              </div>

              <div class="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                @for (entry of iconEntries; track entry.name) {
                  <article class="rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                    <div class="flex items-center gap-3">
                      <div class="rounded-xl bg-mnl-surface-alt p-3 text-mnl-accent">
                        <lucide-icon [img]="entry.icon" class="size-5"></lucide-icon>
                      </div>
                      <div>
                        <p class="m-0 text-sm font-semibold">{{ entry.name }}</p>
                        <code class="mt-1 block text-xs text-mnl-subtext">lucide-angular</code>
                      </div>
                    </div>
                  </article>
                }
              </div>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class FoundationsIconsStoryComponent {
  protected readonly themes = foundationThemes;
  protected readonly iconEntries = iconEntries;
}

const meta: Meta<FoundationsIconsStoryComponent> = {
  title: 'Foundations/Icons',
  component: FoundationsIconsStoryComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<FoundationsIconsStoryComponent>;

export const Overview: Story = {};
