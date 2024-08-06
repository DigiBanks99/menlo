export interface ApplianceUsage {
    applianceId: number;
    hoursOfUse: number;
}

export class ElectricityUsage {
    private _applianceUsage: ApplianceUsage[] = [];
    private _date: string | undefined;
    private _units: number | undefined;

    public get date(): string {
        if (this._date === undefined) {
            throw new Error('date is required');
        }
        return this._date;
    }
    public set date(value: string) {
        this._date = value;
    }

    public get units(): number {
        if (this._units === undefined) {
            throw new Error('units is required');
        }
        return this._units;
    }
    public set units(value: number) {
        this._units = value;
    }

    public get applianceUsage(): ApplianceUsage[] {
        return this._applianceUsage;
    }
    public set applianceUsage(value: ApplianceUsage[]) {
        this._applianceUsage = value;
    }

    public getUnitDifference(from: ElectricityUsage): number {
        return from.units - this.units;
    }
}
