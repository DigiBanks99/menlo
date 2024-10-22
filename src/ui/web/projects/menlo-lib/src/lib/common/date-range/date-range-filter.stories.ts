import { componentWrapperDecorator, Meta, moduleMetadata, StoryObj } from '@storybook/angular';
import { DateRangeFilterComponent } from './date-range-filter.component';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DateRangeFilterUnit } from './date-range-filter.type';

const meta: Meta<DateRangeFilterComponent> = {
    title: 'Common/Date Range Filter',
    component: DateRangeFilterComponent,
    tags: ['autodocs'],
    render: args => ({
        props: args
    })
};

export default meta;

type HostComponent = {
    value: number | null;
    unit: DateRangeFilterUnit | null;
    label?: string | undefined;
};
type Story = StoryObj<DateRangeFilterComponent & HostComponent>;

export const Default: Story = {
    args: { unit: null, value: null, label: undefined },
    decorators: [moduleMetadata({ imports: [ReactiveFormsModule] })],
    render: args => ({
        props: {
            label: args.label,
            form: new FormGroup({
                filter: new FormControl({ unit: args.unit, value: args.value })
            })
        },
        template: `<form [formGroup]="form">
            <menlo-date-range-filter formControlName="filter" [label]="label"></menlo-date-range-filter>
        </form>`
    })
};

export const WithLabel: Story = {
    ...Default,
    args: { ...Default.args, label: 'Date Range' }
};

export const SevenDays: Story = {
    ...Default,
    args: { ...Default.args, value: 7, unit: DateRangeFilterUnit.Days }
};

export const OneWeek: Story = {
    ...Default,
    args: { ...Default.args, value: 1, unit: DateRangeFilterUnit.Weeks }
};

export const TwoHours: Story = {
    ...Default,
    args: { ...Default.args, value: 2, unit: DateRangeFilterUnit.Hours }
};
