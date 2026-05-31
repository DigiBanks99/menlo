import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url.token';
import {
  CreateHouseholdRequest,
  HouseholdDto,
  ListHouseholdsResponse,
} from '../models/household';

@Injectable({
  providedIn: 'root',
})
export class HouseholdApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);
  private readonly apiUrl = `${this.apiBaseUrl}/api/households`;

  listHouseholds(): Observable<ListHouseholdsResponse> {
    return this.http.get<ListHouseholdsResponse>(this.apiUrl);
  }

  createHousehold(request: CreateHouseholdRequest): Observable<HouseholdDto> {
    return this.http.post<HouseholdDto>(this.apiUrl, request);
  }

  joinHousehold(householdId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${householdId}/join`, {});
  }
}
