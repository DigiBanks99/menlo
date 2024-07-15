import { Meta, StoryObj } from '@storybook/angular';
import { RootComponent } from './root.component';

const meta: Meta<RootComponent> = {
    title: 'Layout/Root',
    component: RootComponent,
    tags: ['autodocs'],
    render: (args: RootComponent) => ({
        props: {
            ...args
        }
    })
};

export default meta;

type Story = StoryObj<RootComponent>;

export const Default: Story = {
    args: {
        navItems: [
            {
                description: 'Utilities',
                alternateText: 'Utilities',
                route: 'utilities'
            },
            { description: 'Budget', alternateText: 'Budget', route: 'budget' }
        ]
    }
};
