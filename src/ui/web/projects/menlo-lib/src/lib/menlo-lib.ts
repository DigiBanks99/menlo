import { Component, Signal } from '@angular/core';
import { MenloApiClient, WeatherForecast } from 'data-access-menlo-api';
import { JsonPipe } from '@angular/common';

@Component({
  selector: 'lib-menlo-lib',
  standalone: true,
  imports: [JsonPipe],
  template: `
    <pre>
      {{ forecasts() | json }}
    </pre>
  `,
  styles: ``
})
export class MenloLib {
  constructor(private menloApiClient: MenloApiClient) {
    this.forecasts = this.menloApiClient.forecasts;
    this.menloApiClient.loadWeather();
  }

  public forecasts: Signal<WeatherForecast[] | null>
}
