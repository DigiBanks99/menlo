import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { DataAccessMenloApi } from './data-access-menlo-api';

describe('DataAccessMenloApi', () => {
  let component: DataAccessMenloApi;
  let fixture: ComponentFixture<DataAccessMenloApi>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DataAccessMenloApi]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DataAccessMenloApi);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
