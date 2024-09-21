import { SIGNAL, signalSetFn } from '@angular/core/primitives/signals';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { DateRangeFilterComponent } from './date-range-filter.component';

describe('DateRangeFilterComponent', () => {
    let component: DateRangeFilterComponent;
    let fixture: ComponentFixture<DateRangeFilterComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ReactiveFormsModule, DateRangeFilterComponent]
        }).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(DateRangeFilterComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    describe('label', () => {
        const scenarios = [{ label: 'Date Range' }, { label: 'Snowy' }, { label: 'Mooch' }];

        scenarios.forEach(({ label }) => {
            it(`should return '${label}' when label is '${label}'`, () => {
                signalSetFn(component.label[SIGNAL], label);
                expect(component.label()).toBe(label);
            });
        });

        it('should return undefined when not set', () => {
            expect(component.label()).toBeUndefined();
        });

        it('should have a label element when label is set', () => {
            signalSetFn(component.label[SIGNAL], 'Date Range');
            fixture.detectChanges();
            const labelElement = fixture.nativeElement.querySelector('label');
            expect(labelElement).toBeTruthy();
            expect(labelElement.textContent).toBe('Date Range');
        });

        it('should not have a label element when label is not set', () => {
            const labelElement = fixture.nativeElement.querySelector('label');
            expect(labelElement).toBeFalsy();
        });
    });
});
