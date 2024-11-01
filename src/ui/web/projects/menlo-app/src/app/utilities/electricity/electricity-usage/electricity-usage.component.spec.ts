import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ElectricityUsageComponent } from './electricity-usage.component';
import { SIGNAL, signalSetFn } from '@angular/core/primitives/signals';
import { ElectricityUsage } from '@utilities/electricity/electricity-usage/electricity-usage.model';

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

    describe('chartData', () => {
        it('should calculate the average based on the days between the first and last usage', () => {
            const firstUsage = new ElectricityUsage();
            firstUsage.date = '2024-10-17';
            firstUsage.units = 544.8;
            firstUsage.usage = 23.35;
            firstUsage.applianceUsage = [];

            const secondUsage = new ElectricityUsage();
            secondUsage.date = '2024-10-18';
            secondUsage.units = 530.0;
            secondUsage.usage = 14.8;
            secondUsage.applianceUsage = [];

            // 2024-10-19 would be 0
            // 2024-10-20 would be 0

            const lastUsage = new ElectricityUsage();
            lastUsage.date = '2024-10-21';
            lastUsage.units = 490.65;
            lastUsage.usage = 39.35;
            lastUsage.applianceUsage = [];

            const usages = [firstUsage, secondUsage, lastUsage];
            signalSetFn(component.electricityUsage[SIGNAL], usages);

            const chartData = component.chartData();

            // 23.35 + 14.8 + 0 + 0 + 39.35 = 77.5 / 5 = 15.5
            const expectedAverage = usages.map(usage => usage.usage).reduce((acc, curr) => acc + curr, 0) / 5;
            expect(chartData.datasets[1].data).toEqual(usages.map(_ => expectedAverage));
        });
    });
});
