import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { DestroyableComponent, FormButtonsComponent } from 'menlo-lib';
import { ElectricityPurchaseRequest, ElectricityPurchaseRequestFactory } from '../electricity-purchase.request';
import { takeUntil } from 'rxjs';
import { UtilitiesService } from '@utilities/utilities.service';
import { Router } from '@angular/router';

type ElectricityPurchaseForm = {
    date: FormControl<Date>;
    units: FormControl<number>;
    cost: FormControl<number>;
};

@Component({
    selector: 'menlo-electricity-purchase',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FormButtonsComponent],
    template: `
        <header class="d-flex flex-nowrap p-0">
            <h1 class="me-auto">Electricity Purchase</h1>
            <menlo-form-buttons (submit)="onSubmit()" (cancel)="onCancel()"></menlo-form-buttons>
        </header>
        <article>
            <form [formGroup]="form">
                <div class="form-floating">
                    <input type="date" class="form-control" id="date" formControlName="date" placeholder="Date" />
                    <label for="date">Date</label>
                </div>
                <div class="form-group input-group">
                    <div class="form-floating">
                        <input type="number" class="form-control" id="units" formControlName="units" placeholder="Units" />
                        <label for="units">Units</label>
                    </div>
                    <span class="input-group-text">kW/h</span>
                </div>
                <div class="form-group input-group">
                    <span class="input-group-text">R</span>
                    <div class="form-floating">
                        <input type="number" class="form-control" id="cost" formControlName="cost" placeholder="Cost" pattern="#.##" />
                        <label for="cost">Cost</label>
                    </div>
                </div>
            </form>
        </article>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ElectricityPurchaseComponent extends DestroyableComponent {
    public readonly form = new FormGroup<ElectricityPurchaseForm>({
        date: new FormControl<Date>(new Date(), { nonNullable: true }),
        units: new FormControl<number>(0, { nonNullable: true }),
        cost: new FormControl<number>(0, { nonNullable: true })
    });

    constructor(
        private readonly _utilitiesService: UtilitiesService,
        private readonly _router: Router
    ) {
        super();
    }

    onSubmit(): void {
        const request: ElectricityPurchaseRequest = ElectricityPurchaseRequestFactory.create(
            this.form.value.date ?? new Date(),
            this.form.value.units ?? 0,
            this.form.value.cost ?? 0
        );
        this._utilitiesService
            .captureElectricityPurchase(request)
            .pipe(takeUntil(this.destroyed$))
            .subscribe(() => {
                this.form.reset();
                this._router.navigate(['dashboard'], { relativeTo: this._router.routerState.root });
            });
    }

    onCancel(): void {
        this.form.reset();
    }
}
