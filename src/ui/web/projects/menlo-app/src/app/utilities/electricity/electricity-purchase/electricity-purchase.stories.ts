import { applicationConfig, Meta, StoryObj } from '@storybook/angular';
import { ElectricityPurchaseComponent } from './electricity-purchase.component';
import { provideUtilitiesServiceTesting } from '@utilities/utilities.service';

const meta: Meta<ElectricityPurchaseComponent> = {
    title: 'Utilities/Electricity/Purchase',
    component: ElectricityPurchaseComponent,
    tags: ['autodocs'],
    render: args => ({
        props: args
    }),
    decorators: [
        applicationConfig({
            providers: [provideUtilitiesServiceTesting()]
        })
    ]
};

export default meta;

type Story = StoryObj<ElectricityPurchaseComponent>;

export const Default: Story = { args: {} };

export const Loading: Story = {
    args: {},
    decorators: [
        applicationConfig({
            providers: [provideUtilitiesServiceTesting({ loading: true })]
        })
    ]
};
