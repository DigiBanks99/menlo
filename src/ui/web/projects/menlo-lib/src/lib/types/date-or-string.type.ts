export type DateOrString = Date | string;
export enum DateFormat {
    DateOnly,
    ISO8601,
    ShortDisplay
}

export function formatDate(input: DateOrString, format: DateFormat = DateFormat.DateOnly): string {
    let dateString: string;
    if (typeof input == 'string') {
        dateString = input;
    } else {
        const intlOptions: Intl.DateTimeFormatOptions = getIntlFormatOptions(format);
        const formatter = Intl.DateTimeFormat(Intl.DateTimeFormat().resolvedOptions().locale, intlOptions);
        const parts = formatter.formatToParts(input);
        dateString = buildDateString(parts, format);
    }
    const parts = dateString.replace('/', '-').split('T');
    return parts.length > 1 ? parts[0] : dateString;
}

function getIntlFormatOptions(format: DateFormat): Intl.DateTimeFormatOptions {
    switch (format) {
        case DateFormat.DateOnly:
            return { year: 'numeric', month: '2-digit', day: '2-digit' };
        case DateFormat.ISO8601:
            return { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' };
        case DateFormat.ShortDisplay:
            return { year: 'numeric', month: 'short', day: 'numeric' };
    }
}

function buildDateString(parts: Intl.DateTimeFormatPart[], format: DateFormat): string {
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
            return 'TODO';
        case DateFormat.ShortDisplay:
            return `${dayPart?.value} ${monthPart?.value} ${yearPart?.value}`;
    }
}
