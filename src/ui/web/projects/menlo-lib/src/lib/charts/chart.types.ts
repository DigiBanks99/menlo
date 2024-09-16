import {
    ChartData,
    ChartTypeRegistry,
    Point,
    BubbleDataPoint,
    ScaleOptionsByType,
    CartesianScaleTypeRegistry,
    RadialScaleTypeRegistry,
    LinearScaleOptions,
    RadialLinearScaleOptions
} from 'chart.js';

// DeepPartial implementation taken from the utility-types NPM package, which is
// Copyright (c) 2016 Piotr Witek <piotrek.witek@gmail.com> (http://piotrwitek.github.io)
// and used under the terms of the MIT license
export type DeepPartial<T> = T extends Function
    ? T
    : T extends Array<infer U>
      ? _DeepPartialArray<U>
      : T extends object
        ? _DeepPartialObject<T>
        : T | undefined;

type _DeepPartialArray<T> = Array<DeepPartial<T>>;
type _DeepPartialObject<T> = { [P in keyof T]?: DeepPartial<T[P]> };

export type MenloChartData = ChartData<keyof ChartTypeRegistry, (number | [number, number] | Point | BubbleDataPoint | null)[], unknown>;

export type MenloChartLinearScale = DeepPartial<Record<string, LinearScaleOptions & RadialLinearScaleOptions>>;

export type MenloScaleRegistry = MenloChartLinearScale;
