import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  MnlBadgeComponent,
  type MnlBadgeVariant,
  MnlButtonComponent,
  MnlCardComponent,
  MnlPageHeaderComponent,
  MnlStatComponent,
  type MnlStatTrend,
} from 'menlo-lib';

import { BudgetWidgetComponent } from './budget-widget.component';

interface HomeFeature {
  readonly badge: string;
  readonly badgeVariant: MnlBadgeVariant;
  readonly description: string;
  readonly title: string;
}

interface HomeOverviewStat {
  readonly label: string;
  readonly trend: MnlStatTrend | null;
  readonly value: string;
}

const homeFeatures: readonly HomeFeature[] = [
  {
    badge: 'Smart budgeting',
    badgeVariant: 'success',
    description:
      "Track household spending with calm, readable summaries that keep the family's priorities in view.",
    title: 'Plan together',
  },
  {
    badge: 'Handwritten lists',
    badgeVariant: 'info',
    description:
      'Capture notes and lists quickly, then bring them back into the shared home-management flow.',
    title: 'Bridge paper and digital',
  },
  {
    badge: 'Privacy first',
    badgeVariant: 'neutral',
    description:
      'Run Menlo on your own home server so sensitive household context stays close to the family.',
    title: 'Keep data local',
  },
];

const overviewStats: readonly HomeOverviewStat[] = [
  {
    label: 'Total budget',
    trend: { direction: 'up', value: '+4.8% vs last month', variant: 'success' },
    value: 'R 12 000',
  },
  {
    label: 'Spent this month',
    trend: { direction: 'neutral', value: 'Healthy pace', variant: 'neutral' },
    value: 'R 7 650',
  },
  {
    label: 'Remaining',
    trend: { direction: 'up', value: 'R 4 350 left', variant: 'success' },
    value: '36%',
  },
];

@Component({
  selector: 'app-home',
  imports: [
    BudgetWidgetComponent,
    MnlBadgeComponent,
    MnlButtonComponent,
    MnlCardComponent,
    MnlPageHeaderComponent,
    MnlStatComponent,
  ],
  templateUrl: './home.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeComponent {
  private readonly router = inject(Router);

  protected readonly features = homeFeatures;
  protected readonly overviewStats = overviewStats;

  protected navigateTo(path: '/analytics' | '/budgets'): void {
    void this.router.navigateByUrl(path);
  }
}
