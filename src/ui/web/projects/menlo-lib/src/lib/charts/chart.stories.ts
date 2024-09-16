import { componentWrapperDecorator, Meta, StoryObj } from '@storybook/angular';
import { ChartComponent } from './chart.component';
import { ScaleOptionsByType, CartesianScaleTypeRegistry, ScaleTypeRegistry, ScaleChartOptions, ChartType, ChartTypeRegistry } from 'chart.js';
import { MenloChartLinearScale } from './chart.types';

const meta: Meta<ChartComponent> = {
    title: 'Charts/Chart',
    component: ChartComponent,
    tags: ['autodocs'],
    render: args => ({
        props: args
    })
};

export default meta;

type Story = StoryObj<ChartComponent>;

const linearScale: MenloChartLinearScale = {
    x: {
        beginAtZero: true,
        title: {
            display: true,
            text: 'Date'
        }
    },
    y: {
        beginAtZero: true,
        title: {
            display: true,
            text: 'Units'
        }
    }
};

export const Default: Story = {
    args: {
        data: {
            datasets: []
        },
        type: 'line'
    }
};

export const Line: Story = {
    args: {
        data: {
            datasets: [
                {
                    label: 'Dataset 1',
                    data: [10, 20, 30, 40, 50]
                }
            ],
            labels: ['January', 'February', 'March', 'April', 'May']
        },
        title: 'Chart Title',
        type: 'line',
        scales: linearScale
    }
};
