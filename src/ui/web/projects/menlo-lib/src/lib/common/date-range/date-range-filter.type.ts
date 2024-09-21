export const DateRangeFilterUnit = {
    Minutes: 'minutes',
    Hours: 'hours',
    Days: 'days',
    Weeks: 'weeks',
    Months: 'months',
    Years: 'years'
} as const;
export type DateRangeFilterUnit = (typeof DateRangeFilterUnit)[keyof typeof DateRangeFilterUnit];

export class DateRangeFilter {
    unit: DateRangeFilterUnit;
    value: number;

    constructor(unit: DateRangeFilterUnit, value: number) {
        this.unit = unit;
        this.value = value;
    }
}
