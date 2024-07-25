import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef } from 'ag-grid-community';
import { ElectricityUsage } from './electricity-usage.model';
import { DatePipe } from '@angular/common';
import { DateOrString } from 'menlo-lib';

@Component({
    selector: 'menlo-electricity-usage',
    standalone: true,
    imports: [AgGridAngular],
    providers: [DatePipe],
    template: ` <ag-grid-angular class="ag-theme-quartz" [rowData]="electricityUsage" [columnDefs]="columnDefs" [defaultColDef]="defaultColDef" />`,
    styleUrl: './electricity-usage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ElectricityUsageComponent {
    @Input() public electricityUsage: ElectricityUsage[] = [];

    constructor(private readonly _datePipe: DatePipe) {}

    public readonly columnDefs: ColDef[] = [
        {
            field: 'date',
            headerName: 'Date',
            cellRenderer: (params: { value: DateOrString }) => this._datePipe.transform(params.value, 'dd MMM YYYY')
        },
        { field: 'units', headerName: 'Units', type: 'numericColumn' }
    ];

    public readonly defaultColDef: ColDef = {
        flex: 1
    };
}
