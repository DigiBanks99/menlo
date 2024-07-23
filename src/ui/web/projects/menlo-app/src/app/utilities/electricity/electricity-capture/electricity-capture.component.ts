import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { FormButtonsComponent } from '../../../../../../menlo-lib/src/lib/forms/form-buttons/form-buttons.component';

type ApplianceUsageForm = {
    applianceId: FormControl<number>;
    hoursOfUse: FormControl<number>;
};

type ElectricityCaptureForm = {
    date: FormControl<Date>;
    units: FormControl<number>;
    applianceUsages: FormArray<FormGroup<ApplianceUsageForm>>;
};

@Component({
    selector: 'menlo-electricity-capture',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FormButtonsComponent],
    template: ` <header class="d-flex flex-nowrap p-0">
            <h1 class="me-auto">Electricity Capture</h1>
            <menlo-form-buttons (submit)="onSubmit()" (cancel)="onCancel()"></menlo-form-buttons>
        </header>
        <article>
            <form [formGroup]="form">
                <div class="form-floating">
                    <input type="date" class="form-control" id="date" formControlName="date" />
                    <label for="date">Date</label>
                </div>
                <div class="form-floating">
                    <input type="number" class="form-control" id="units" formControlName="units" />
                    <label for="units">Units (kW)</label>
                </div>
            </form>
        </article>`,
    styleUrl: './electricity-capture.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ElectricityCaptureComponent {
    public readonly form = new FormGroup<ElectricityCaptureForm>({
        date: new FormControl<Date>(new Date(), { nonNullable: true }),
        units: new FormControl<number>(0, { nonNullable: true }),
        applianceUsages: new FormArray<FormGroup<ApplianceUsageForm>>([])
    });

    public onCancel(): void {
        this.form.reset();
    }

    public onSubmit(): void {
        throw new Error('Method not implemented.');
    }
}