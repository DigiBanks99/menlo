import { Routes } from '@angular/router';
import { UtilitiesDashboardComponent } from './utilities-dashboard/utilities-dashboard.component';
import { ElectricityCaptureComponent } from './electricity/electricity-capture/electricity-capture.component';
import { MenloRoutes } from '../app.routes';
import { ElectricityPurchaseComponent } from './electricity';

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
        path: 'electricity/usage',
        component: ElectricityCaptureComponent
    },
    {
        path: 'electricity/purchase',
        component: ElectricityPurchaseComponent
    }
];
