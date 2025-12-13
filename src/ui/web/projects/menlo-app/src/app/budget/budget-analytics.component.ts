import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-budget-analytics',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './budget-analytics/budget-analytics.component.html',
  styleUrl: './budget-analytics/budget-analytics.component.scss'
})
export class BudgetAnalyticsComponent {
  summaryMetrics = signal([
    {
      id: '1',
      title: 'Total Budget',
      value: 'R 15,500',
      icon: '💰',
      type: 'primary',
      change: '+5.2% from last month',
      changeType: 'positive'
    },
    {
      id: '2',
      title: 'Spent This Month',
      value: 'R 11,350',
      icon: '💸',
      type: 'warning',
      change: '73% of budget used',
      changeType: 'neutral'
    },
    {
      id: '3',
      title: 'Remaining',
      value: 'R 4,150',
      icon: '🎯',
      type: 'success',
      change: '12 days left',
      changeType: 'positive'
    },
    {
      id: '4',
      title: 'Savings Rate',
      value: '23%',
      icon: '📈',
      type: 'success',
      change: '+2% vs target',
      changeType: 'positive'
    }
  ]);

  categories = signal([
    { id: '1', name: 'Groceries', amount: 3200, percentage: 85, icon: '🛒' },
    { id: '2', name: 'Utilities', amount: 1800, percentage: 75, icon: '💡' },
    { id: '3', name: 'Transport', amount: 2100, percentage: 70, icon: '🚗' },
    { id: '4', name: 'Entertainment', amount: 850, percentage: 42, icon: '🎬' },
    { id: '5', name: 'Healthcare', amount: 1200, percentage: 60, icon: '🏥' },
    { id: '6', name: 'Shopping', amount: 2200, percentage: 88, icon: '🛍️' }
  ]);

  aiInsights = signal([
    {
      id: '1',
      title: 'Grocery Spending Alert',
      description: 'You\'re spending 15% more on groceries than usual. Consider meal planning to reduce costs.',
      icon: '⚠️',
      type: 'warning',
      action: 'View Suggestions'
    },
    {
      id: '2',
      title: 'Great Savings Progress',
      description: 'You\'re ahead of your savings goal this month! Consider allocating extra funds to your emergency fund.',
      icon: '🎉',
      type: 'success',
      action: 'Adjust Budget'
    },
    {
      id: '3',
      title: 'Upcoming Bill Reminder',
      description: 'Your electricity bill is typically due in 5 days. Budget shows you have sufficient funds allocated.',
      icon: '💡',
      type: 'tip',
      action: null
    }
  ]);
}
