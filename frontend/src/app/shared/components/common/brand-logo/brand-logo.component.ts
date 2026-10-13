import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Logo de Tejidos Alma: isotipo a la izquierda y el nombre a la derecha.
 * El isotipo es un PNG transparente usado como máscara: toma el color del texto (azul marino
 * del logo, blanco en modo oscuro). `compact` muestra solo el isotipo (sidebar colapsado).
 * `inverse` lo pone en blanco para fondos oscuros fijos (panel lateral del login).
 */
@Component({
  selector: 'app-brand-logo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="flex items-center gap-3" [class]="colorClass()">
      <span
        role="img"
        aria-label="Tejidos Alma"
        class="shrink-0 bg-current [mask:url(/images/logo/logo-icon.png)_center/contain_no-repeat]"
        [class]="iconClass()"
      ></span>
      @if (!compact()) {
        <span class="flex items-baseline gap-2 leading-none whitespace-nowrap">
          <span class="font-extrabold tracking-wide" [class]="size() === 'lg' ? 'text-3xl' : 'text-xl'">TEJIDOS</span>
          <span class="font-medium tracking-widest" [class]="size() === 'lg' ? 'text-xl' : 'text-sm'">ALMA</span>
        </span>
      }
    </span>
  `,
})
export class BrandLogoComponent {
  readonly compact = input(false);
  readonly inverse = input(false);
  readonly size = input<'md' | 'lg'>('md');

  protected readonly colorClass = computed(() => (this.inverse() ? 'text-white' : 'text-brand-logo'));
  // Proporción del isotipo: 318 × 236 (≈ 1,35 : 1).
  protected readonly iconClass = computed(() => (this.size() === 'lg' ? 'h-14 w-[76px]' : 'h-9 w-12'));
}
