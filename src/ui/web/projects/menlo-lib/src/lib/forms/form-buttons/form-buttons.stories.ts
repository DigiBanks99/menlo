import { action } from '@storybook/addon-actions';
import { Meta, StoryObj } from '@storybook/angular';
import { FormButtonsComponent } from './form-buttons.component';

const meta: Meta<FormButtonsComponent> = {
    title: 'Forms/Buttons',
    component: FormButtonsComponent,
    tags: ['autodocs'],
    argTypes: {
        cancel: { action: 'cancel' },
        submit: { action: 'submit' }
    },
    args: {
        cancel: action('cancel'),
        submit: action('submit')
    }
};

export default meta;

type Story = StoryObj<FormButtonsComponent>;

export const Default: Story = { args: {} };
