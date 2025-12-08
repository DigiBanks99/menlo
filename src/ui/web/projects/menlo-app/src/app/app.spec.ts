import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { App } from './app';

// Mock the MenloLib component to avoid injection issues
@Component({
  selector: 'lib-menlo-lib',
  template: '<p>Mock MenloLib component</p>',
  standalone: true
})
class MockMenloLib {}

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App, MockMenloLib],
      providers: [
        provideZonelessChangeDetection()
      ]
    })
    .overrideComponent(App, {
      set: {
        imports: [MockMenloLib],
        template: `
          <main class="main">
            <div class="content">
              <div class="left-side">
                <h1>Hello, {{ title() }}</h1>
                <p>Congratulations! Your app is running. 🎉</p>
                <lib-menlo-lib></lib-menlo-lib>
              </div>
            </div>
          </main>
        `
      }
    })
    .compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render title', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Hello, menlo-app');
  });
});
