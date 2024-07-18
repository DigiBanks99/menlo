import { Routes } from '@angular/router';
import { HomeComponent } from './home';

export const routes: Routes = [
    {
        path: '',
        pathMatch: 'full',
        redirectTo: 'home'
    },
    {
        path: 'home',
        component: HomeComponent
    },
    {
        path: 'utilities',
        loadChildren: async () => (await import('@utilities/utilities.routes')).routes
    }
];
