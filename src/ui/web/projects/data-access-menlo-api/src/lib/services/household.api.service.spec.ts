import { HttpErrorResponse } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { HouseholdApiService } from './household.api.service';

describe('HouseholdApiService', () => {
  let service: HouseholdApiService;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [HouseholdApiService],
    });

    service = TestBed.inject(HouseholdApiService);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
  });

  it('should list households', () => {
    const mockHouseholds = {
      households: [
        { id: '1', name: 'Family A' },
        { id: '2', name: 'Family B' },
      ],
    };

    let result = mockHouseholds.households;

    service.listHouseholds().subscribe((response) => {
      result = response.households;
    });

    const req = httpController.expectOne('/api/households');
    expect(req.request.method).toBe('GET');
    req.flush(mockHouseholds);

    expect(result).toHaveLength(2);
    expect(result[0]?.name).toBe('Family A');
  });

  it('should create household', () => {
    const request = { name: 'New Family' };
    const mockResponse = { id: '3', name: 'New Family' };
    let result = mockResponse;

    service.createHousehold(request).subscribe((response) => {
      result = response;
    });

    const req = httpController.expectOne('/api/households');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(mockResponse);

    expect(result).toEqual(mockResponse);
  });

  it('should join household', () => {
    let completed = false;

    service.joinHousehold('1').subscribe(() => {
      completed = true;
    });

    const req = httpController.expectOne('/api/households/1/join');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
  });

  it('should bubble up list errors', () => {
    let error: HttpErrorResponse | undefined;

    service.listHouseholds().subscribe({
      next: () => undefined,
      error: (response: HttpErrorResponse) => {
        error = response;
      },
    });

    const req = httpController.expectOne('/api/households');
    req.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(error?.status).toBe(401);
  });
});
