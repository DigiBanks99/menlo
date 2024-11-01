import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef } from 'ag-grid-community';
import { ElectricityUsage } from './electricity-usage.model';
import { DatePipe } from '@angular/common';
import {
    ChartComponent,
    DateFormat,
    DateOrString,
    DateRangeService,
    formatDate, getDateDiff,
    LoadingComponent,
    MenloChartData,
    MenloChartLinearScale
} from 'menlo-lib';

@Component({
    selector: 'menlo-electricity-usage',
    standalone: true,
    imports: [AgGridAngular, ChartComponent, LoadingComponent],
    providers: [DatePipe],
    template: ` @if (loading()) {
            <menlo-loading />
        } @else {
            <div class="electricity-usage">
                <div class="electricity-usage__chart">
                    <menlo-chart [data]="chartData()" title="Electricity Usage" type="line" [scales]="chartScales" />
                </div>
                <div class="h-50">
                    <ag-grid-angular
                        class="ag-theme-quartz ag-theme-quartz-auto-dark"
                        [rowData]="electricityUsage()"
                        [columnDefs]="columnDefs"
                        [defaultColDef]="defaultColDef" />
                </div>
            </div>
        }`,
    styleUrl: './electricity-usage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ElectricityUsageComponent {
    public readonly electricityUsage = input.required<ElectricityUsage[]>();
    public readonly loading = input.required<boolean>();

    public readonly chartData = computed(() => this.getChartData(this.electricityUsage()));

    public readonly chartScales: MenloChartLinearScale = {
        x: {
            title: {
                display: true,
                text: 'Date'
            }
        },
        y: {
            title: {
                display: true,
                text: 'Usage'
            }
        }
    };

    public readonly columnDefs: ColDef[] = [
        {
            field: 'date',
            headerName: 'Date',
            cellRenderer: (params: { value: DateOrString }) => formatDate(params.value, DateFormat.ShortDisplay)
        },
        { field: 'units', headerName: 'Units', type: 'numericColumn' }
    ];

    public readonly defaultColDef: ColDef = {
        flex: 1
    };

    private getChartData(electricityUsage: ElectricityUsage[]): MenloChartData {
        const usages: number[] = electricityUsage.map(usage => usage.usage);
        const days = electricityUsage.length > 0 ? getDateDiff(electricityUsage[0].date, electricityUsage[1].date) : 1;

        return {
            labels: electricityUsage.map(usage => formatDate(usage.date, DateFormat.ShortDisplay)),
            datasets: [
                {
                    label: 'Electricity Usage',
                    data: usages,
                    pointStyle: 'circle',
                    pointRadius: 10
                },
                {
                    label: 'Average Usage',
                    data: usages.map(() => usages.reduce((acc, val) => acc + val, 0) / days),
                    pointStyle: 'circle',
                    pointRadius: 5
                }
            ]
        };
    }
}
