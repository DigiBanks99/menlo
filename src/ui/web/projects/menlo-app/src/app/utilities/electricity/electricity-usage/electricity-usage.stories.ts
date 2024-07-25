import { Meta, StoryObj } from '@storybook/angular';
import { ElectricityUsageComponent } from './electricity-usage.component';

const meta: Meta<ElectricityUsageComponent> = {
    title: 'Utilities/Electricity/Usage',
    component: ElectricityUsageComponent,
    tags: ['autodocs'],
    render: args => ({
        props: args
    })
};

export default meta;

type Story = StoryObj<ElectricityUsageComponent>;

export const Default: Story = { args: {} };

export const WithData: Story = {
    args: {
        electricityUsage: [
            {
                date: '2024-07-23T00:00:00Z',
                units: 359.62,
                applianceUsage: []
            },
            {
                date: '2024-07-24T00:00:00Z',
                units: 336.99,
                applianceUsage: []
            },
            {
                date: '2024-07-25T00:00:00Z',
                units: 307.24,
                applianceUsage: []
            }
        ]
    }
};
