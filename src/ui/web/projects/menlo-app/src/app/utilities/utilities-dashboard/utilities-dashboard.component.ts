import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLinkWithHref } from '@angular/router';

@Component({
    selector: 'menlo-utility-dashboard',
    standalone: true,
    imports: [CommonModule, RouterLinkWithHref],
    template: `<header class="d-flex flex-nowrap p-0">
        <h1>Utilities</h1>
        <div class="w-100"></div>
        <a class="btn btn-primary text-nowrap" routerLink="../electricity">
            <span class="material-symbols-outlined feather feather-home align-text-bottom">add</span>
            Add
        </a>
    </header>`,
    styleUrl: './utilities-dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class UtilitiesDashboardComponent {}
