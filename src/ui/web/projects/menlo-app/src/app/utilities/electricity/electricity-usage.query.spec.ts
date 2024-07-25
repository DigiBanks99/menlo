import { ElectricityUsageQuery, ElectricityUsageQueryFactory } from './electricity-usage.query';

describe('ElectricityUsageQuery', () => {
    describe('constructor', () => {
        it('should set the start date', () => {
            const startDate = '2021-01-01';
            const query = new ElectricityUsageQuery(startDate, null, 'Africa/Johannesburg');
            expect(query.startDate).toEqual(startDate);
            expect(query.endDate).toBeNull();
        });

        it('should set the end date', () => {
            const startDate = '2021-01-01';
            const endDate = '2021-01-31';
            const query = new ElectricityUsageQuery(startDate, endDate, 'Africa/Johannesburg');
            expect(query.endDate).toEqual(endDate);
        });

        it('should set the time zone', () => {
            const startDate = '2021-01-01';
            const timeZone = 'Africa/Johannesburg';
            const query = new ElectricityUsageQuery(startDate, null, timeZone);
            expect(query.timeZone).toEqual(timeZone);
        });
    });
});

describe('ElectricityUsageQueryFactory', () => {
    describe('create', () => {
        it('should return an ElectricityUsageQuery with the start date formatted as a string', () => {
            const startDate = new Date();
            const query = ElectricityUsageQueryFactory.create(startDate);
            expect(query.startDate).toEqual(startDate.toISOString());
        });

        it('should return an ElectricityUsageQuery with the start date formatted as a string when provided as a string', () => {
            const startDate = '2021-01-01';
            const query = ElectricityUsageQueryFactory.create(startDate);
            expect(query.startDate).toEqual(startDate);
        });

        it('should return an ElectricityUsageQuery with the end date formatted as a string', () => {
            const startDate = new Date();
            const endDate = new Date();
            const query = ElectricityUsageQueryFactory.create(startDate, endDate);
            expect(query.endDate).toEqual(endDate.toISOString());
        });

        it('should return an ElectricityUsageQuery with the end date formatted as a string when provided as a string', () => {
            const startDate = '2021-01-01';
            const endDate = '2021-01-31';
            const query = ElectricityUsageQueryFactory.create(startDate, endDate);
            expect(query.endDate).toEqual(endDate);
        });

        it('should return an ElectricityUsageQuery with the time zone set to the default time zone', () => {
            const startDate = new Date();
            const query = ElectricityUsageQueryFactory.create(startDate);
            expect(query.timeZone).toEqual(Intl.DateTimeFormat().resolvedOptions().timeZone);
        });

        it('should return an ElectricityUsageQuery with the time zone set to the provided time zone', () => {
            const startDate = new Date();
            const timeZone = 'Africa/Johannesburg';
            const query = ElectricityUsageQueryFactory.create(startDate, undefined, timeZone);
            expect(query.timeZone).toEqual(timeZone);
        });

        it('should return an ElectricityUsageQuery with the end date set to null when not provided', () => {
            const startDate = new Date();
            const query = ElectricityUsageQueryFactory.create(startDate);
            expect(query.endDate).toBeNull();
        });

        it('should return an ElectricityUsageQuery with the end date set to null when undefined', () => {
            const startDate = new Date();
            const query = ElectricityUsageQueryFactory.create(startDate, undefined);
            expect(query.endDate).toBeNull();
        });
    });
});
