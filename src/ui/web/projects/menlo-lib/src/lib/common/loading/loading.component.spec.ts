import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadingComponent } from './loading.component';
import { SIGNAL, signalSetFn } from '@angular/core/primitives/signals';
import { LoadKind } from './load-kind.enum';

describe('LoadingComponent', () => {
    let component: LoadingComponent;
    let fixture: ComponentFixture<LoadingComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [LoadingComponent]
        }).compileComponents();

        fixture = TestBed.createComponent(LoadingComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render a spinning element', () => {
        const compiled = fixture.nativeElement;
        expect(compiled.querySelector('.spinner-border')).toBeTruthy();
    });

    describe('when kind is set to small', () => {
        beforeEach(() => {
            signalSetFn(component.kind[SIGNAL], LoadKind.Small);
            fixture.detectChanges();
        });

        it('should render a small spinner', () => {
            const compiled = fixture.nativeElement;
            expect(compiled.querySelector('.spinner-border-sm')).toBeTruthy();
        });
    });
});
