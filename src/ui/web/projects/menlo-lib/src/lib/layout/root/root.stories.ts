import { applicationConfig, Meta, StoryObj } from '@storybook/angular';
import { RootComponent } from './root.component';
import { provideRouter } from '@angular/router';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LorumIpsum } from '../../story.constants';
import { provideLocationMocks } from '@angular/common/testing';

@Component({
    template: `<p>{{ lorumIpsum }}</p>`,
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush
})
class LorumIpsumComponent {
    public lorumIpsum: string = LorumIpsum;
}

@Component({
    standalone: true,
    template: `<h1>Utilities</h1> <p>It works!</>`,
    changeDetection: ChangeDetectionStrategy.OnPush
})
class UtilitiesComponent {
    public lorumIpsum: string = LorumIpsum;
}

@Component({
    standalone: true,
    template: `<h1>Budget</h1> <p>It works!</>`,
    changeDetection: ChangeDetectionStrategy.OnPush
})
class BudgetComponent {
    public lorumIpsum: string = LorumIpsum;
}

const meta: Meta<RootComponent> = {
    title: 'Layout/Root',
    component: RootComponent,
    tags: ['autodocs'],
    render: (args: RootComponent) => ({
        props: {
            ...args
        }
    }),
    decorators: [
        applicationConfig({
            providers: [
                provideRouter([
                    { path: '', pathMatch: 'full', redirectTo: 'lorum-ipsum' },
                    { path: 'lorum-ipsum', component: LorumIpsumComponent },
                    { path: 'utilities', component: UtilitiesComponent },
                    { path: 'budget', component: BudgetComponent }
                ]),
                provideLocationMocks()
            ]
        })
    ]
};

export default meta;

type Story = StoryObj<RootComponent>;

export const Default: Story = {
    args: {
        navItems: [
            { description: 'Lorum Ipsum', alternateText: 'Lorum Ipsum', route: 'lorum-ipsum', iconName: 'article' },
            { description: 'Utilities', alternateText: 'Utilities', route: 'utilities', iconName: 'water_ec' },
            { description: 'Budget', alternateText: 'Budget', route: 'budget', iconName: 'account_balance_wallet' }
        ]
    }
};
