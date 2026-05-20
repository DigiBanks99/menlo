import { ChangeDetectionStrategy, Component, computed, effect, input, signal } from '@angular/core';
import { LucideAngularModule, User } from 'lucide-angular';

export type MnlAvatarSize = 'sm' | 'md' | 'lg';

const baseClasses =
  'relative inline-flex shrink-0 select-none items-center justify-center overflow-hidden rounded-full border border-mnl-border bg-mnl-surface-alt text-mnl-subtext shadow-sm';

const sizeClasses: Record<MnlAvatarSize, string> = {
  sm: 'size-8 text-xs',
  md: 'size-10 text-sm',
  lg: 'size-14 text-lg',
};

const iconSizeClasses: Record<MnlAvatarSize, string> = {
  sm: 'size-4',
  md: 'size-5',
  lg: 'size-7',
};

@Component({
  selector: 'mnl-avatar',
  standalone: true,
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'inline-flex align-middle',
  },
  template: `
    <span
      [attr.aria-label]="showImage() ? null : accessibleLabel()"
      [attr.data-size]="size()"
      [attr.data-state]="showImage() ? 'image' : 'fallback'"
      [attr.role]="showImage() ? null : 'img'"
      [class]="avatarClasses()"
      data-testid="mnl-avatar"
    >
      @if (showImage()) {
        <img
          [attr.alt]="imageAlt()"
          [attr.src]="src()"
          class="size-full object-cover"
          data-testid="mnl-avatar-image"
          (error)="handleImageError()"
          (load)="handleImageLoad()"
        />
      } @else if (fallbackText()) {
        <span
          aria-hidden="true"
          class="font-semibold uppercase tracking-[0.08em]"
          data-testid="mnl-avatar-fallback"
        >
          {{ fallbackText() }}
        </span>
      } @else {
        <lucide-icon
          [img]="userIcon"
          [class]="iconClasses()"
          aria-hidden="true"
          data-testid="mnl-avatar-icon"
        ></lucide-icon>
      }
    </span>
  `,
})
export class MnlAvatarComponent {
  readonly alt = input('');
  readonly fallback = input('');
  readonly size = input<MnlAvatarSize>('md');
  readonly src = input('');

  private readonly imageFailed = signal(false);

  protected readonly userIcon = User;
  protected readonly avatarClasses = computed(() =>
    [baseClasses, sizeClasses[this.size()]].join(' '),
  );
  protected readonly iconClasses = computed(() =>
    ['text-current', iconSizeClasses[this.size()]].join(' '),
  );
  protected readonly fallbackText = computed(() => toFallbackText(this.fallback()));
  protected readonly accessibleLabel = computed(
    () => this.alt().trim() || this.fallbackText() || 'Avatar',
  );
  protected readonly imageAlt = computed(
    () => this.alt().trim() || this.fallbackText() || 'Avatar',
  );
  protected readonly showImage = computed(() => Boolean(this.src().trim()) && !this.imageFailed());

  constructor() {
    effect(() => {
      this.src();
      this.imageFailed.set(false);
    });
  }

  protected handleImageError(): void {
    this.imageFailed.set(true);
  }

  protected handleImageLoad(): void {
    this.imageFailed.set(false);
  }
}

function toFallbackText(value: string): string {
  const trimmedValue = value.trim();
  if (!trimmedValue) {
    return '';
  }

  const words = trimmedValue
    .split(/\s+/)
    .map((word) => word.replace(/[^A-Za-z0-9]/g, ''))
    .filter(Boolean);

  if (words.length === 0) {
    return '';
  }

  if (words.length === 1) {
    return words[0].slice(0, 2).toUpperCase();
  }

  return words
    .slice(0, 2)
    .map((word) => word[0])
    .join('')
    .toUpperCase();
}
