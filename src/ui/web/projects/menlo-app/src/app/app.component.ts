import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'menlo-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `
    <h1>Welcome to {{title}}!</h1>

    <router-outlet />
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'menlo-app';
}
