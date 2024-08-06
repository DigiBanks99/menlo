export function compareDates(date1: string | Date, date2: string | Date): number {
    return new Date(date1).getTime() - new Date(date2).getTime();
}
