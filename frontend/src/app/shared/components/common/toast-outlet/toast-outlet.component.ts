import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ToastService } from '../../../../core/services/toast.service';

/** Pila de avisos flotantes (esquina inferior). */
@Component({
  selector: 'app-toast-outlet',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="fixed z-[100000] flex flex-col gap-2 bottom-4 end-4 start-4 sm:start-auto sm:w-96" aria-live="polite">
      @for (t of toasts.toasts(); track t.id) {
        <div
          class="flex items-start gap-3 p-4 text-sm bg-white border rounded-xl shadow-theme-lg dark:bg-gray-900"
          [class.border-success-500]="t.type === 'success'"
          [class.border-error-500]="t.type === 'error'"
          [class.border-gray-200]="t.type === 'info'"
          [class.dark:border-gray-800]="t.type === 'info'"
          role="status"
        >
          <span
            class="mt-1 size-2 rounded-full shrink-0"
            [class.bg-success-500]="t.type === 'success'"
            [class.bg-error-500]="t.type === 'error'"
            [class.bg-brand-500]="t.type === 'info'"
          ></span>
          <p class="flex-1 text-heading">{{ t.message }}</p>
          <button type="button" class="text-body hover:text-heading" aria-label="Cerrar" (click)="toasts.dismiss(t.id)">✕</button>
        </div>
      }
    </div>
  `,
})
export class ToastOutletComponent {
  protected readonly toasts = inject(ToastService);
}
