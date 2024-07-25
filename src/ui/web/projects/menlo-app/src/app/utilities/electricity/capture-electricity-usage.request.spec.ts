import { CaptureElectricityUsageRequest, CaptureElectricityUsageRequestFactory } from './capture-electricity-usage.request';

describe('CaptureElectricityUsageRequest', () => {
    describe('constructor', () => {
        it('should set the date', () => {
            const date = '2021-01-01';
            const request = new CaptureElectricityUsageRequest(date, 1);
            expect(request.date).toEqual(date);
        });

        it('should set the units', () => {
            const units = 1;
            const request = new CaptureElectricityUsageRequest('2021-01-01', units);
            expect(request.units).toEqual(units);
        });

        it('should set the appliance usages to an empty array when not provided', () => {
            const request = new CaptureElectricityUsageRequest('2021-01-01', 1);
            expect(request.applianceUsages).toEqual([]);
        });

        it('should set the appliance usages to the provided appliance usages', () => {
            const applianceUsages = [{ applianceId: 1, hoursOfUse: 1 }];
            const request = new CaptureElectricityUsageRequest('2021-01-01', 1, applianceUsages);
            expect(request.applianceUsages).toEqual(applianceUsages);
        });
    });

    describe('addApplianceUsage', () => {
        it('should add the appliance usage', () => {
            const request = new CaptureElectricityUsageRequest('2021-01-01', 1);
            request.addApplianceUsage(1, 1);
            expect(request.applianceUsages).toEqual([{ applianceId: 1, hoursOfUse: 1 }]);
        });
    });
});

describe('CaptureElectricityUsageRequestFactory', () => {
    describe('create', () => {
        it('should return a CaptureElectricityUsageRequest with the date formatted as a string', () => {
            const date = new Date();
            const request = CaptureElectricityUsageRequestFactory.create(date, 1);
            expect(request.date).toEqual(date.toISOString());
        });

        it('should return a CaptureElectricityUsageRequest with the date formatted as a string when provided as a string', () => {
            const date = '2021-01-01';
            const request = CaptureElectricityUsageRequestFactory.create(date, 1);
            expect(request.date).toEqual(date);
        });

        it('should return a CaptureElectricityUsageRequest with the units', () => {
            const units = 1;
            const request = CaptureElectricityUsageRequestFactory.create('2021-01-01', units);
            expect(request.units).toEqual(units);
        });

        it('should return a CaptureElectricityUsageRequest with the appliance usages set to an empty array when not provided', () => {
            const request = CaptureElectricityUsageRequestFactory.create('2021-01-01', 1);
            expect(request.applianceUsages).toEqual([]);
        });

        it('should return a CaptureElectricityUsageRequest with the appliance usages set to the provided appliance usages', () => {
            const applianceUsages = [{ applianceId: 1, hoursOfUse: 1 }];
            const request = CaptureElectricityUsageRequestFactory.create('2021-01-01', 1, applianceUsages);
            expect(request.applianceUsages).toEqual(applianceUsages);
        });
    });
});
