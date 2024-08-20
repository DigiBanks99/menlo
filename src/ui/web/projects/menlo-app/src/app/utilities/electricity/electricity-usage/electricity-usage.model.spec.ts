import { ElectricityUsage } from './electricity-usage.model';

describe('ElectricityUsage', () => {
    it('should create an instance', () => {
        expect(new ElectricityUsage()).toBeTruthy();
    });

    describe('date', () => {
        it('should throw an error if date is not set', () => {
            const usage = new ElectricityUsage();
            expect(() => usage.date).toThrowError('date is required');
        });

        it('should return the date', () => {
            const usage = new ElectricityUsage();
            usage.date = '2024-06-22T00:00:00Z';
            expect(usage.date).toBe('2024-06-22T00:00:00Z');
        });
    });

    describe('units', () => {
        it('should throw an error if units is not set', () => {
            const usage = new ElectricityUsage();
            expect(() => usage.units).toThrowError('units is required');
        });

        it('should return the units', () => {
            const usage = new ElectricityUsage();
            usage.units = 318.16;
            expect(usage.units).toBe(318.16);
        });
    });

    describe('usage', () => {
        it('should throw an error if usage is not set', () => {
            const usage = new ElectricityUsage();
            expect(() => usage.usage).toThrowError('usage is required');
        });

        it('should return the usage', () => {
            const usage = new ElectricityUsage();
            usage.usage = 318.16;
            expect(usage.usage).toBe(318.16);
        });
    });

    describe('applianceUsage', () => {
        it('should return the appliance usage', () => {
            const usage = new ElectricityUsage();
            usage.applianceUsage = [{ applianceId: 1, hoursOfUse: 2 }];
            expect(usage.applianceUsage).toEqual([{ applianceId: 1, hoursOfUse: 2 }]);
        });
    });
});
