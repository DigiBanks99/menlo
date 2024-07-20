import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLinkWithHref } from '@angular/router';

@Component({
    standalone: true,
    imports: [CommonModule, RouterLinkWithHref],
    template: `<header class="d-flex flex-nowrap p-0">
        <h1>Utilities</h1>
        <div class="w-100"></div>
        <div class="px-1">
            <a class="btn btn-primary text-nowrap" routerLink="../electricity">
                <span class="material-symbols-outlined align-text-bottom">bolt</span>
                Add
            </a>
        </div>
    </header>
    <article>
    </article>`,
    styleUrl: './utilities-dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class UtilitiesDashboardComponent {}
