import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'menlo-home',
    standalone: true,
    imports: [],
    template: `<p>home works!</p>`,
    styleUrl: './home.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent {}
