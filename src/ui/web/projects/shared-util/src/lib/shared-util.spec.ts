import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { SharedUtil } from './shared-util';

describe('SharedUtil', () => {
  let component: SharedUtil;
  let fixture: ComponentFixture<SharedUtil>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SharedUtil],
    });
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SharedUtil);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render default message', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('shared-util works!');
  });
});
