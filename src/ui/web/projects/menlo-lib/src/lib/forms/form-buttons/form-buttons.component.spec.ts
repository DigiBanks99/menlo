import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormButtonsComponent } from './form-buttons.component';

describe('FormButtonsComponent', () => {
    let component: FormButtonsComponent;
    let fixture: ComponentFixture<FormButtonsComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FormButtonsComponent]
        }).compileComponents();

        fixture = TestBed.createComponent(FormButtonsComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have a cancel button', () => {
        const compiled = fixture.nativeElement;
        expect(compiled.querySelector('button[type="reset"]').textContent).toContain('Cancel');
    });

    it('should have a submit button', () => {
        const compiled = fixture.nativeElement;
        expect(compiled.querySelector('button[type="submit"]').textContent).toContain('Submit');
    });

    it('should emit the cancel event when the cancel button is clicked', () => {
        const cancelSpy = spyOn(component.cancel, 'emit');
        const compiled = fixture.nativeElement;
        const cancelButton = compiled.querySelector('button[type="reset"]');
        cancelButton.click();
        expect(cancelSpy).toHaveBeenCalled();
    });

    it('should emit the submit event when the submit button is clicked', () => {
        const submitSpy = spyOn(component.submit, 'emit');
        const compiled = fixture.nativeElement;
        const submitButton = compiled.querySelector('button[type="submit"]');
        submitButton.click();
        expect(submitSpy).toHaveBeenCalled();
    });
});
