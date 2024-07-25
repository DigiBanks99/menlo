export type DateOrString = Date | string;

export function formatDate(input: DateOrString): string {
    return typeof input == 'string' ? input : input.toISOString();
}
