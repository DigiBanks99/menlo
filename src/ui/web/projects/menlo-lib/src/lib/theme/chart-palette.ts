import { InjectionToken, computed, inject, type Signal } from '@angular/core';
import type {
  ApexChart,
  ApexDataLabels,
  ApexFill,
  ApexGrid,
  ApexLegend,
  ApexNoData,
  ApexOptions,
  ApexStroke,
  ApexTheme,
  ApexTooltip,
  ApexXAxis,
  ApexYAxis,
} from 'ng-apexcharts';

import { ThemeService, type Theme } from './theme.service';

interface MnlChartThemeTokens {
  readonly border: string;
  readonly subtext: string;
  readonly text: string;
  readonly themeMode: NonNullable<ApexTheme['mode']>;
}

export interface MnlChartPalette {
  readonly colors: Signal<readonly string[]>;
}

export interface MnlChartOptions {
  readonly chart: Partial<ApexChart>;
  readonly dataLabels: ApexDataLabels;
  readonly fill: ApexFill;
  readonly grid: ApexGrid;
  readonly legend: ApexLegend;
  readonly noData: ApexNoData;
  readonly stroke: ApexStroke;
  readonly theme: ApexTheme;
  readonly tooltip: ApexTooltip;
  readonly xaxis: ApexXAxis;
  readonly yaxis: ApexYAxis;
}

export const MNL_CHART_FONT_FAMILY = "'Nunito Sans', ui-sans-serif, system-ui, sans-serif";

export const MNL_LIGHT_CHART_COLORS = [
  '#ea76cb',
  '#8839ef',
  '#7287fd',
  '#fe640b',
  '#40a02b',
  '#df8e1d',
  '#dc8a78',
  '#d20f39',
] as const;

export const MNL_DARK_CHART_COLORS = [
  '#f5c2e7',
  '#cba6f7',
  '#b4befe',
  '#fab387',
  '#a6e3a1',
  '#f9e2af',
  '#f5e0dc',
  '#f38ba8',
] as const;

const chartThemeTokens: Record<Theme, MnlChartThemeTokens> = {
  light: {
    border: '#bcc0cc',
    subtext: '#6c6f85',
    text: '#4c4f69',
    themeMode: 'light',
  },
  dark: {
    border: '#6c7086',
    subtext: '#a6adc8',
    text: '#cdd6f4',
    themeMode: 'dark',
  },
};

export const MNL_CHART_PALETTE = new InjectionToken<MnlChartPalette>('MNL_CHART_PALETTE', {
  providedIn: 'root',
  factory: () => {
    const themeService = inject(ThemeService);

    return {
      colors: computed(() => getMnlChartColors(themeService.currentTheme())),
    };
  },
});

export function getMnlChartColors(theme: Theme): readonly string[] {
  return theme === 'dark' ? MNL_DARK_CHART_COLORS : MNL_LIGHT_CHART_COLORS;
}

export function createMnlChartOptions(theme: Theme): MnlChartOptions {
  const tokens = chartThemeTokens[theme];

  return {
    chart: {
      animations: {
        dynamicAnimation: {
          enabled: true,
          speed: 220,
        },
        enabled: true,
        speed: 300,
      },
      background: 'transparent',
      fontFamily: MNL_CHART_FONT_FAMILY,
      foreColor: tokens.text,
      toolbar: {
        show: false,
      },
      zoom: {
        enabled: false,
      },
    },
    dataLabels: {
      enabled: false,
      style: {
        fontFamily: MNL_CHART_FONT_FAMILY,
      },
    },
    fill: {
      opacity: 0.9,
    },
    grid: {
      borderColor: tokens.border,
      padding: {
        bottom: 0,
        left: 8,
        right: 12,
        top: 8,
      },
      strokeDashArray: 4,
      xaxis: {
        lines: {
          show: false,
        },
      },
    },
    legend: {
      fontFamily: MNL_CHART_FONT_FAMILY,
      fontSize: '13px',
      fontWeight: 600,
      itemMargin: {
        horizontal: 12,
        vertical: 4,
      },
      labels: {
        colors: tokens.text,
      },
    },
    noData: {
      style: {
        color: tokens.subtext,
        fontFamily: MNL_CHART_FONT_FAMILY,
        fontSize: '14px',
      },
      text: 'No data',
    },
    stroke: {
      curve: 'smooth',
      lineCap: 'round',
      width: 3,
    },
    theme: {
      mode: tokens.themeMode,
    },
    tooltip: {
      style: {
        fontFamily: MNL_CHART_FONT_FAMILY,
        fontSize: '13px',
      },
      theme: tokens.themeMode,
      x: {
        show: true,
      },
    },
    xaxis: {
      axisBorder: {
        show: false,
      },
      axisTicks: {
        color: tokens.border,
        show: false,
      },
      crosshairs: {
        stroke: {
          color: tokens.border,
          dashArray: 4,
          width: 1,
        },
      },
      labels: {
        style: {
          colors: tokens.subtext,
          fontFamily: MNL_CHART_FONT_FAMILY,
          fontSize: '12px',
          fontWeight: 600,
        },
      },
      tooltip: {
        enabled: false,
      },
    },
    yaxis: {
      labels: {
        style: {
          colors: tokens.subtext,
          fontFamily: MNL_CHART_FONT_FAMILY,
          fontSize: '12px',
          fontWeight: 600,
        },
      },
      title: {
        style: {
          color: tokens.subtext,
          fontFamily: MNL_CHART_FONT_FAMILY,
          fontSize: '12px',
          fontWeight: 600,
        },
      },
    },
  };
}
