import { HttpClient, provideHttpClient } from '@angular/common/http';
import { EnvironmentProviders, Injectable, Provider } from '@angular/core';
import { CaptureElectricityUsageRequest } from './electricity';
import { Observable, of } from 'rxjs';
import { APP_BASE_HREF } from '@angular/common';
import { provideHttpClientTesting } from '@angular/common/http/testing';

@Injectable({
    providedIn: 'root'
})
export class UtilitiesService {
    constructor(private readonly _http: HttpClient) {}

    public captureElectricalUsage(request: CaptureElectricityUsageRequest): Observable<string> {
        return this._http.post<string>(`/utilities/electricity`, request);
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
    return [provideUtilitiesService(), provideHttpClient(), provideHttpClientTesting()];
}
