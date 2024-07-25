import { DateOrString, formatDate } from 'menlo-lib';

export interface ApplianceUsageInfo {
    applianceId: number;
    hoursOfUse: number;
}

export class CaptureElectricityUsageRequest {
    public applianceUsages: ApplianceUsageInfo[];

    constructor(
        public date: string,
        public units: number,
        applianceUsages?: ApplianceUsageInfo[]
    ) {
        this.date = date;
        this.units = units;
        this.applianceUsages = applianceUsages ?? [];
    }

    public addApplianceUsage(applianceId: number, hoursOfUse: number): void {
        this.applianceUsages.push({ applianceId, hoursOfUse });
    }
}

export class CaptureElectricityUsageRequestFactory {
    public static create(date: DateOrString, units: number, applianceUsages?: ApplianceUsageInfo[]): CaptureElectricityUsageRequest {
        return new CaptureElectricityUsageRequest(formatDate(date), units, applianceUsages);
    }
}
