export interface HouseholdDto {
  id: string;
  name: string;
}

export interface CreateHouseholdRequest {
  name: string;
}

export interface ListHouseholdsResponse {
  households: HouseholdDto[];
}
