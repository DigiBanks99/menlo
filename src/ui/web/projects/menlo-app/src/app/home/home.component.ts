import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'menlo-home',
    standalone: true,
    imports: [],
    template: `<header class="d-flex flex-nowrap p-0"><h1 class="me-auto">Dashboard</h1></header>`,
    styleUrl: './home.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent {}
