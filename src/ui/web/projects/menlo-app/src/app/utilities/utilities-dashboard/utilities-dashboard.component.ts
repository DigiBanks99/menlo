import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, signal, Signal, WritableSignal } from '@angular/core';
import { RouterLinkWithHref } from '@angular/router';
import { ElectricityUsageComponent } from '../electricity/electricity-usage/electricity-usage.component';
import { UtilitiesService } from '@utilities/utilities.service';
import { catchError, distinct, distinctUntilChanged, distinctUntilKeyChanged, map, Observable, switchMap, takeUntil, tap } from 'rxjs';
import { ElectricityUsageQueryFactory } from '@utilities/electricity';
import { ElectricityUsage } from '@utilities/electricity/electricity-usage/electricity-usage.model';
import { DateRangeFilter, DateRangeFilterComponent, DateRangeFilterUnit, DateRangeService, DestroyableComponent } from 'menlo-lib';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';

type DateRangeFilterForm = {
    dateRange: FormControl<DateRangeFilter>;
};

@Component({
    standalone: true,
    imports: [AsyncPipe, RouterLinkWithHref, ElectricityUsageComponent, DateRangeFilterComponent, ReactiveFormsModule],
    template: `<header class="d-flex flex-nowrap p-0">
            <h1 class="me-auto">Utilities</h1>
            <!-- add the buttons to a div with 1 px to prevent them from filling the whole box -->
            <form [formGroup]="form">
                <menlo-date-range-filter formControlName="dateRange"></menlo-date-range-filter>
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
            </form>
        </header>
        <article class="h-100">
            <h3>Electricity Usage</h3>
            <menlo-electricity-usage [electricityUsage]="electricityUsage()" [loading]="loading()" />
        </article>`,
    styles: [
        `
            :host {
                display: block;
                height: 100%;
            }

            header form {
                display: flex;
            }
        `
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class UtilitiesDashboardComponent extends DestroyableComponent {
    public readonly electricityUsage: Signal<ElectricityUsage[]>;
    public readonly form = new FormGroup<DateRangeFilterForm>({
        dateRange: new FormControl<DateRangeFilter>(new DateRangeFilter(DateRangeFilterUnit.Days, 1), { nonNullable: true })
    });
    public readonly loading = computed(() => this._utilitiesService.loading());

    private _dateRangeFilter(): FormControl<DateRangeFilter> {
        return this.form.get('dateRange') as FormControl<DateRangeFilter>;
    }

    constructor(
        private readonly _utilitiesService: UtilitiesService,
        private readonly _dateRangeService: DateRangeService
    ) {
        super();

        this.electricityUsage = toSignal(this.getElectricityUsage(), { initialValue: [] });
        this.form.patchValue({ dateRange: new DateRangeFilter(DateRangeFilterUnit.Weeks, 1) }); // force fetch
    }

    private getElectricityUsage(): Observable<ElectricityUsage[]> {
        return this._dateRangeFilter().valueChanges.pipe(
            switchMap(filter => this.fetchElectricityUsage(filter)),
            takeUntil(this.destroyed$)
        );
    }

    private fetchElectricityUsage(filter: DateRangeFilter): Observable<ElectricityUsage[]> {
        const today = new Date();
        const startDate = this._dateRangeService.getDuration(filter);
        const request = ElectricityUsageQueryFactory.create(startDate, today);
        return this._utilitiesService.getElectricityUsage(request).pipe(
            catchError(err => {
                console.error('Error fetching electricity usage', err);
                return [];
            }),
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
            catchError(err => {
                console.error('Error mapping electricity usage', err);
                return [];
            }),
            takeUntil(this.destroyed$)
        );
    }
}
