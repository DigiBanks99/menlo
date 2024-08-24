import { ChangeDetectionStrategy, Component, input, InputSignal, ViewEncapsulation } from '@angular/core';
import { LoadKind } from './load-kind.enum';

@Component({
    selector: 'menlo-loading',
    standalone: true,
    imports: [],
    template: `<div class="loader-container">
        <div class="loader">
            <div class="loader__center">
                <div class="spinner-border" [class.spinner-border-sm]="kind() === LoadingKind.Small" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        </div>
    </div>`,
    styleUrl: './loading.component.scss',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadingComponent {
    public readonly kind = input<LoadKind>(LoadKind.Default);

    public LoadingKind = LoadKind;
}
