import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

type LogEntry = { message: string; value?: string };

console.group('main.ts');

function updateColorScheme(event: MediaQueryListEvent) {
    const theme = event.matches ? 'dark' : 'light';

    log({
        message: 'Theme selected',
        value: theme
    });
    const html: HTMLElement = document.getElementsByTagName('html')[0];
    html.setAttribute('data-bs-theme', theme);
}

if (window.matchMedia) {
    log({
        message: 'matchMedia supported'
    });
    const colorSchemeQuery: MediaQueryList = window.matchMedia('(prefers-color-scheme: dark)');
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', updateColorScheme);
    updateColorScheme(
        new MediaQueryListEvent('change', {
            matches: colorSchemeQuery.matches
        })
    );
}

bootstrapApplication(AppComponent, appConfig).catch(err => console.error(err));

console.groupEnd();

function log(entry: LogEntry) {
    console.log(entry);
}
