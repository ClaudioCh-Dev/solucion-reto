import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, signal } from '@angular/core';

import { AuthStore } from '../../../../core/services/auth.store';

@Component({
  selector: 'app-user-dropdown',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './user-dropdown.component.html',
  host: { '(document:click)': 'onDocumentClick($event)' },
})
export class UserDropdownComponent {
  protected readonly auth = inject(AuthStore);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly isOpen = signal(false);

  /** Nombre corto a partir del email: "demo@entrevista.local" → "demo". */
  protected readonly userName = computed(() => this.auth.email()?.split('@')[0] ?? '');
  protected readonly initials = computed(() => this.userName().slice(0, 2).toUpperCase());

  protected toggleDropdown(event: Event): void {
    event.stopPropagation();
    this.isOpen.update((open) => !open);
  }

  protected logout(): void {
    this.isOpen.set(false);
    this.auth.logout();
  }

  protected onDocumentClick(event: MouseEvent): void {
    if (this.isOpen() && !this.elementRef.nativeElement.contains(event.target as Node)) {
      this.isOpen.set(false);
    }
  }
}
