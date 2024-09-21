import { DateFormat } from '../../types/date-or-string.type';
import { DateRangeFilter, DateRangeFilterUnit } from './date-range-filter.type';
import { DateRangeService } from './date-range.service';

class Scenario {
    unit: DateRangeFilterUnit;
    value: number;
    expectedDate: string;
    format: DateFormat;

    constructor(unit: DateRangeFilterUnit, value: number, expectedDate: string, format: DateFormat = DateFormat.DateOnly) {
        this.unit = unit;
        this.value = value;
        this.expectedDate = expectedDate;
        this.format = format;
    }
}

describe('DateRangeService', () => {
    const service = new DateRangeService();
    const snapshotDate = new Date('2024-09-21T00:00:00Z');

    describe('getPriorDate', () => {
        const scenarios: Scenario[] = [
            new Scenario(DateRangeFilterUnit.Minutes, 30, '2024-09-20T23:30:00Z', DateFormat.ISO8601),
            new Scenario(DateRangeFilterUnit.Minutes, 30, '2024-09-21'),
            new Scenario(DateRangeFilterUnit.Hours, 2, '2024-09-20T22:00:00Z', DateFormat.ISO8601),
            new Scenario(DateRangeFilterUnit.Hours, 2, '2024-09-21'),
            new Scenario(DateRangeFilterUnit.Days, 7, '2024-09-14'),
            new Scenario(DateRangeFilterUnit.Weeks, 1, '2024-09-14'),
            new Scenario(DateRangeFilterUnit.Months, 1, '2024-08-21'),
            new Scenario(DateRangeFilterUnit.Years, 4, '2020-09-21')
        ];

        scenarios.forEach(scenario => {
            it(`should return a date that is ${scenario.value} ${scenario.unit} prior to the snapshot date when formatted as ${scenario.format}`, () => {
                const filter = { unit: scenario.unit, value: scenario.value } as DateRangeFilter;
                const result = service.getPriorDate(snapshotDate, filter, scenario.format);
                expect(result).toEqual(scenario.expectedDate);
            });
        });
    });

    describe('getFutureDate', () => {
        const scenarios: Scenario[] = [
            new Scenario(DateRangeFilterUnit.Minutes, 30, '2024-09-21T00:30:00Z', DateFormat.ISO8601),
            new Scenario(DateRangeFilterUnit.Minutes, 30, '2024-09-21'),
            new Scenario(DateRangeFilterUnit.Hours, 2, '2024-09-21T02:00:00Z', DateFormat.ISO8601),
            new Scenario(DateRangeFilterUnit.Hours, 2, '2024-09-21'),
            new Scenario(DateRangeFilterUnit.Days, 7, '2024-09-28'),
            new Scenario(DateRangeFilterUnit.Weeks, 1, '2024-09-28'),
            new Scenario(DateRangeFilterUnit.Months, 1, '2024-10-21'),
            new Scenario(DateRangeFilterUnit.Years, 4, '2028-09-21')
        ];

        scenarios.forEach(scenario => {
            it(`should return a date that is ${scenario.value} ${scenario.unit} after the snapshot date when formatted as ${scenario.format}`, () => {
                const filter = { unit: scenario.unit, value: scenario.value } as DateRangeFilter;
                const result = service.getFutureDate(snapshotDate, filter, scenario.format);
                console.log(`Expecting ${scenario.expectedDate}, got ${result}`);
                expect(result).toEqual(scenario.expectedDate);
            });
        });
    });
});
