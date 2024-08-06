import { HttpClient, HttpParams, provideHttpClient } from '@angular/common/http';
import { EnvironmentProviders, Injectable, Provider } from '@angular/core';
import { CaptureElectricityUsageRequest, ElecricityUsageResponse, ElectricityUsageQuery } from './electricity';
import { Observable } from 'rxjs';
import { APP_BASE_HREF } from '@angular/common';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';
import { routes } from './utilities.routes';

@Injectable({
    providedIn: 'root'
})
export class UtilitiesService {
    constructor(private readonly _http: HttpClient) {}

    public captureElectricalUsage(request: CaptureElectricityUsageRequest): Observable<string> {
        return this._http.post<string>(`/api/utilities/electricity`, request);
    }

    public getElectricityUsage(query: ElectricityUsageQuery): Observable<ElecricityUsageResponse[]> {
        let queryString = new HttpParams().set('startDate', query.startDate);

        if (query.endDate !== null) {
            queryString = queryString.set('endDate', query.endDate);
        }

        queryString = queryString.set('timeZone', query.timeZone);

        return this._http.get<ElecricityUsageResponse[]>(`/api/utilities/electricity`, { params: queryString });
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

export function provideUtilitiesServiceTesting(): (Provider | EnvironmentProviders)[] {
    return [provideUtilitiesService(), provideHttpClient(), provideHttpClientTesting(), provideRouter([...routes]), provideLocationMocks()];
}
