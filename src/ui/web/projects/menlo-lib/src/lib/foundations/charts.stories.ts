import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
} from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';
import {
  ChartComponent,
  type ApexAxisChartSeries,
  type ApexFill,
  type ApexLegend,
  type ApexNonAxisChartSeries,
  type ApexPlotOptions,
  type ApexTooltip,
  type ApexXAxis,
  type ApexYAxis,
} from 'ng-apexcharts';

import { MnlButtonComponent } from '../atoms/button';
import {
  createMnlChartOptions,
  MNL_CHART_PALETTE,
  type MnlChartOptions,
  ThemeService,
  type Theme,
} from '../theme';

interface ChartStoryCard {
  readonly chartLabel: string;
  readonly chartDescription: string;
  readonly colors: readonly string[];
  readonly chart: MnlChartOptions['chart'];
  readonly fill?: ApexFill;
  readonly labels?: string[];
  readonly legend?: ApexLegend;
  readonly plotOptions?: ApexPlotOptions;
  readonly series: ApexAxisChartSeries | ApexNonAxisChartSeries;
  readonly stroke?: MnlChartOptions['stroke'];
  readonly tooltip?: ApexTooltip;
  readonly xaxis?: ApexXAxis;
  readonly yaxis?: ApexYAxis;
}

@Component({
  selector: 'lib-foundations-charts-story',
  standalone: true,
  imports: [ChartComponent, MnlButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <div class="flex flex-wrap items-start justify-between gap-4">
            <div class="space-y-2">
              <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
                Foundations
              </p>
              <h1 class="m-0 text-3xl font-bold tracking-tight">Charts</h1>
              <p class="m-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
                ng-apexcharts inherits Menlo's Catppuccin palette through a reactive chart token and
                shared default options for typography, grid, tooltip, and axis styling.
              </p>
            </div>

            <mnl-button variant="secondary" (pressed)="toggleTheme()">
              Toggle {{ currentTheme() === 'light' ? 'dark' : 'light' }} mode
            </mnl-button>
          </div>
        </section>

        <section class="grid gap-4 md:grid-cols-3">
          <article class="rounded-2xl bg-mnl-surface p-5 shadow-sm ring-1 ring-mnl-border">
            <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-mnl-subtext">
              Active theme
            </p>
            <p class="mt-3 mb-0 text-2xl font-bold tracking-tight capitalize">
              {{ currentTheme() }}
            </p>
          </article>

          <article class="rounded-2xl bg-mnl-surface p-5 shadow-sm ring-1 ring-mnl-border">
            <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-mnl-subtext">
              Palette colors
            </p>
            <div class="mt-3 flex flex-wrap gap-2">
              @for (color of palette.colors(); track color) {
                <span
                  class="size-6 rounded-full ring-1 ring-black/10"
                  [style.backgroundColor]="color"
                ></span>
              }
            </div>
          </article>

          <article class="rounded-2xl bg-mnl-surface p-5 shadow-sm ring-1 ring-mnl-border">
            <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-mnl-subtext">
              Shared options
            </p>
            <p class="mt-3 mb-0 text-sm leading-6 text-mnl-subtext">
              Nunito Sans, theme-aware tooltip mode, and subdued axis/grid tokens come from
              createMnlChartOptions().
            </p>
          </article>
        </section>

        <div class="grid gap-6 xl:grid-cols-2">
          @for (chartCard of chartCards(); track chartCard.chartLabel) {
            <article class="rounded-2xl bg-mnl-surface p-5 shadow-sm ring-1 ring-mnl-border">
              <div class="space-y-2">
                <p class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-accent">
                  {{ chartCard.chartLabel }}
                </p>
                <h2 class="m-0 text-xl font-semibold capitalize">
                  {{ chartCard.chart.type }} chart
                </h2>
                <p class="m-0 text-sm leading-6 text-mnl-subtext">
                  {{ chartCard.chartDescription }}
                </p>
              </div>

              <div class="mt-5 rounded-2xl bg-mnl-surface-alt/50 p-3 ring-1 ring-mnl-border">
                <apx-chart
                  [chart]="chartCard.chart"
                  [colors]="chartCard.colors"
                  [fill]="chartCard.fill"
                  [labels]="chartCard.labels"
                  [legend]="chartCard.legend"
                  [plotOptions]="chartCard.plotOptions"
                  [series]="chartCard.series"
                  [stroke]="chartCard.stroke"
                  [tooltip]="chartCard.tooltip"
                  [xaxis]="chartCard.xaxis"
                  [yaxis]="chartCard.yaxis"
                />
              </div>
            </article>
          }
        </div>
      </div>
    </div>
  `,
})
class FoundationsChartsStoryComponent {
  readonly themeMode = input<Theme>('light');

  private readonly themeService = inject(ThemeService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly currentTheme = this.themeService.currentTheme;
  protected readonly palette = inject(MNL_CHART_PALETTE);
  protected readonly chartCards = computed<readonly ChartStoryCard[]>(() => {
    const colors = this.palette.colors();
    const baseOptions = createMnlChartOptions(this.currentTheme());

    return [
      createBarCard(baseOptions, colors),
      createLineCard(baseOptions, colors),
      createDonutCard(baseOptions, colors),
      createRadialBarCard(baseOptions, colors),
    ];
  });

  private readonly initialTheme = this.themeService.currentTheme();

  constructor() {
    effect(
      () => {
        this.themeService.setTheme(this.themeMode());
      },
      { allowSignalWrites: true },
    );

    this.destroyRef.onDestroy(() => {
      this.themeService.setTheme(this.initialTheme);
    });
  }

  protected toggleTheme(): void {
    this.themeService.toggle();
  }
}

const meta: Meta<FoundationsChartsStoryComponent> = {
  title: 'Foundations/Charts',
  component: FoundationsChartsStoryComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<FoundationsChartsStoryComponent>;

export const Overview: Story = {
  args: {
    themeMode: 'light',
  },
};

export const DarkMode: Story = {
  args: {
    themeMode: 'dark',
  },
};

function createBarCard(baseOptions: MnlChartOptions, colors: readonly string[]): ChartStoryCard {
  return {
    chart: {
      ...baseOptions.chart,
      height: 320,
      type: 'bar',
    },
    chartDescription:
      'Monthly envelope allocations use rounded bars with Menlo axis and tooltip styling.',
    chartLabel: 'Allocation',
    colors: colors.slice(0, 4),
    legend: {
      ...baseOptions.legend,
      show: false,
    },
    plotOptions: {
      bar: {
        borderRadius: 12,
        columnWidth: '56%',
      },
    },
    series: [
      {
        data: [18500, 9400, 12800, 5100],
        name: 'Allocated',
      },
    ],
    tooltip: {
      ...baseOptions.tooltip,
      y: {
        formatter: (value: number) => formatCurrency(value),
      },
    },
    xaxis: {
      ...baseOptions.xaxis,
      categories: ['Housing', 'Groceries', 'School', 'Transport'],
    },
    yaxis: {
      ...baseOptions.yaxis,
      title: {
        ...baseOptions.yaxis.title,
        text: 'Rand',
      },
    },
  };
}

function createLineCard(baseOptions: MnlChartOptions, colors: readonly string[]): ChartStoryCard {
  return {
    chart: {
      ...baseOptions.chart,
      height: 320,
      type: 'line',
    },
    chartDescription:
      'Trend charts reuse the shared font, grid, and tooltip defaults while highlighting the Catppuccin accent lane.',
    chartLabel: 'Trend',
    colors: colors.slice(0, 2),
    fill: {
      type: 'gradient',
      gradient: {
        gradientToColors: [colors[1]],
        opacityFrom: 0.35,
        opacityTo: 0.05,
        stops: [0, 90, 100],
      },
    },
    legend: {
      ...baseOptions.legend,
      position: 'top',
    },
    series: [
      {
        data: [12450, 13120, 13880, 14960, 15510, 16240],
        name: 'Spent',
      },
    ],
    stroke: {
      ...baseOptions.stroke,
      width: 4,
    },
    tooltip: {
      ...baseOptions.tooltip,
      y: {
        formatter: (value: number) => formatCurrency(value),
      },
    },
    xaxis: {
      ...baseOptions.xaxis,
      categories: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
    },
    yaxis: {
      ...baseOptions.yaxis,
      title: {
        ...baseOptions.yaxis.title,
        text: 'Spend',
      },
    },
  };
}

function createDonutCard(baseOptions: MnlChartOptions, colors: readonly string[]): ChartStoryCard {
  return {
    chart: {
      ...baseOptions.chart,
      height: 320,
      type: 'donut',
    },
    chartDescription:
      'Category breakdowns use the reactive Menlo palette while keeping labels legible in both themes.',
    chartLabel: 'Breakdown',
    colors: colors.slice(0, 4),
    labels: ['Housing', 'Food', 'School', 'Transport'],
    legend: {
      ...baseOptions.legend,
      position: 'bottom',
    },
    plotOptions: {
      pie: {
        donut: {
          labels: {
            name: {
              color: 'var(--mnl-color-subtext)',
              fontFamily: baseOptions.chart.fontFamily,
              show: true,
            },
            show: true,
            total: {
              color: 'var(--mnl-color-text)',
              fontFamily: baseOptions.chart.fontFamily,
              fontSize: '15px',
              fontWeight: 700,
              label: 'Budget mix',
              show: true,
            },
            value: {
              color: 'var(--mnl-color-text)',
              fontFamily: baseOptions.chart.fontFamily,
              show: true,
            },
          },
          size: '68%',
        },
      },
    },
    series: [42, 28, 18, 12],
    tooltip: {
      ...baseOptions.tooltip,
      y: {
        formatter: (value: number) => `${value}%`,
      },
    },
  };
}

function createRadialBarCard(
  baseOptions: MnlChartOptions,
  colors: readonly string[],
): ChartStoryCard {
  return {
    chart: {
      ...baseOptions.chart,
      height: 320,
      type: 'radialBar',
    },
    chartDescription:
      'Single-metric radial bars reuse the same palette token while the track follows Menlo surface colors.',
    chartLabel: 'Progress',
    colors: [colors[0]],
    plotOptions: {
      radialBar: {
        dataLabels: {
          name: {
            color: 'var(--mnl-color-subtext)',
            fontFamily: baseOptions.chart.fontFamily,
            show: true,
          },
          total: {
            color: 'var(--mnl-color-subtext)',
            fontFamily: baseOptions.chart.fontFamily,
            fontSize: '14px',
            fontWeight: 600,
            formatter: () => 'This month',
            label: 'Cycle',
            show: true,
          },
          value: {
            color: 'var(--mnl-color-text)',
            fontFamily: baseOptions.chart.fontFamily,
            fontSize: '32px',
            fontWeight: 700,
            formatter: (value: number) => `${value}%`,
            show: true,
          },
        },
        hollow: {
          size: '58%',
        },
        track: {
          background: 'var(--mnl-color-surface-alt)',
          strokeWidth: '100%',
        },
      },
    },
    series: [72],
    tooltip: {
      ...baseOptions.tooltip,
      y: {
        formatter: (value: number) => `${value}% budget used`,
      },
    },
  };
}

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-ZA', {
    currency: 'ZAR',
    maximumFractionDigits: 0,
    style: 'currency',
  }).format(value);
}
