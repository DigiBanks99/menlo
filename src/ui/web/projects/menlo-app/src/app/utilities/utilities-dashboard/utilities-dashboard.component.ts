import { AsyncPipe, CommonModule, JsonPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, Signal } from '@angular/core';
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
            <!-- add the buttons to a div with 1 px to prevent them from filling the whole box -->
            <div class="px-1">
                <div class="btn-group" role="group">
                    <a class="btn btn-primary text-nowrap" routerLink="../electricity/usage" role="button" id="addElectricityUsage">
                        <span class="material-symbols-outlined align-text-bottom">bolt</span>
                        Add
                    </a>
                    <div class="btn-group" role="group">
                        <button type="button" class="btn btn-primary dropdown-toggle" data-bs-toggle="dropdown" aria-expanded="false"></button>
                        <ul class="dropdown-menu">
                            <li>
                                <a class="dropdown-item" routerLink="../electricity/purchase">
                                    <span class="material-symbols-outlined align-text-bottom">shopping_cart</span>
                                    Electricity Purchase
                                </a>
                            </li>
                        </ul>
                    </div>
                </div>
            </div>
        </header>
        <article class="h-100">
            <h3>Electricity Usage</h3>
            <menlo-electricity-usage [electricityUsage]="electricityUsage()" [loading]="loading()" />
        </article>`,
    styleUrl: './utilities-dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class UtilitiesDashboardComponent extends DestroyableComponent {
    public readonly electricityUsage: Signal<ElectricityUsage[]>;

    public readonly loading = computed(() => this._utilitiesService.loading());

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
                response.map(item => {
                    const usage = new ElectricityUsage();
                    usage.date = item.date;
                    usage.units = item.units;
                    usage.usage = item.usage;
                    for (const appliance of item.applianceUsages) {
                        usage.applianceUsage.push({
                            applianceId: appliance.applianceId,
                            hoursOfUse: appliance.hoursOfUse
                        });
                    }
                    return usage;
                })
            ),
            takeUntil(this.destroyed$)
        );
    }
}
