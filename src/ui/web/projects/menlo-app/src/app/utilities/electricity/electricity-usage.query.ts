import { DateOrString, formatDate } from 'menlo-lib';

export class ElectricityUsageQuery {
    constructor(
        public readonly startDate: string,
        public readonly endDate: string | null,
        public readonly timeZone: string
    ) {
        this.startDate = startDate;
        this.endDate = endDate;
        this.timeZone = timeZone;
    }
}

export class ElectricityUsageQueryFactory {
    public static create(startDate: DateOrString, endDate?: DateOrString, timeZone?: string): ElectricityUsageQuery {
        return new ElectricityUsageQuery(
            formatDate(startDate),
            endDate !== undefined ? formatDate(endDate) : null,
            timeZone === undefined ? Intl.DateTimeFormat().resolvedOptions().timeZone : timeZone
        );
    }
}
