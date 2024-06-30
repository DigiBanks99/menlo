import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MenloLibComponent } from './menlo-lib.component';

describe('MenloLibComponent', () => {
  let component: MenloLibComponent;
  let fixture: ComponentFixture<MenloLibComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MenloLibComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MenloLibComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
