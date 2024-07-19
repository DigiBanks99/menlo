import { Component } from '@angular/core';
import NavItem from 'menlo-lib/src/lib/nav-item.model';
import { RootComponent } from 'menlo-lib/src/public-api';
import { routes } from './app.routes';

@Component({
    selector: 'menlo-root',
    standalone: true,
    imports: [RootComponent],
    template: ` <menlo-layout-root [navItems]="navItems" /> `,
    styleUrl: './app.component.scss'
})
export class AppComponent {
    public navItems: NavItem[] = routes
        .filter(route => (route.path ?? '').length > 0)
        .map(
            route =>
                ({
                    route: route.path,
                    description: route.title,
                    alternateText: route.title,
                    iconName: (route.data ?? { iconName: null })['iconName'] ?? null
                }) as NavItem
        );
}
