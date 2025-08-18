import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';

export interface WeatherForecast {
  date: string;
  temperatureC: number;
  summary: string;
}

@Injectable({ providedIn: 'root' })
export class MenloApiClient {
  private readonly http = inject(HttpClient);

  forecasts = signal<WeatherForecast[] | null>(null);

  loadWeather(): void {
    this.http.get<WeatherForecast[]>('/api/weatherforecast').subscribe({
      next: (data) => this.forecasts.set(data),
      error: () => this.forecasts.set([]),
    });
  }
}
