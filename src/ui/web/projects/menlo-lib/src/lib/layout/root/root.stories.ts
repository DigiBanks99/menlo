import { applicationConfig, Meta, StoryObj } from '@storybook/angular';
import { RootComponent } from './root.component';
import { provideRouter } from '@angular/router';

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
            providers: [provideRouter([])]
        })
    ]
};

export default meta;

type Story = StoryObj<RootComponent>;

export const Default: Story = {
    args: {
        navItems: [
            { description: 'Utilities', alternateText: 'Utilities', route: 'utilities', iconName: 'water_ec' },
            { description: 'Budget', alternateText: 'Budget', route: 'budget', iconName: 'account_balance_wallet' }
        ]
    }
};
