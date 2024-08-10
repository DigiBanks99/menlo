import { DateOrString, formatDate } from 'menlo-lib';

export class ElectricityPurchaseRequest {
    constructor(
        public date: string,
        public units: number,
        public cost: number
    ) {
        this.date = date;
        this.units = units;
        this.cost = cost;
    }
}

export class ElectricityPurchaseRequestFactory {
    public static create(date: DateOrString, units: number, cost: number): ElectricityPurchaseRequest {
        return new ElectricityPurchaseRequest(formatDate(date), units, cost);
    }
}
