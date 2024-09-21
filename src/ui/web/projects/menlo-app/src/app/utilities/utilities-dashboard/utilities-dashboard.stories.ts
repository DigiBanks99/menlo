import { applicationConfig, Meta, StoryObj } from '@storybook/angular';
import { UtilitiesDashboardComponent } from './utilities-dashboard.component';
import { provideRouter } from '@angular/router';
import { routes } from '@utilities/utilities.routes';
import { provideUtilitiesServiceTesting } from '@utilities/utilities.service';
import { electricityUsageResponseData } from '@utilities/mock-data';

const meta: Meta<UtilitiesDashboardComponent> = {
    title: 'Utilities/Dashboard',
    component: UtilitiesDashboardComponent,
    tags: ['autodocs'],
    render: args => ({
        props: args
    }),
    decorators: [
        applicationConfig({
            providers: [provideRouter(routes), provideUtilitiesServiceTesting({ loading: false, electricityUsage: electricityUsageResponseData })]
        })
    ]
};

export default meta;

type Story = StoryObj<UtilitiesDashboardComponent>;

export const Default: Story = { args: {} };

export const Loading: Story = {
    args: {},
    decorators: [
        applicationConfig({
            providers: [provideUtilitiesServiceTesting({ loading: true })]
        })
    ]
};
