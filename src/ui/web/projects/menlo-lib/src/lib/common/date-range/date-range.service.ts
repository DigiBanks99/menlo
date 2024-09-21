import { Injectable, Provider } from '@angular/core';
import { DateRangeFilter, DateRangeFilterUnit } from './date-range-filter.type';
import { DateFormat, DateOrString, formatDate } from '../../types/date-or-string.type';
import { APP_BASE_HREF } from '@angular/common';

@Injectable({
    providedIn: 'root'
})
export class DateRangeService {
    public getPriorDate(snapshotDate: DateOrString, filter: DateRangeFilter, format: DateFormat = DateFormat.DateOnly): DateOrString {
        const priorDate = new Date(snapshotDate);
        switch (filter.unit) {
            case DateRangeFilterUnit.Minutes:
                if (this.isTimeApplicable(format)) {
                    priorDate.setMinutes(priorDate.getMinutes() - filter.value);
                }
                break;
            case DateRangeFilterUnit.Hours:
                if (this.isTimeApplicable(format)) {
                    priorDate.setHours(priorDate.getHours() - filter.value);
                }
                break;
            case DateRangeFilterUnit.Days:
                priorDate.setDate(priorDate.getDate() - filter.value);
                break;
            case DateRangeFilterUnit.Weeks:
                priorDate.setDate(priorDate.getDate() - filter.value * 7);
                break;
            case DateRangeFilterUnit.Months:
                priorDate.setMonth(priorDate.getMonth() - filter.value);
                break;
            case DateRangeFilterUnit.Years:
                priorDate.setFullYear(priorDate.getFullYear() - filter.value);
                break;
            default:
                throw new Error(`'${filter.unit}' is not supported for computing a prior date`);
        }
        return formatDate(priorDate, format);
    }

    public getFutureDate(snapshotDate: DateOrString, filter: DateRangeFilter, format: DateFormat = DateFormat.DateOnly): DateOrString {
        const futureDate = new Date(snapshotDate);
        switch (filter.unit) {
            case DateRangeFilterUnit.Minutes:
                if (this.isTimeApplicable(format)) {
                    futureDate.setMinutes(futureDate.getMinutes() + filter.value);
                }
                break;
            case DateRangeFilterUnit.Hours:
                if (this.isTimeApplicable(format)) {
                    futureDate.setHours(futureDate.getHours() + filter.value);
                }
                break;
            case DateRangeFilterUnit.Days:
                futureDate.setDate(futureDate.getDate() + filter.value);
                break;
            case DateRangeFilterUnit.Weeks:
                futureDate.setDate(futureDate.getDate() + filter.value * 7);
                break;
            case DateRangeFilterUnit.Months:
                futureDate.setMonth(futureDate.getMonth() + filter.value);
                break;
            case DateRangeFilterUnit.Years:
                futureDate.setFullYear(futureDate.getFullYear() + filter.value);
                break;
            default:
                throw new Error(`'${filter.unit}' is not supported for computing a future date`);
        }
        return formatDate(futureDate, format);
    }

    private isTimeApplicable(format: DateFormat): boolean {
        return format === DateFormat.ISO8601;
    }
}

export function provideDateRangeService(): Provider[] {
    return [
        DateRangeService,
        {
            provide: APP_BASE_HREF,
            useValue: '/'
        }
    ];
}

export function provideDateRangeServiceTesting(useValue = new DateRangeService()): Provider[] {
    return [
        { provide: DateRangeService, useValue },
        {
            provide: APP_BASE_HREF,
            useValue: '/'
        }
    ];
}
