import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MenloLib } from 'menlo-lib';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, MenloLib],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('menlo-app');
}
