export interface ApplianceUsage {
    applianceId: number;
    hoursOfUse: number;
}

export interface ElectricityUsage {
    date: string;
    units: number;
    applianceUsage: ApplianceUsage[];
}
