import { Meta, StoryObj } from '@storybook/angular';
import { ElectricityUsageComponent } from './electricity-usage.component';
import { ElectricityUsage } from './electricity-usage.model';

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
        electricityUsage: [] as ElectricityUsage[]
    }
};

const electricityUsage: ElectricityUsage[] = [];
const juneTwentySecond = new ElectricityUsage();
juneTwentySecond.date = '2024-06-22T00:00:00Z';
juneTwentySecond.units = 318.16;
electricityUsage.push(juneTwentySecond);

const juneTwentyThird = new ElectricityUsage();
juneTwentyThird.date = '2024-06-23T00:00:00Z';
juneTwentyThird.units = 295.6;
electricityUsage.push(juneTwentyThird);

const juneTwentyFourth = new ElectricityUsage();
juneTwentyFourth.date = '2024-06-24T00:00:00Z';
juneTwentyFourth.units = 276.93;
electricityUsage.push(juneTwentyFourth);

const juneTwentyFifth = new ElectricityUsage();
juneTwentyFifth.date = '2024-06-25T00:00:00Z';
juneTwentyFifth.units = 262.45;
electricityUsage.push(juneTwentyFifth);

const juneTwentySixth = new ElectricityUsage();
juneTwentySixth.date = '2024-06-26T00:00:00Z';
juneTwentySixth.units = 238.24;
electricityUsage.push(juneTwentySixth);

const juneTwentySeventh = new ElectricityUsage();
juneTwentySeventh.date = '2024-06-27T00:00:00Z';
juneTwentySeventh.units = 215.22;
electricityUsage.push(juneTwentySeventh);

const juneTwentyEighth = new ElectricityUsage();
juneTwentyEighth.date = '2024-06-28T00:00:00Z';
juneTwentyEighth.units = 195.21;
electricityUsage.push(juneTwentyEighth);

const juneTwentyNinth = new ElectricityUsage();
juneTwentyNinth.date = '2024-06-29T00:00:00Z';
juneTwentyNinth.units = 177.02;
electricityUsage.push(juneTwentyNinth);

export const WithData: Story = {
    args: {
        electricityUsage
    }
};
