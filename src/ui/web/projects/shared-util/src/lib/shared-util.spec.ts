import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SharedUtil } from './shared-util';

describe('SharedUtil', () => {
  let component: SharedUtil;
  let fixture: ComponentFixture<SharedUtil>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SharedUtil]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SharedUtil);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
