import { TestBed } from '@angular/core/testing';

import { MenloLibService } from './menlo-lib.service';

describe('MenloLibService', () => {
  let service: MenloLibService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MenloLibService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
