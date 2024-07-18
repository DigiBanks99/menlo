import { Component } from '@angular/core';
import NavItem from 'menlo-lib/src/lib/nav-item.model';
import { RootComponent } from 'menlo-lib/src/public-api';

@Component({
  selector: 'menlo-root',
  standalone: true,
  imports: [RootComponent],
  template: `
    <menlo-layout-root [navItems]="navItems" />
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  public navItems: NavItem[] = []; // pull from routes data
}
