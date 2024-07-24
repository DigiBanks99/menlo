import { TestBed } from '@angular/core/testing';

import { UtilitiesService } from './utilities.service';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CaptureElectricityUsageRequest } from './electricity';
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

            httpTesting.verify();
        });
    });
});
