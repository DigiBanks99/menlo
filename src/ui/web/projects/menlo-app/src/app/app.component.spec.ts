import { TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { routes } from './app.routes';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should have the root component as its entry point', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const rootComponent = fixture.nativeElement.querySelector('menlo-layout-root');
    expect(rootComponent).toBeTruthy();
  });

  it('should map the root routes to the nav-items of the root component', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    for (const a of app.navItems) {
        const route = routes.find(r => r.path === a.route);
        expect(route).toBeTruthy();
    }

    expect(app.navItems.map(a => a.route)).not.toContain('');
  });
});
