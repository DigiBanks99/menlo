import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { MenloLib } from './menlo-lib';

describe('MenloLib', () => {
  let component: MenloLib;
  let fixture: ComponentFixture<MenloLib>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MenloLib]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MenloLib);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
