import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ChartComponent } from './chart.component';
import { SIGNAL, signalSetFn } from '@angular/core/primitives/signals';
import { MenloChartLinearScale } from './chart.types';
import { RadialLinearScaleOptions } from 'chart.js';

describe('ChartComponent', () => {
    let component: ChartComponent;
    let fixture: ComponentFixture<ChartComponent>;
    let data = { labels: ['A', 'B', 'C'], datasets: [{ data: [1, 2, 3] }] };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ChartComponent]
        }).compileComponents();

        fixture = TestBed.createComponent(ChartComponent);
        component = fixture.componentInstance;
        signalSetFn(component.type[SIGNAL], 'line');
        signalSetFn(component.data[SIGNAL], data);
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should contain a canvas with a chart query tag', () => {
        const compiled = fixture.nativeElement;
        expect(compiled.querySelector('canvas')).toBeTruthy();
    });

    describe('chart', () => {
        it('should not be null', () => {
            expect(component.chart).toBeTruthy();
        });

        it('should have no title if not set', () => {
            expect(component.chart?.options.plugins?.title?.text).toBe('');
        });

        it('should have a title if set', () => {
            fixture = TestBed.createComponent(ChartComponent);
            component = fixture.componentInstance;
            signalSetFn(component.type[SIGNAL], 'line');
            signalSetFn(component.data[SIGNAL], data);
            signalSetFn(component.title[SIGNAL], 'The Title');
            fixture.detectChanges();
            expect(component.chart?.options.plugins?.title?.text).toBe('The Title');
        });

        it('should have the initial data set', () => {
            expect(component.chart?.data.datasets).toEqual(data.datasets);
        });

        it('should update the chart when the data changes', () => {
            const newData = { labels: ['A', 'B', 'C'], datasets: [{ data: [4, 5, 6] }] };
            signalSetFn(component.data[SIGNAL], newData);
            fixture.detectChanges();
            expect(component.chart?.data.datasets).toEqual(newData.datasets);
        });

        it('should have the default scales', () => {
            const actualScale = component.chart?.options.scales! as MenloChartLinearScale;
            expect(actualScale).not.toBeNull();

            const x = actualScale['x']!;
            expect(x.beginAtZero).toBeFalse();
            expect(x.title?.text).toBe('');
            expect(x.title?.display).toBeFalse();

            const y = actualScale['y']!;
            expect(y.beginAtZero).toBeFalse();
            expect(y.title?.text).toBe('');
            expect(y.title?.display).toBeFalse();
        });

        it('should configure the scales if set', () => {
            const scales: MenloChartLinearScale = {
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
            };
            fixture = TestBed.createComponent(ChartComponent);
            component = fixture.componentInstance;
            signalSetFn(component.type[SIGNAL], 'line');
            signalSetFn(component.data[SIGNAL], data);
            signalSetFn(component.scales[SIGNAL], scales);
            fixture.detectChanges();

            const actualScale = component.chart?.options.scales! as MenloChartLinearScale;
            expect(actualScale).not.toBeNull();

            const x = actualScale['x']!;
            expect(x.beginAtZero).toBeTrue();
            expect(x.title?.text).toBe('Date');
            expect(x.title?.display).toBeTrue();

            const y = actualScale['y']!;
            expect(y.beginAtZero).toBeTrue();
            expect(y.title?.text).toBe('Units');
            expect(y.title?.display).toBeTrue();
        });
    });
});
