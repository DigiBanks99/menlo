import { ChangeDetectionStrategy, Component, EventEmitter, output } from '@angular/core';

@Component({
    selector: 'menlo-form-buttons',
    standalone: true,
    imports: [],
    template: `<div class="d-flex flex-nowrap flex-gap">
        <button class="btn btn-secondary" type="reset" (click)="cancel.emit()">Cancel</button>
        <div class="w-100"></div>
        <button class="btn btn-primary" type="submit" (click)="submit.emit()">Submit</button>
    </div>`,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormButtonsComponent {
    public readonly submit = output<void>();
    public readonly cancel = output<void>();
}
