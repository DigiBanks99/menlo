import {
    AfterViewInit,
    ChangeDetectionStrategy,
    Component,
    computed,
    effect,
    ElementRef,
    input,
    InputSignal,
    viewChild,
    ViewEncapsulation
} from '@angular/core';
import { Chart, ChartConfiguration, ChartItem, ChartType, registerables } from 'chart.js';
import { MenloChartData, MenloScaleRegistry } from './chart.types';

@Component({
    selector: 'menlo-chart',
    standalone: true,
    template: '<canvas #chart></canvas>',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChartComponent implements AfterViewInit {
    public readonly data = input.required<MenloChartData>();
    public readonly title = input<string>();
    public readonly type = input.required<ChartType>();
    public readonly scales: InputSignal<MenloScaleRegistry | undefined> = input<MenloScaleRegistry>();

    private readonly _chartData = computed(() => this.data());
    private readonly _chartElement = viewChild.required<ElementRef<ChartItem>>('chart');
    private _chartInstance: Chart | null = null;

    public get chart(): Chart | null {
        return this._chartInstance;
    }

    constructor() {
        effect(() => {
            if (this.chart == null) {
                return;
            }
            this.updateChart(this._chartData());
        });
    }

    ngAfterViewInit(): void {
        this._chartInstance = this.createChart();
    }

    private createChart(): Chart | null {
        if (this._chartElement() == null) {
            console.error('Chart element is null');
            return null;
        }

        Chart.register(...registerables);

        const config: ChartConfiguration = {
            type: this.type(),
            data: this.data(),
            options: {
                responsive: true,
                plugins: {
                    title: {
                        display: true,
                        text: this.title()
                    }
                },
                interaction: {
                    intersect: false
                },
                scales: this.scales()
            }
        };

        return new Chart(this._chartElement().nativeElement, config);
    }

    private updateChart(data: MenloChartData): void {
        if (this._chartInstance === null) {
            console.error('Chart instance is null');
            return;
        }

        this._chartInstance.data = data;
        this._chartInstance.update();
    }
}
