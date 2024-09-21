import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, forwardRef, input } from '@angular/core';
import { ControlValueAccessor, FormControl, FormGroup, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { DateRangeFilter, DateRangeFilterUnit } from './date-range-filter.type';

type DateRangeFilterForm = {
    unit: FormControl<DateRangeFilterUnit>;
    value: FormControl<number>;
};

@Component({
    selector: 'menlo-date-range-filter',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => DateRangeFilterComponent),
            multi: true
        }
    ],
    template: `<form [formGroup]="form">
        <div class="form-group input-group">
            @if (label()) {
                <div class="form-floating">
                    <input type="number" class="form-control" id="value" formControlName="value" placeholder="Value" />
                    <label for="value">{{ label() }}</label>
                </div>
            } @else {
                <input type="number" class="form-control" id="value" formControlName="value" placeholder="Value" />
            }
            <select class="form-select input-group-text" id="unit" formControlName="unit">
                @for (unit of unitOptions; track $index) {
                    <option [value]="unit[1]">{{ unit[0] }}</option>
                }
            </select>
        </div>
    </form>`,
    styles: [
        `
            :host {
                display: block;
            }

            #value {
                text-align: right;
            }
        `
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class DateRangeFilterComponent implements ControlValueAccessor {
    public readonly label = input<string>();
    public readonly form = new FormGroup<DateRangeFilterForm>({
        unit: new FormControl<DateRangeFilterUnit>(DateRangeFilterUnit.Days, { nonNullable: true }),
        value: new FormControl<number>(0, { nonNullable: true })
    });

    public readonly unitOptions = Object.entries(DateRangeFilterUnit);

    private _onChange: (val: DateRangeFilter) => void = () => {};
    private _onTouched: () => void = () => {};

    constructor() {
        this.form.valueChanges.subscribe(value => {
            this._onChange({
                unit: value.unit ?? DateRangeFilterUnit.Days,
                value: value.value ?? 0
            });
        });

        this.form.statusChanges.subscribe(_ => {
            this._onTouched();
        });
    }

    writeValue(filter: DateRangeFilter): void {
        this.form.patchValue({
            unit: filter.unit ?? DateRangeFilterUnit.Days,
            value: filter.value ?? 0
        });
    }

    registerOnChange(fn: (val: DateRangeFilter) => void): void {
        this._onChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this._onTouched = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        if (isDisabled) {
            this.form.disable();
        } else {
            this.form.enable();
        }
    }
}
