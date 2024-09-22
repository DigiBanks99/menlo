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

    describe('getDuration', () => {
        it('should return the duration string for minutes', () => {
            const filter = { unit: DateRangeFilterUnit.Minutes, value: 30 } as DateRangeFilter;
            const result = service.getDuration(filter);
            expect(result).toEqual('PT30M');
        });

        it('should return the duration string for hours', () => {
            const filter = { unit: DateRangeFilterUnit.Hours, value: 2 } as DateRangeFilter;
            const result = service.getDuration(filter);
            expect(result).toEqual('PT2H');
        });

        it('should return the duration string for days', () => {
            const filter = { unit: DateRangeFilterUnit.Days, value: 7 } as DateRangeFilter;
            const result = service.getDuration(filter);
            expect(result).toEqual('P7D');
        });

        it('should return the duration string for weeks', () => {
            const filter = { unit: DateRangeFilterUnit.Weeks, value: 1 } as DateRangeFilter;
            const result = service.getDuration(filter);
            expect(result).toEqual('P7D');
        });

        it('should return the duration string for months', () => {
            const filter = { unit: DateRangeFilterUnit.Months, value: 1 } as DateRangeFilter;
            const result = service.getDuration(filter);
            expect(result).toEqual('P1M');
        });

        it('should return the duration string for years', () => {
            const filter = { unit: DateRangeFilterUnit.Years, value: 4 } as DateRangeFilter;
            const result = service.getDuration(filter);
            expect(result).toEqual('P4Y');
        });
    });

    // Obsolete method
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

    // Obsolete method
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
                expect(result).toEqual(scenario.expectedDate);
            });
        });
    });
});
