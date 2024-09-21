import { HttpClient, HttpParams, provideHttpClient } from '@angular/common/http';
import { computed, EnvironmentProviders, Injectable, Provider, signal } from '@angular/core';
import { CaptureElectricityUsageRequest, ElecricityUsageResponse, ElectricityPurchaseRequest, ElectricityUsageQuery } from './electricity';
import { Observable, of, tap } from 'rxjs';
import { APP_BASE_HREF } from '@angular/common';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';
import { NoopComponent } from 'menlo-lib';

@Injectable({
    providedIn: 'root'
})
export class UtilitiesService {
    public loading = signal(false);

    constructor(private readonly _http: HttpClient) {}

    public captureElectricalUsage(request: CaptureElectricityUsageRequest): Observable<string> {
        this.loading.set(true);
        return this._http.post<string>(`/api/utilities/electricity/usage`, request).pipe(tap(() => this.loading.set(false)));
    }

    public captureElectricityPurchase(request: ElectricityPurchaseRequest) {
        this.loading.set(true);
        return this._http.post<string>(`/api/utilities/electricity/purchase`, request).pipe(tap(() => this.loading.set(false)));
    }

    public getElectricityUsage(query: ElectricityUsageQuery): Observable<ElecricityUsageResponse[]> {
        this.loading.set(true);
        let queryString = new HttpParams().set('startDate', query.startDate);

        if (query.endDate !== null) {
            queryString = queryString.set('endDate', query.endDate);
        }

        queryString = queryString.set('timeZone', query.timeZone);

        return this._http
            .get<ElecricityUsageResponse[]>(`/api/utilities/electricity`, { params: queryString })
            .pipe(tap(() => this.loading.set(false)));
    }
}

export function provideUtilitiesService(): Provider[] {
    return [
        UtilitiesService,
        {
            provide: APP_BASE_HREF,
            useValue: '/'
        }
    ];
}

export interface UtilitiesServiceTestingOptions {
    loading: boolean;
}

type UtilitiesServiceTestingProviders = (Provider | EnvironmentProviders)[];

export function provideUtilitiesServiceTesting(options: UtilitiesServiceTestingOptions | null = null): UtilitiesServiceTestingProviders {
    const providers: (Provider | EnvironmentProviders)[] = [
        provideUtilitiesService(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
            {
                path: 'dashboard',
                component: NoopComponent
            }
        ]),
        provideLocationMocks()
    ];

    const effectiveOptions: UtilitiesServiceTestingOptions = options ?? { loading: false };

    providers.push({
        provide: UtilitiesService,
        useValue: {
            loading: signal(effectiveOptions.loading),
            captureElectricalUsage: (request: CaptureElectricityUsageRequest) => {
                return [];
            },
            captureElectricityPurchase: (request: ElectricityPurchaseRequest) => {
                return [];
            },
            getElectricityUsage: (query: ElectricityUsageQuery) => {
                return of([]);
            }
        }
    });

    return providers;
}
