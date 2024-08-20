import { ChangeDetectionStrategy, Component, computed, effect, ElementRef, input, OnInit } from '@angular/core';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef } from 'ag-grid-community';
import { ElectricityUsage } from './electricity-usage.model';
import { DatePipe } from '@angular/common';
import { DateOrString, formatDate } from 'menlo-lib';
import { Chart, ChartConfiguration, registerables, ChartData, ChartTypeRegistry, Point, BubbleDataPoint, ChartItem } from 'chart.js';

@Component({
    selector: 'menlo-electricity-usage',
    standalone: true,
    imports: [AgGridAngular],
    providers: [DatePipe],
    template: ` <div class="d-flex justify-content-center h-50">
            <canvas id="chart"></canvas>
        </div>
        <div class="h-50">
            <ag-grid-angular
                class="ag-theme-quartz ag-theme-quartz-auto-dark"
                [rowData]="electricityUsage()"
                [columnDefs]="columnDefs"
                [defaultColDef]="defaultColDef" />
        </div>`,
    styleUrl: './electricity-usage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ElectricityUsageComponent implements OnInit {
    private _chartElement: ChartItem | null = null;
    private _chartInstance: Chart | null = null;
    private _chartData = computed(() => this.getChartData(this.electricityUsage()));

    public readonly electricityUsage = input.required<ElectricityUsage[]>();

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

    public get chart(): Chart | null {
        return this._chartInstance;
    }

    constructor(
        private readonly _datePipe: DatePipe,
        private readonly _elementRef: ElementRef
    ) {
        effect(() => {
            this.updateChart();
        });
    }

    public ngOnInit(): void {
        if (this._chartElement !== null) {
            return;
        }

        this._chartElement = this._elementRef.nativeElement.querySelector('#chart');
        if (this._chartElement === null) {
            console.error('Could not find chart element');
            return;
        }
        this.createChart();
    }

    private createChart(): void {
        if (this._chartElement === null) {
            return;
        }

        Chart.register(...registerables);

        const config: ChartConfiguration = {
            type: 'line',
            data: this._chartData(),
            options: {
                responsive: true,
                plugins: {
                    title: {
                        display: true,
                        text: 'Electricity Usage'
                    }
                },
                interaction: {
                    intersect: false
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Date'
                        }
                    },
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Units'
                        }
                    }
                }
            }
        };

        this._chartInstance = new Chart(this._chartElement, config);
    }

    private updateChart(): void {
        if (this._chartInstance === null) {
            console.error('Chart instance is null');
            return;
        }

        this._chartInstance.data = this._chartData();
        this._chartInstance.update();
    }

    private getChartData(
        electricityUsage: ElectricityUsage[]
    ): ChartData<keyof ChartTypeRegistry, (number | [number, number] | Point | BubbleDataPoint | null)[], unknown> {
        const usages: number[] = electricityUsage.map(usage => usage.usage);

        return {
            labels: electricityUsage.map(usage => formatDate(usage.date)),
            datasets: [
                {
                    label: 'Electricity Usage',
                    data: usages,
                    pointStyle: 'circle',
                    pointRadius: 10
                },
                {
                    label: 'Average Usage',
                    data: usages.map(() => usages.reduce((acc, val) => acc + val, 0) / usages.length),
                    pointStyle: 'circle',
                    pointRadius: 5
                }
            ]
        };
    }
}
