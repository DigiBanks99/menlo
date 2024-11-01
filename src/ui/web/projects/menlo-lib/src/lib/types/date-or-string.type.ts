import { DateRangeFilterUnit } from '../common/date-range';

export type DateOrString = Date | string;
export const DateFormat = {
    DateOnly: 'DateOnly',
    ISO8601: 'ISO8601',
    ShortDisplay: 'ShortDisplay'
} as const;
export type DateFormat = (typeof DateFormat)[keyof typeof DateFormat];

export function formatDate(input: DateOrString, format: DateFormat = DateFormat.DateOnly): string {
    let dateString: string;
    if (typeof input == 'string') {
        dateString = input;
    } else {
        const intlOptions: Intl.DateTimeFormatOptions = getIntlFormatOptions(format);
        dateString = buildDateString(input, format, intlOptions);
    }

    if (format == DateFormat.ISO8601) {
        return dateString;
    }

    const parts = dateString.replace('/', '-').split('T');
    return parts.length > 1 ? parts[0] : dateString;
}

function getIntlFormatOptions(format: DateFormat): Intl.DateTimeFormatOptions {
    switch (format) {
        case DateFormat.DateOnly:
            return { year: 'numeric', month: '2-digit', day: '2-digit' };
        case DateFormat.ISO8601:
            return {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: false,
                timeZone: 'UTC'
            };
        case DateFormat.ShortDisplay:
            return { year: 'numeric', month: 'short', day: 'numeric' };
    }
}

function buildDateString(input: Date | number, format: DateFormat, intlOptions: Intl.DateTimeFormatOptions): string {
    const formatter = Intl.DateTimeFormat(Intl.DateTimeFormat().resolvedOptions().locale, intlOptions);
    const parts = formatter.formatToParts(input);

    const yearPart = parts.find(part => part.type == 'year');
    const monthPart = parts.find(part => part.type == 'month');
    const dayPart = parts.find(part => part.type == 'day');
    const hourPart = parts.find(part => part.type == 'hour');
    const minutePart = parts.find(part => part.type == 'minute');
    const secondPart = parts.find(part => part.type == 'second');

    switch (format) {
        case DateFormat.DateOnly:
            return `${yearPart?.value}-${monthPart?.value}-${dayPart?.value}`;
        case DateFormat.ISO8601:
            return `${yearPart?.value}-${monthPart?.value}-${dayPart?.value}T${hourPart?.value}:${minutePart?.value}:${secondPart?.value}Z`;
        case DateFormat.ShortDisplay:
            return `${dayPart?.value} ${monthPart?.value} ${yearPart?.value}`;
    }
}

export function getDateDiff(left: DateOrString, right: DateOrString, unit: DateRangeFilterUnit = 'days') {
    const leftDate = new Date(left);
    const rightDate = new Date(right);

    let diff: number;
    switch (unit) {
        case 'years':
            diff = leftDate.getFullYear() - rightDate.getFullYear();
            break;
        case 'months':
            diff = (leftDate.getFullYear() - rightDate.getFullYear()) * 12 + leftDate.getMonth() - rightDate.getMonth();
            break;
        case 'days':
            diff = (leftDate.getTime() - rightDate.getTime()) / (1000 * 60 * 60 * 24);
            break;
        case 'hours':
            diff = (leftDate.getTime() - rightDate.getTime()) / (1000 * 60 * 60);
            break;
        case 'minutes':
            diff = (leftDate.getTime() - rightDate.getTime()) / (1000 * 60);
            break;
        default:
            throw new Error(`Unsupported unit: ${unit}`);
    }

    return Math.ceil(Math.abs(diff));
}
