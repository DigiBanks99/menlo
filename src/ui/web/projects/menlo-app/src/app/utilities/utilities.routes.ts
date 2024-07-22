import { Routes } from '@angular/router';
import { UtilitiesDashboardComponent } from './utilities-dashboard/utilities-dashboard.component';
import { ElectricityCaptureComponent } from './electricity/electricity-capture/electricity-capture.component';
import { MenloRoutes } from '../app.routes';

export const routes: Routes & MenloRoutes = [
    {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
    },
    {
        path: 'dashboard',
        component: UtilitiesDashboardComponent
    },
    {
        path: 'electricity',
        component: ElectricityCaptureComponent
    }
];
