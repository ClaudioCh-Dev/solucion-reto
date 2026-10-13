import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

type BadgeVariant = 'light' | 'solid';
type BadgeSize = 'sm' | 'md';
type BadgeColor = 'primary' | 'success' | 'error' | 'warning' | 'info' | 'light' | 'dark';

// Nombres literales para que Tailwind detecte las utilidades de styles/gestion.css.
const SIZES: Record<BadgeSize, string> = {
  sm: 'badge-sm',
  md: 'badge-md',
};

const COLORS: Record<BadgeVariant, Record<BadgeColor, string>> = {
  light: {
    primary: 'badge-primary',
    success: 'badge-success',
    error: 'badge-error',
    warning: 'badge-warning',
    info: 'badge-info',
    light: 'badge-light',
    dark: 'badge-dark',
  },
  solid: {
    primary: 'badge-solid-primary',
    success: 'badge-solid-success',
    error: 'badge-solid-error',
    warning: 'badge-solid-warning',
    info: 'badge-solid-info',
    light: 'badge-solid-light',
    dark: 'badge-solid-dark',
  },
};

@Component({
  selector: 'app-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex' },
  template: `<span [class]="classes()"><ng-content></ng-content></span>`,
})
export class BadgeComponent {
  readonly variant = input<BadgeVariant>('light');
  readonly size = input<BadgeSize>('md');
  readonly color = input<BadgeColor>('primary');

  protected readonly classes = computed(
    () => `badge ${SIZES[this.size()]} ${COLORS[this.variant()][this.color()]}`,
  );
}
