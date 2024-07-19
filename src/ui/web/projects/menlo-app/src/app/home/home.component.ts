import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'menlo-home',
    standalone: true,
    imports: [],
    template: `<h1>Dashboard</h1>`,
    styleUrl: './home.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent {}
