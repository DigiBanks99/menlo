import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElectricityUsageComponent } from './electricity-usage.component';

describe('ElectricityUsageComponent', () => {
    let component: ElectricityUsageComponent;
    let fixture: ComponentFixture<ElectricityUsageComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ElectricityUsageComponent]
        }).compileComponents();

        fixture = TestBed.createComponent(ElectricityUsageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have electricityUsage set to an empty array by default', () => {
        expect(component.electricityUsage).toBeTruthy([]);
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
});
