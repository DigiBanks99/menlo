import { Data, Routes } from '@angular/router';
import { HomeComponent } from './home';

export type MenloRouteData = { iconName: string }
export type MenloRoutes = Routes & {data?: Data & MenloRouteData }[];

export const routes: MenloRoutes = [
    {
        path: '',
        pathMatch: 'full',
        redirectTo: 'home'
    },
    {
        path: 'home',
        component: HomeComponent,
        title: 'Home',
        data: {
            iconName: 'home'
        }
    },
    {
        path: 'utilities',
        loadChildren: async () => (await import('@utilities/utilities.routes')).routes,
        title: 'Utilities',
        data: {
            iconName: 'water_ec'
        }
    }
];
