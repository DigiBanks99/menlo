import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, ViewEncapsulation } from '@angular/core';
import NavItem from '../../nav-item.model';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'menlo-layout-root',
    standalone: true,
    imports: [CommonModule, RouterModule],
    template: `<header class="navbar sticky-top bg-primary flex-md-nowrap shadow p-0">
            <a class="navbar-brand col-md-3 col-lg-2 me-0 px-3">Menlo</a>
            <button
                class="navbar-toggler position-absolute d-md-none collapsed"
                type="button"
                data-bs-toggle="collapse"
                data-bs-target="#sidebarMenu"
                aria-controls="sidebarMenu"
                aria-expanded="false"
                aria-label="Toggle navigation">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="w-100"></div>
            <div class="navbar-nav">
                <div class="nav-item text-nowrap">
                    <a class="nav-link px-3" href="/signout">Signout</a>
                </div>
            </div>
        </header>
        <div class="container-fluid">
            <div class="row">
                <nav id="sideBarMenu" class="col-md-3 col-lg-2 d-md-block bg-light sidebar collapse">
                    <div class="position-sticky sidebar-sticky">
                        <ul class="nav flex-column">
                            @for (navItem of navItems; track $index) {
                                <li class="nav-item">
                                    <a class="nav-link" [href]="navItem.route">
                                        <span class="material-symbols-outlined feather feather-home align-text-bottom"> {{ navItem.iconName }} </span>
                                        {{ navItem.description }}
                                    </a>
                                </li>
                            }
                        </ul>
                    </div>
                </nav>
                <main class="col-md-9 ms-sm-auto col-lg-10 px-md-4">
                    <router-outlet />
                </main>
            </div>
        </div>`,
    styleUrl: './root.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None
})
export class RootComponent {
    @Input() public navItems: NavItem[] = [];
}
