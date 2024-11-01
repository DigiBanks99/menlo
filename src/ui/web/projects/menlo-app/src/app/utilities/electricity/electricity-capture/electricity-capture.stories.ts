import { applicationConfig, Meta, StoryObj } from '@storybook/angular';
import { ElectricityCaptureComponent } from './electricity-capture.component';
import { provideUtilitiesServiceTesting } from '@utilities/utilities.service';

const meta: Meta<ElectricityCaptureComponent> = {
    title: 'Utilities/Electricity/Capture',
    component: ElectricityCaptureComponent,
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

type Story = StoryObj<ElectricityCaptureComponent>;

export const Default: Story = { args: {} };

export const Loading: Story = {
    args: {},
    decorators: [
        applicationConfig({
            providers: [provideUtilitiesServiceTesting({ loading: true })]
        })
    ]
};
