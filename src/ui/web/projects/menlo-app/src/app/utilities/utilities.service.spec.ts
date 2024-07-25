import { TestBed } from '@angular/core/testing';

import { UtilitiesService } from './utilities.service';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CaptureElectricityUsageRequest, ElectricityUsageQueryFactory } from './electricity';
import { firstValueFrom } from 'rxjs';

describe('UtilitiesService', () => {
    let service: UtilitiesService;
    let httpTesting: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [UtilitiesService, provideHttpClient(), provideHttpClientTesting()]
        });
        service = TestBed.inject(UtilitiesService);
        httpTesting = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpTesting.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('captureElectricalUsage', () => {
        it('should post electricity usage', async () => {
            const captureElectricalUsageRequest = new CaptureElectricityUsageRequest('2024-07-23T00:00:00Z', 1);
            const request$ = service.captureElectricalUsage(captureElectricalUsageRequest);
            const requestPromise = firstValueFrom(request$);

            const request = httpTesting.expectOne('/api/utilities/electricity', 'Expect capture electricity usage request to be made');

            expect(request.request.method).toBe('POST');
            expect(request.request.body as CaptureElectricityUsageRequest).toBe(captureElectricalUsageRequest);

            request.flush('/api/utilities/electricity/1');

            expect(await requestPromise).toBe('/api/utilities/electricity/1');
        });

        afterEach(() => {
            httpTesting.verify();
        });
    });

    describe('getElectricityUsage', () => {
        beforeEach(() => {
            service = TestBed.inject(UtilitiesService);
            httpTesting = TestBed.inject(HttpTestingController);
        });

        it('should get electricity usage', async () => {
            const query = ElectricityUsageQueryFactory.create('2024-07-23T00:00:00Z');
            const request$ = service.getElectricityUsage(query);
            const requestPromise = firstValueFrom(request$);
            const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;

            const request = httpTesting.expectOne(
                `/api/utilities/electricity?startDate=2024-07-23T00:00:00Z&timeZone=${timeZone}`,
                'Expect get electricity usage request to be made'
            );

            expect(request.request.method).toBe('GET');

            request.flush([]);

            expect(await requestPromise).toEqual([]);
        });

        it('should get electricity usage with end date', async () => {
            const query = ElectricityUsageQueryFactory.create('2024-07-24T00:00:00Z', '2024-07-25T00:00:00Z');
            const request$ = service.getElectricityUsage(query);
            const requestPromise = firstValueFrom(request$);
            const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;

            const request = httpTesting.expectOne(
                `/api/utilities/electricity?startDate=2024-07-24T00:00:00Z&endDate=2024-07-25T00:00:00Z&timeZone=${timeZone}`,
                'Expect get electricity usage request to be made with an end date'
            );

            expect(request.request.method).toBe('GET');

            request.flush([]);

            expect(await requestPromise).toEqual([]);
        });

        it('should get electricity usage with end date and time zone', async () => {
            const query = ElectricityUsageQueryFactory.create('2024-07-01T00:00:00Z', '2024-07-31T00:00:00Z', 'Africa/Johannesburg');
            const request$ = service.getElectricityUsage(query);
            const requestPromise = firstValueFrom(request$);

            const request = httpTesting.expectOne(
                '/api/utilities/electricity?startDate=2024-07-01T00:00:00Z&endDate=2024-07-31T00:00:00Z&timeZone=Africa/Johannesburg',
                'Expect get electricity usage request to be made'
            );

            expect(request.request.method).toBe('GET');

            request.flush([]);

            expect(await requestPromise).toEqual([]);
        });

        afterEach(() => {
            httpTesting.verify();
        });
    });
});
