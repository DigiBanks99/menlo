import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'mnl-form-field',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block w-full',
  },
  template: `
    <div [attr.data-has-error]="hasError()" class="space-y-2" data-testid="mnl-form-field">
      <label
        [attr.for]="inputId() || null"
        class="block text-xs font-semibold text-mnl-text"
        data-testid="mnl-form-field-label"
      >
        <span>{{ label() }}</span>

        @if (required()) {
          <span aria-hidden="true" class="text-mnl-red"> *</span>
        }
      </label>

      <div class="w-full">
        <ng-content></ng-content>
      </div>

      @if (hint()) {
        <p class="m-0 text-xs leading-5 text-mnl-subtext" data-testid="mnl-form-field-hint">
          {{ hint() }}
        </p>
      }

      @if (error()) {
        <p
          class="m-0 text-xs font-medium leading-5 text-mnl-red"
          [attr.data-testid]="errorTestId()"
        >
          {{ error() }}
        </p>
      }
    </div>
  `,
})
export class MnlFormFieldComponent {
  readonly error = input<string | null>(null);
  readonly errorTestId = input('mnl-form-field-error');
  readonly hint = input('');
  readonly inputId = input('');
  readonly label = input.required<string>();
  readonly required = input(false);

  protected readonly hasError = computed(() => Boolean(this.error()));
}
