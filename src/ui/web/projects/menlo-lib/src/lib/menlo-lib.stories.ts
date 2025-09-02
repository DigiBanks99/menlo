import type { Meta, StoryObj } from '@storybook/angular';
import { MenloLib } from './menlo-lib';

const meta: Meta<MenloLib> = {
  title: 'Menlo Lib/MenloLib',
  component: MenloLib,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
  argTypes: {},
};

export default meta;
type Story = StoryObj<MenloLib>;

export const Default: Story = {
  args: {},
};
