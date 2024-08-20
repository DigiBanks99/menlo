export type DateOrString = Date | string;

export function formatDate(input: DateOrString): string {
    const formatter = Intl.DateTimeFormat();
    const dateString = typeof input == 'string' ? input : formatter.format(input);
    const parts = dateString.split('T');
    return parts.length > 1 ? parts[0] : dateString;
}
