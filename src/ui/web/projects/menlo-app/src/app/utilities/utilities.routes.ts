import { Routes } from "@angular/router";
import { UtilitiesDashboardComponent } from "./utilities-dashboard/utilities-dashboard.component";

export const routes: Routes = [
    {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
    },
    {
        path: 'dashboard',
        component: UtilitiesDashboardComponent
    }
];
