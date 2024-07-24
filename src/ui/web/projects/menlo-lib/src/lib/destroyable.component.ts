import { OnDestroy } from '@angular/core';
import { Observable, Subject } from 'rxjs';

import { Component } from '@angular/core';

@Component({
    selector: 'app-destroyable',
    template: ''
})
export abstract class DestroyableComponent implements OnDestroy {
    private readonly _destroyed$ = new Subject<void>();

    public ngOnDestroy(): void {
        this._destroyed$.next();
        this._destroyed$.complete();
    }

    public get destroyed$(): Observable<void> {
        return this._destroyed$.asObservable();
    }
}
