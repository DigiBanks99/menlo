import { Meta, StoryObj } from '@storybook/angular';
import { HomeComponent } from './home.component';

const meta: Meta<HomeComponent> = {
    title: 'Home',
    component: HomeComponent,
    tags: ['autodocs'],
    render: (args: HomeComponent) => ({
        props: {
            ...args
        }
    })
};

export default meta;

type Story = StoryObj<HomeComponent>;

export const Default: Story = { args: {} };
