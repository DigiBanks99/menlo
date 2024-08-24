import { componentWrapperDecorator, Meta, StoryObj } from '@storybook/angular';
import { LoadingComponent } from './loading.component';
import { LoadKind } from './load-kind.enum';

const meta: Meta<LoadingComponent> = {
    title: 'Common/Loading',
    component: LoadingComponent,
    tags: ['autodocs'],
    render: args => ({
        props: args
    })
};

export default meta;

type Story = StoryObj<LoadingComponent>;

export const Default: Story = { args: {} };

export const Small: Story = { args: { kind: LoadKind.Small } };

export const Contained: Story = {
    args: { ...Default.args },
    decorators: [
        componentWrapperDecorator(
            story => `
            <style>
                .card-body {
                    height: 300px;
                }
            </style>
            <div class="card">
                <div class="card-header">
                    <h5 class="card-title">Loading</h5>
                </div>
                <div class="card-body">
                    ${story}
                </div>
            </div>`
        )
    ]
};

export const ContainedSmaller: Story = {
    args: { ...Default.args },
    decorators: [
        componentWrapperDecorator(
            story => `
            <style>
                .card-body {
                    height: 150px;
                }
            </style>
            <div class="card">
                <div class="card-header">
                    <h5 class="card-title">Loading</h5>
                </div>
                <div class="card-body">
                    ${story}
                </div>
            </div>`
        )
    ]
};

export const ContainedXL: Story = {
    args: { ...Default.args },
    decorators: [
        componentWrapperDecorator(
            story => `
            <style>
                .card-body {
                    height: 500px;
                }
            </style>
            <div class="card">
                <div class="card-header">
                    <h5 class="card-title">Loading</h5>
                </div>
                <div class="card-body">
                    ${story}
                </div>
            </div>`
        )
    ]
};

export const Button: Story = {
    args: { ...Default.args, kind: LoadKind.Small },
    decorators: [
        componentWrapperDecorator(
            story => `
            <button class="btn btn-primary d-flex justify-content-evenly" disabled>
                    ${story}
                    Submit
            </button>`
        )
    ]
};
