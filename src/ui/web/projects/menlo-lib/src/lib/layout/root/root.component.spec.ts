import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RootComponent } from './root.component';
import { provideRouter } from '@angular/router';

describe('RootComponent', () => {
    let component: RootComponent;
    let fixture: ComponentFixture<RootComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [RootComponent],
            providers: [provideRouter([])]
        });
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(RootComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have the app title in the header', () => {
        const title = fixture.nativeElement.querySelector('header.navbar > a.navbar-brand');
        expect(title.textContent).toContain('Menlo');
    });

    it('should have a signout link in the header', () => {
        const signout = fixture.nativeElement.querySelector('header.navbar div.navbar-nav a.nav-link');
        expect(signout.textContent).toContain('Signout');
    });

    it('should have a sidebar menu', () => {
        const sidebar = fixture.nativeElement.querySelector('nav#sideBarMenu');
        expect(sidebar).toBeTruthy();
    });

    it('should have a main content area', () => {
        const main = fixture.nativeElement.querySelector('main.main-content');
        expect(main).toBeTruthy();
    });

    it('should have a router outlet in the main content area', () => {
        const routerOutlet = fixture.nativeElement.querySelector('main.main-content router-outlet');
        expect(routerOutlet).toBeTruthy();
    });

    it('should have a list of nav items in the sidebar', () => {
        fixture = TestBed.createComponent(RootComponent);
        component = fixture.componentInstance;
        component.navItems = [
            { route: '/home', description: 'Home', iconName: 'home', alternateText: 'Home' },
            { route: '/about', description: 'About', iconName: 'info', alternateText: 'About' }
        ];
        fixture.detectChanges();
        const navItems = fixture.nativeElement.querySelectorAll('nav#sideBarMenu ul.nav li.nav-item');
        expect(navItems.length).toBe(2);

        const aItems: HTMLCollectionOf<HTMLLinkElement> = fixture.nativeElement.querySelectorAll('nav#sideBarMenu ul.nav li.nav-item a');
        for (let i = 0; i < aItems.length; i++) {
            const aItem = aItems[i];
            expect(aItem.textContent).toContain(component.navItems[i].description);
            expect(aItem.href).toContain(component.navItems[i].route);
            expect(aItem.querySelector('span')?.textContent).toContain(component.navItems[i].iconName);
        }
    });
});
