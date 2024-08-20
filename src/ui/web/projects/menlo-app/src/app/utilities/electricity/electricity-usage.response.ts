export interface ApplianceUsageResponse {
    applianceId: number;
    hoursOfUse: number;
}

export interface ElecricityUsageResponse {
    date: string;
    units: number;
    usage: number;
    applianceUsages: ApplianceUsageResponse[];
}
