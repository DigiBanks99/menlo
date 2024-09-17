import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UtilitiesDashboardComponent } from './utilities-dashboard.component';
import { provideRouter } from '@angular/router';
import { Component } from '@angular/core';
import { ElectricityUsage } from '@utilities/electricity/electricity-usage/electricity-usage.model';
import { provideUtilitiesServiceTesting } from '@utilities/utilities.service';

@Component({ selector: 'menlo-electricity-usage', template: '', standalone: true })
class ElectricityUsageStubComponent {
    electricityUsage: ElectricityUsage[] = [];
    loading = false;
}

describe('UtilitiesDashboardComponent', () => {
    let component: UtilitiesDashboardComponent;
    let fixture: ComponentFixture<UtilitiesDashboardComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [UtilitiesDashboardComponent, ElectricityUsageStubComponent],
            providers: [provideRouter([]), provideUtilitiesServiceTesting()]
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

    describe('header', () => {
        it('should have a header with the title', () => {
            const compiled = fixture.nativeElement;
            expect(compiled.querySelector('header h1').textContent).toContain('Utilities');
        });

        it('should have a button to add a new electricity usage', () => {
            const compiled: HTMLElement = fixture.nativeElement;
            const button: HTMLButtonElement = compiled.querySelector('header div.btn-group a.btn.btn-primary') as HTMLButtonElement;
            expect(button).toBeTruthy();
            expect(button.getAttribute('routerLink')).toBe('../electricity/usage');
        });

        it('should have a button to add a new electricity purchase', () => {
            const compiled = fixture.nativeElement;
            const button: HTMLButtonElement = compiled.querySelector('header div.btn-group div.btn-group a') as HTMLButtonElement;
            expect(button).toBeTruthy();
            expect(button.getAttribute('routerLink')).toBe('../electricity/purchase');
        });
    });
});
