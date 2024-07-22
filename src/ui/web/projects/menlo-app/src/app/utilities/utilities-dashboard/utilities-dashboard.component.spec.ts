import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UtilitiesDashboardComponent } from './utilities-dashboard.component';
import { provideRouter } from '@angular/router';

describe('UtilitiesDashboardComponent', () => {
    let component: UtilitiesDashboardComponent;
    let fixture: ComponentFixture<UtilitiesDashboardComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [UtilitiesDashboardComponent],
            providers: [provideRouter([])]
        }).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(UtilitiesDashboardComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have a header with the title', () => {
        const compiled = fixture.nativeElement;
        expect(compiled.querySelector('header h1').textContent).toContain('Utilities');
    });

    it('should have a button to add a new electricity usage', () => {
        const compiled = fixture.nativeElement;
        expect(compiled.querySelector('header a').textContent).toContain('Add');
    });
});
