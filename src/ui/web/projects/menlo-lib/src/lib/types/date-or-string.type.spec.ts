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
        expect(formatted).toEqual(date.toISOString());
    });

    it('should return the string when given a string', () => {
        const date = '2021-01-01';
        const formatted = formatDate(date);
        expect(formatted).toEqual(date);
    });
});
