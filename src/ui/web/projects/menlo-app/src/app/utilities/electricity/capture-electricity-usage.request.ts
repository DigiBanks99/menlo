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
    public static create(date: Date | string, units: number, applianceUsages?: ApplianceUsageInfo[]): CaptureElectricityUsageRequest {
        return new CaptureElectricityUsageRequest(typeof date == 'string' ? date : date.toISOString(), units, applianceUsages);
    }
}
