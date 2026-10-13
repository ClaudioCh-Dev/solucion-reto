import { computed, Injectable, signal } from '@angular/core';

/** Estado del sidebar (expandido, abierto en móvil, hover) con signals. */
@Injectable({ providedIn: 'root' })
export class SidebarService {
  readonly isExpanded = signal(true);
  readonly isMobileOpen = signal(false);
  readonly isHovered = signal(false);

  /** Se muestran los textos del menú (no solo los íconos). */
  readonly isWide = computed(() => this.isExpanded() || this.isHovered() || this.isMobileOpen());

  toggleExpanded(): void {
    this.isExpanded.update((v) => !v);
  }

  toggleMobileOpen(): void {
    this.isMobileOpen.update((v) => !v);
  }
}
