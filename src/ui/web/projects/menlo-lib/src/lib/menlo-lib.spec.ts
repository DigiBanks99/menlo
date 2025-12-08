import type { Signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { MenloApiClient, WeatherForecast } from 'data-access-menlo-api';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MenloLib } from './menlo-lib';

// Mock the MenloApiClient
const mockMenloApiClient: MenloApiClient = {
  forecasts: vi.fn(() => null) as unknown as Signal<WeatherForecast[] | null>,
  loadWeather: vi.fn(),
} as any;

// Mock the data-access-menlo-api module
vi.mock('data-access-menlo-api', () => ({
  MenloApiClient: vi.fn(() => mockMenloApiClient),
}));

describe('MenloLib', () => {
  let component: MenloLib;
  let fixture: ComponentFixture<MenloLib>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MenloLib],
      providers: [
        { provide: 'MenloApiClient', useValue: mockMenloApiClient }
      ]
    })
    .overrideComponent(MenloLib, {
      set: {
        providers: [
          { provide: 'MenloApiClient', useValue: mockMenloApiClient }
        ]
      }
    })
    .compileComponents();

    fixture = TestBed.createComponent(MenloLib);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have forecasts property', () => {
    expect(component.forecasts).toBeDefined();
  });
});
