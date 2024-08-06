import { ElectricityUsage } from './electricity-usage.model';

describe('ElectricityUsage', () => {
    it('should create an instance', () => {
        expect(new ElectricityUsage()).toBeTruthy();
    });

    describe('getUnitDifference', () => {
        it('should return the difference between the units of two ElectricityUsage instances', () => {
            const usage1 = new ElectricityUsage();
            usage1.units = 100;

            const usage2 = new ElectricityUsage();
            usage2.units = 50;

            expect(usage2.getUnitDifference(usage1)).toBe(50);
        });
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

    describe('applianceUsage', () => {
        it('should return the appliance usage', () => {
            const usage = new ElectricityUsage();
            usage.applianceUsage = [{ applianceId: 1, hoursOfUse: 2 }];
            expect(usage.applianceUsage).toEqual([{ applianceId: 1, hoursOfUse: 2 }]);
        });
    });
});
