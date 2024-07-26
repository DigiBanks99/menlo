import { AsyncPipe, CommonModule, JsonPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Signal } from '@angular/core';
import { RouterLinkWithHref } from '@angular/router';
import { ElectricityUsageComponent } from '../electricity/electricity-usage/electricity-usage.component';
import { UtilitiesService } from '@utilities/utilities.service';
import { map, Observable, takeUntil } from 'rxjs';
import { ElectricityUsageQueryFactory } from '@utilities/electricity';
import { ElectricityUsage } from '@utilities/electricity/electricity-usage/electricity-usage.model';
import { DestroyableComponent } from 'menlo-lib';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
    standalone: true,
    imports: [AsyncPipe, JsonPipe, RouterLinkWithHref, ElectricityUsageComponent],
    template: `<header class="d-flex flex-nowrap p-0">
            <h1 class="me-auto">Utilities</h1>
            <div class="px-1">
                <a class="btn btn-primary text-nowrap" routerLink="../electricity">
                    <span class="material-symbols-outlined align-text-bottom">bolt</span>
                    Add
                </a>
            </div>
        </header>
        <article class="h-100">
            <h3>Electricity Usage</h3>
            <menlo-electricity-usage [electricityUsage]="electricityUsage()" />
        </article>`,
    styleUrl: './utilities-dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class UtilitiesDashboardComponent extends DestroyableComponent {
    public readonly electricityUsage: Signal<ElectricityUsage[]>;

    constructor(private readonly _utilitiesService: UtilitiesService) {
        super();
        this.electricityUsage = toSignal(this.setupElectricityUsageObservable(), { initialValue: [] });
    }

    private setupElectricityUsageObservable(): Observable<ElectricityUsage[]> {
        const today = new Date();
        const startDate = new Date(today.getFullYear(), today.getMonth(), 1);
        const request = ElectricityUsageQueryFactory.create(startDate, today);
        return this._utilitiesService.getElectricityUsage(request).pipe(
            map(response =>
                response.map(
                    item =>
                        <ElectricityUsage>{
                            date: item.date,
                            units: item.units
                        }
                )
            ),
            takeUntil(this.destroyed$)
        );
    }
}
