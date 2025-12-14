import { JsonPipe } from '@angular/common';
import { Component, signal } from '@angular/core';

export interface WeatherForecast {
  date: string;
  temperatureC: number;
  summary: string;
}

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
  public forecasts = signal<WeatherForecast[] | null>([]);
}
