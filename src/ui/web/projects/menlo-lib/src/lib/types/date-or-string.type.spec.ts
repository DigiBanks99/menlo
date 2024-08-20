import { DateOrString, formatDate } from './date-or-string.type';

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
        const date = new Date();
        const formatted = formatDate(date);
        expect(formatted).toEqual(Intl.DateTimeFormat().format(date));
    });

    for (const date of ['2024-08-01', '2024-08-01T00:00:00Z', '2024-08-01T00:00:00+02:00']) {
        it(`should return the string when given a string - ${date}`, () => {
            const formatted = formatDate(date);
            expect(formatted).toEqual('2024-08-01');
        });
    }
});
