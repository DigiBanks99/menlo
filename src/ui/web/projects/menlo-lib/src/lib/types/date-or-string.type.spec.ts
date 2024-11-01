import { DateOrString, formatDate, getDateDiff } from './date-or-string.type';

describe('DateOrString', () => {
    it('should allow a Date to be assinged', () => {
        const date: DateOrString = new Date();
        expect(date).toBeDefined();
    });

    it('should allow a string to be assigned', () => {
        const date: DateOrString = '2021-01-01';
        expect(date).toBeDefined();
    });
});

describe('formatDate', () => {
    it('should return a string when given a Date', () => {
        const date = new Date(2024, 7, 1);
        const formatted = formatDate(date);
        expect(formatted).toEqual('2024-08-01');
    });

    for (const date of ['2024-08-01', '2024-08-01T00:00:00Z', '2024-08-01T00:00:00+02:00']) {
        it(`should return the string when given a string - ${date}`, () => {
            const formatted = formatDate(date);
            expect(formatted).toEqual('2024-08-01');
        });
    }
});

describe('getDateDiff', () => {
    it('should return the difference in days between two dates', () => {
        const date1 = new Date(2024, 7, 1);
        const date2 = new Date(2024, 7, 5);
        const diff = getDateDiff(date1, date2);
        expect(diff).toEqual(4);
    });

    it('should return the difference in months between two dates', () => {
        const date1 = new Date(2024, 7, 1);
        const date2 = new Date(2025, 9, 1);
        const diff = getDateDiff(date1, date2, 'months');
        expect(diff).toEqual(14);
    });

    it('should return the difference in years between two dates', () => {
        const date1 = new Date(2024, 7, 1);
        const date2 = new Date(2026, 7, 1);
        const diff = getDateDiff(date1, date2, 'years');
        expect(diff).toEqual(2);
    });

    it('should return the difference in days between two strings', () => {
        const date1 = '2024-08-01';
        const date2 = '2024-08-05';
        const diff = getDateDiff(date1, date2);
        expect(diff).toEqual(4);
    });

    it('should return the difference in months between two strings', () => {
        const date1 = '2024-08-01';
        const date2 = '2024-10-01';
        const diff = getDateDiff(date1, date2, 'months');
        expect(diff).toEqual(2);
    });

    it('should return the difference in years between two strings', () => {
        const date1 = '2024-08-01';
        const date2 = '2026-08-01';
        const diff = getDateDiff(date1, date2, 'years');
        expect(diff).toEqual(2);
    });

    it('should return the difference in days between a string and a Date', () => {
        const date1 = '2024-08-01';
        const date2 = new Date(2024, 7, 5);
        const diff = getDateDiff(date1, date2);
        expect(diff).toEqual(4);
    });

    it('should return the difference in months between a string and a Date', () => {
        const date1 = '2024-08-01';
        const date2 = new Date(2024, 9, 1);
        const diff = getDateDiff(date1, date2, 'months');
        expect(diff).toEqual(2);
    });

    it('should return the difference in years between a string and a Date', () => {
        const date1 = '2024-08-01';
        const date2 = new Date(2026, 7, 1);
        const diff = getDateDiff(date1, date2, 'years');
        expect(diff).toEqual(2);
    });
});
