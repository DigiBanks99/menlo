import { Routes } from '@angular/router';
import { UtilitiesDashboardComponent } from './utilities-dashboard/utilities-dashboard.component';
import { ElectricityCaptureComponent } from './electricity/electricity-capture/electricity-capture.component';

export const routes: Routes = [
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
