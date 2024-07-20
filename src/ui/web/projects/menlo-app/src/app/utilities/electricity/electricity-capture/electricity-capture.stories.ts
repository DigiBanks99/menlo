import { applicationConfig, Meta, StoryObj } from '@storybook/angular';
import { ElectricityCaptureComponent } from './electricity-capture.component';

const meta: Meta<ElectricityCaptureComponent> = {
    title: 'Utilities/Electricity/Capture',
    component: ElectricityCaptureComponent,
    tags: ['autodocs'],
    render: (args: ElectricityCaptureComponent) => ({
        props: {
            ...args
        }
    })
};

export default meta;

type Story = StoryObj<ElectricityCaptureComponent>;

export const Default: Story = { args: {} };
