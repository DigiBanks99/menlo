import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NavItem, RootComponent } from 'menlo-lib';
import { MenloRoutes, routes } from './app.routes';

@Component({
    selector: 'menlo-root',
    standalone: true,
    imports: [RootComponent],
    template: `<menlo-layout-root [navItems]="navItems" />`,
    styleUrl: './app.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {
    public navItems: NavItem[] = this.mapRoutesToNavItems(routes);

    private mapRoutesToNavItems(routes: MenloRoutes): NavItem[] {
        const navItems = routes
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

        return navItems;
    }
}
