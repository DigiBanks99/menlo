import { Pipe, PipeTransform } from '@angular/core';
import { Money, MoneyUtils } from 'shared-util';

/**
 * Angular pipe for formatting Money values in templates.
 *
 * @example
 * ```html
 * <!-- Display Money with default locale -->
 * <span>{{ plannedAmount | money }}</span>
 *
 * <!-- Display Money with custom locale -->
 * <span>{{ plannedAmount | money:'en-US' }}</span>
 * ```
 */
@Pipe({
  name: 'money',
  standalone: true,
})
export class MoneyPipe implements PipeTransform {
  transform(value: Money | null | undefined, locale?: string): string {
    if (!value) {
      return '';
    }
    return MoneyUtils.format(value, locale);
  }
}
