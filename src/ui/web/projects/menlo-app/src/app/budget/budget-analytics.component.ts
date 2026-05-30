import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  MnlBadgeComponent,
  type MnlBadgeVariant,
  MnlButtonComponent,
  MnlCardComponent,
  MnlListItemComponent,
  MnlPageHeaderComponent,
  MnlProgressComponent,
  type MnlProgressVariant,
  MnlSelectComponent,
  type MnlSelectOption,
  MnlStatComponent,
  type MnlStatTrend,
} from 'menlo-lib';

type MetricType = 'primary' | 'warning' | 'success';
type MetricChangeType = 'positive' | 'negative' | 'neutral';
type InsightType = 'warning' | 'success' | 'tip';
type BudgetAnalyticsPeriod = 'current' | 'last' | 'quarter' | 'year';

interface BudgetAnalyticsMetric {
  readonly change: string;
  readonly changeType: MetricChangeType;
  readonly icon: string;
  readonly id: string;
  readonly title: string;
  readonly type: MetricType;
  readonly value: string;
}

interface BudgetAnalyticsCategory {
  readonly amount: number;
  readonly icon: string;
  readonly id: string;
  readonly name: string;
  readonly percentage: number;
}

interface BudgetAnalyticsInsight {
  readonly action: string | null;
  readonly description: string;
  readonly icon: string;
  readonly id: string;
  readonly title: string;
  readonly type: InsightType;
}

@Component({
  selector: 'app-budget-analytics',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MnlBadgeComponent,
    MnlButtonComponent,
    MnlCardComponent,
    MnlListItemComponent,
    MnlPageHeaderComponent,
    MnlProgressComponent,
    MnlSelectComponent,
    MnlStatComponent,
  ],
  templateUrl: './budget-analytics/budget-analytics.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BudgetAnalyticsComponent {
  readonly summaryMetrics = signal<readonly BudgetAnalyticsMetric[]>([
    {
      id: '1',
      title: 'Total Budget',
      value: 'R 15,500',
      icon: '💰',
      type: 'primary',
      change: '+5.2% from last month',
      changeType: 'positive',
    },
    {
      id: '2',
      title: 'Spent This Month',
      value: 'R 11,350',
      icon: '💸',
      type: 'warning',
      change: '73% of budget used',
      changeType: 'neutral',
    },
    {
      id: '3',
      title: 'Remaining',
      value: 'R 4,150',
      icon: '🎯',
      type: 'success',
      change: '12 days left',
      changeType: 'positive',
    },
    {
      id: '4',
      title: 'Savings Rate',
      value: '23%',
      icon: '📈',
      type: 'success',
      change: '+2% vs target',
      changeType: 'positive',
    },
  ]);

  readonly categories = signal<readonly BudgetAnalyticsCategory[]>([
    { id: '1', name: 'Groceries', amount: 3200, percentage: 85, icon: '🛒' },
    { id: '2', name: 'Utilities', amount: 1800, percentage: 75, icon: '💡' },
    { id: '3', name: 'Transport', amount: 2100, percentage: 70, icon: '🚗' },
    { id: '4', name: 'Entertainment', amount: 850, percentage: 42, icon: '🎬' },
    { id: '5', name: 'Healthcare', amount: 1200, percentage: 60, icon: '🏥' },
    { id: '6', name: 'Shopping', amount: 2200, percentage: 88, icon: '🛍️' },
  ]);

  readonly aiInsights = signal<readonly BudgetAnalyticsInsight[]>([
    {
      id: '1',
      title: 'Grocery Spending Alert',
      description:
        "You're spending 15% more on groceries than usual. Consider meal planning to reduce costs.",
      icon: '⚠️',
      type: 'warning',
      action: 'View Suggestions',
    },
    {
      id: '2',
      title: 'Great Savings Progress',
      description:
        "You're ahead of your savings goal this month! Consider allocating extra funds to your emergency fund.",
      icon: '🎉',
      type: 'success',
      action: 'Adjust Budget',
    },
    {
      id: '3',
      title: 'Upcoming Bill Reminder',
      description:
        'Your electricity bill is typically due in 5 days. Budget shows you have sufficient funds allocated.',
      icon: '💡',
      type: 'tip',
      action: null,
    },
  ]);

  protected readonly periodOptions: readonly MnlSelectOption[] = [
    { value: 'current', label: 'Current Month' },
    { value: 'last', label: 'Last Month' },
    { value: 'quarter', label: 'This Quarter' },
    { value: 'year', label: 'This Year' },
  ];
  protected readonly selectedPeriod = signal<BudgetAnalyticsPeriod>('current');
  protected readonly selectedPeriodLabel = computed(
    () =>
      this.periodOptions.find((option) => option.value === this.selectedPeriod())?.label ?? 'Current Month',
  );

  protected categoryProgressVariantFor(percentage: number): MnlProgressVariant {
    if (percentage >= 95) {
      return 'error';
    }

    if (percentage >= 85) {
      return 'warning';
    }

    return 'success';
  }

  protected insightLabelFor(type: InsightType): string {
    switch (type) {
      case 'warning':
        return 'Warning';
      case 'success':
        return 'Success';
      default:
        return 'Tip';
    }
  }

  protected insightVariantFor(type: InsightType): MnlBadgeVariant {
    switch (type) {
      case 'warning':
        return 'warning';
      case 'success':
        return 'success';
      default:
        return 'info';
    }
  }

  protected metricBadgeVariantFor(type: MetricType): MnlBadgeVariant {
    switch (type) {
      case 'warning':
        return 'warning';
      case 'success':
        return 'success';
      default:
        return 'info';
    }
  }

  protected metricTrendFor(metric: BudgetAnalyticsMetric): MnlStatTrend {
    switch (metric.changeType) {
      case 'negative':
        return {
          direction: 'down',
          value: metric.change,
          variant: 'error',
        };
      case 'neutral':
        return {
          direction: 'neutral',
          value: metric.change,
          variant: 'neutral',
        };
      default:
        return {
          direction: 'up',
          value: metric.change,
          variant: 'success',
        };
    }
  }

  protected updateSelectedPeriod(period: string | null): void {
    if (
      period === 'current' ||
      period === 'last' ||
      period === 'quarter' ||
      period === 'year'
    ) {
      this.selectedPeriod.set(period);
      return;
    }

    this.selectedPeriod.set('current');
  }
}
