import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ElectricityCaptureComponent } from './electricity-capture.component';

describe('ElectricityCaptureComponent', () => {
    let component: ElectricityCaptureComponent;
    let fixture: ComponentFixture<ElectricityCaptureComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ElectricityCaptureComponent]
        }).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(ElectricityCaptureComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have a header with the title', () => {
        const compiled = fixture.nativeElement;
        expect(compiled.querySelector('header h1').textContent).toContain('Electricity Capture');
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
    });
});
