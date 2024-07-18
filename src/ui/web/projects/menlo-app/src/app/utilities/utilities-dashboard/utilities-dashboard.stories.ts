import { applicationConfig, Meta, StoryObj } from '@storybook/angular';
import { UtilitiesDashboardComponent } from './utilities-dashboard.component';
import { provideRouter } from '@angular/router';
import { routes } from '@utilities/utilities.routes';

const meta: Meta<UtilitiesDashboardComponent> = {
    title: 'Utilities/Dashboard',
    component: UtilitiesDashboardComponent,
    tags: ['autodocs'],
    render: (args: UtilitiesDashboardComponent) => ({
        props: {
            ...args
        }
    }),
    decorators: [
        applicationConfig({
            providers: [provideRouter(routes)]
        })
    ]
};

export default meta;

type Story = StoryObj<UtilitiesDashboardComponent>;

export const Default: Story = { args: {} };
