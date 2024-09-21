import { Meta, StoryObj } from '@storybook/angular';
import { ElectricityUsageComponent } from './electricity-usage.component';
import { ElectricityUsage } from './electricity-usage.model';
import { electricityUsageData } from '@utilities/mock-data';

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

export const Default: Story = {
    args: {
        electricityUsage: [] as ElectricityUsage[],
        loading: false
    }
};

export const Loading: Story = {
    args: {
        electricityUsage: [] as ElectricityUsage[],
        loading: true
    }
};

export const WithData: Story = {
    args: {
        electricityUsage: electricityUsageData,
        loading: false
    }
};
