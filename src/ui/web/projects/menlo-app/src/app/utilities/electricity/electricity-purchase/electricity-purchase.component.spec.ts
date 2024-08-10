import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';

import { ElectricityPurchaseComponent } from './electricity-purchase.component';
import { provideUtilitiesServiceTesting, UtilitiesService } from '@utilities/utilities.service';
import { asyncData } from '../electricity-capture/electricity-capture.component.spec';

describe('ElectricityPurchaseComponent', () => {
    let component: ElectricityPurchaseComponent;
    let fixture: ComponentFixture<ElectricityPurchaseComponent>;
    let utilitiesService: UtilitiesService;
    let utilityServiceSpy: jasmine.Spy;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ElectricityPurchaseComponent],
            providers: [provideUtilitiesServiceTesting()]
        }).compileComponents();
    });

    beforeEach(() => {
        utilitiesService = TestBed.inject(UtilitiesService);
        utilityServiceSpy = spyOn(utilitiesService, 'captureElectricityPurchase').and.returnValue(asyncData('/utilities/electricity/1'));

        fixture = TestBed.createComponent(ElectricityPurchaseComponent);

        component = fixture.componentInstance;
        fixture.autoDetectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have a header with the title', () => {
        const compiled = fixture.nativeElement;
        expect(compiled.querySelector('header h1').textContent).toContain('Electricity Purchase');
    });

    it('should have a form inside the article with the required form fields', () => {
        const compiled = fixture.nativeElement;
        const form = compiled.querySelector('article form');
        expect(form).toBeTruthy();

        const dateInput = form.querySelector('input#date');
        expect(dateInput).toBeTruthy();
        expect(dateInput.type).toBe('date');

        const unitsInput = form.querySelector('input#units');
        expect(unitsInput).toBeTruthy();
        expect(unitsInput.type).toBe('number');

        const costInput = form.querySelector('input#cost');
        expect(costInput).toBeTruthy();
        expect(costInput.type).toBe('number');
    });

    it('should call the utilities service to capture electricity usage when the submit button is clicked', fakeAsync(() => {
        const onSubmitSpy = spyOn(component, 'onSubmit').and.callThrough();

        const compiled = fixture.nativeElement;
        const form = compiled.querySelector('article form');

        const dateInput = form.querySelector('input#date');
        dateInput.value = '2024-07-23';
        dateInput.dispatchEvent(new Event('input'));

        const unitsInput = form.querySelector('input#units');
        unitsInput.value = '1';
        unitsInput.dispatchEvent(new Event('input'));

        const costInput = form.querySelector('input#cost');
        costInput.value = '5';
        costInput.dispatchEvent(new Event('input'));

        const submitButton = compiled.querySelector('menlo-form-buttons button[type="submit"]');
        submitButton.click();

        expect(onSubmitSpy).toHaveBeenCalled();
        tick();
        fixture.detectChanges();
        expect(utilityServiceSpy).withContext('Service posted request').toHaveBeenCalledTimes(1);
    }));
});
