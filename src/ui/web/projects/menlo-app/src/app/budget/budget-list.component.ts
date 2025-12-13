import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-budget-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './budget-list/budget-list.component.html',
  styleUrl: './budget-list/budget-list.component.scss'
})
export class BudgetListComponent {
  budgets = signal([
    {
      id: '1',
      name: 'Monthly Household Budget',
      period: 'December 2025',
      spent: 8750,
      total: 12000,
      spentPercentage: 73,
      status: 'good',
      statusIcon: '✅',
      statusText: 'On track'
    },
    {
      id: '2',
      name: 'Holiday Spending',
      period: 'December 2025',
      spent: 2100,
      total: 2500,
      spentPercentage: 84,
      status: 'warning',
      statusIcon: '⚠️',
      statusText: 'Watch spending'
    },
    {
      id: '3',
      name: 'Emergency Fund',
      period: 'December 2025',
      spent: 500,
      total: 5000,
      spentPercentage: 10,
      status: 'good',
      statusIcon: '💰',
      statusText: 'Healthy reserve'
    }
  ]);
}
