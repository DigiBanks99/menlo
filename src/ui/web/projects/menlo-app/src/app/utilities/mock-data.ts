import { ElectricityUsage } from './electricity/electricity-usage/electricity-usage.model';

const electricityUsage: ElectricityUsage[] = [];
const juneTwentySecond = new ElectricityUsage();
juneTwentySecond.date = '2024-06-22T00:00:00Z';
juneTwentySecond.units = 318.16;
juneTwentySecond.usage = 20.0;
electricityUsage.push(juneTwentySecond);

const juneTwentyThird = new ElectricityUsage();
juneTwentyThird.date = '2024-06-23T00:00:00Z';
juneTwentyThird.units = 295.6;
juneTwentyThird.usage = juneTwentySecond.units - juneTwentyThird.units;
electricityUsage.push(juneTwentyThird);

const juneTwentyFourth = new ElectricityUsage();
juneTwentyFourth.date = '2024-06-24T00:00:00Z';
juneTwentyFourth.units = 276.93;
juneTwentyFourth.usage = juneTwentyThird.units - juneTwentyFourth.units;
electricityUsage.push(juneTwentyFourth);

const juneTwentyFifth = new ElectricityUsage();
juneTwentyFifth.date = '2024-06-25T00:00:00Z';
juneTwentyFifth.units = 262.45;
juneTwentyFifth.usage = juneTwentyFourth.units - juneTwentyFifth.units;
electricityUsage.push(juneTwentyFifth);

const juneTwentySixth = new ElectricityUsage();
juneTwentySixth.date = '2024-06-26T00:00:00Z';
juneTwentySixth.units = 238.24;
juneTwentySixth.usage = juneTwentyFifth.units - juneTwentySixth.units;
electricityUsage.push(juneTwentySixth);

const juneTwentySeventh = new ElectricityUsage();
juneTwentySeventh.date = '2024-06-27T00:00:00Z';
juneTwentySeventh.units = 215.22;
juneTwentySeventh.usage = juneTwentySixth.units - juneTwentySeventh.units;
electricityUsage.push(juneTwentySeventh);

const juneTwentyEighth = new ElectricityUsage();
juneTwentyEighth.date = '2024-06-28T00:00:00Z';
juneTwentyEighth.units = 195.21;
juneTwentyEighth.usage = juneTwentySeventh.units - juneTwentyEighth.units;
electricityUsage.push(juneTwentyEighth);

const juneTwentyNinth = new ElectricityUsage();
juneTwentyNinth.date = '2024-06-29T00:00:00Z';
juneTwentyNinth.units = 177.02;
juneTwentyNinth.usage = juneTwentyEighth.units - juneTwentyNinth.units;
electricityUsage.push(juneTwentyNinth);

export const electricityUsageData = electricityUsage;
export const electricityUsageResponseData = electricityUsage.map(usage => ({
    date: usage.date,
    units: usage.units,
    usage: usage.usage,
    applianceUsages: []
}));
