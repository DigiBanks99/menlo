import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ElectricityUsageComponent } from './electricity-usage.component';
import { SIGNAL, signalSetFn } from '@angular/core/primitives/signals';

describe('ElectricityUsageComponent', () => {
    let component: ElectricityUsageComponent;
    let fixture: ComponentFixture<ElectricityUsageComponent>;

    beforeAll(async () => {
        await TestBed.configureTestingModule({
            imports: [ElectricityUsageComponent]
        }).compileComponents();
    });

    beforeEach(async () => {
        fixture = TestBed.createComponent(ElectricityUsageComponent);
        component = fixture.componentInstance;
        signalSetFn(component.electricityUsage[SIGNAL], []);
        signalSetFn(component.loading[SIGNAL], false);
        await fixture.whenStable();

        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have electricityUsage set to an empty array by default', () => {
        expect(component.electricityUsage()).toEqual([]);
    });

    it('should have columnDefs set to an array with two elements', () => {
        expect(component.columnDefs).toBeTruthy();
        expect(component.columnDefs.length).toBe(2);
        expect(component.columnDefs[0].field).toBe('date');
        expect(component.columnDefs[0].headerName).toBe('Date');
        expect(component.columnDefs[1].field).toBe('units');
        expect(component.columnDefs[1].headerName).toBe('Units');
    });

    it('should have defaultColDef set to an object with a flex property', () => {
        expect(component.defaultColDef).toBeTruthy();
        expect(component.defaultColDef.flex).toBe(1);
    });

    it('should render the ag-grid-angular component', () => {
        const agGridElement = fixture.nativeElement.querySelector('ag-grid-angular');
        expect(agGridElement).toBeTruthy();
    });

    it('should render the chart element', () => {
        const chartElement = fixture.nativeElement.querySelector('menlo-chart');
        expect(chartElement).toBeTruthy();
    });

    describe('when loading is true', () => {
        beforeEach(async () => {
            signalSetFn(component.loading[SIGNAL], true);
            await fixture.whenStable();
            fixture.detectChanges();
        });

        it('should render the loading component', () => {
            const loadingElement = fixture.nativeElement.querySelector('menlo-loading');
            expect(loadingElement).toBeTruthy();
        });

        it('should not render the chart element', () => {
            const chartElement = fixture.nativeElement.querySelector('menlo-chart');
            expect(chartElement).toBeFalsy();
        });

        it('should not render the ag-grid-angular component', () => {
            const agGridElement = fixture.nativeElement.querySelector('ag-grid-angular');
            expect(agGridElement).toBeFalsy();
        });
    });
});
