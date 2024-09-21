import { ChangeDetectionStrategy, Component, input, InputSignal, ViewEncapsulation } from '@angular/core';
import { LoadKind } from './load-kind.type';

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
    styles: [
        `
            .loader-container {
                width: 100%;
                height: 100%;
                display: flex;
                margin-right: 1rem;
                justify-content: center;
                align-items: center;

                .loader {
                    position: relative;
                    margin: auto;
                    width: 70%;
                    height: 70%;
                    display: flex;
                    justify-content: center;
                    align-items: center;

                    .loader__center {
                        height: 75%;
                        width: auto;
                        aspect-ratio: 1/1;

                        .spinner-border {
                            --menlo-spinner-width: 100%;
                            --menlo-spinner-height: 100%;
                            min-height: 1rem;
                            min-width: 1rem;
                        }

                        .spinner-border-sm {
                            --menlo-spinner-width: 1rem;
                            --menlo-spinner-height: 1rem;
                        }
                    }
                }
            }
        `
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadingComponent {
    public readonly kind = input<LoadKind>(LoadKind.Default);

    public LoadingKind = LoadKind;
}
