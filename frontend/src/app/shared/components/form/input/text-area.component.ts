import { ChangeDetectionStrategy, Component, computed, input, model, output } from '@angular/core';
import { FormValueControl } from '@angular/forms/signals';

/** Textarea con el estilo de TailAdmin. Se enlaza con Signal Forms usando [formField]. */
@Component({
  selector: 'app-text-area',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative">
      <textarea
        [id]="inputId()"
        [placeholder]="placeholder()"
        [rows]="rows()"
        [value]="value()"
        [disabled]="disabled()"
        [attr.aria-invalid]="showError()"
        [class]="textareaClasses()"
        (input)="value.set($any($event.target).value)"
        (blur)="touch.emit()"
      ></textarea>
      @if (hint()) {
        <p class="mt-2 text-sm" [class]="showError() ? 'text-error-500' : 'text-gray-500 dark:text-gray-400'">
          {{ hint() }}
        </p>
      }
    </div>
  `,
})
export class TextAreaComponent implements FormValueControl<string> {
  readonly value = model('');
  readonly touched = input(false);
  readonly touch = output<void>();
  readonly disabled = input(false);
  readonly invalid = input(false);

  // No se llama "id": Angular también dejaría ese atributo en el elemento host y el id quedaría duplicado.
  readonly inputId = input<string>('');
  readonly placeholder = input<string>('');
  readonly rows = input(3);
  readonly hint = input<string>('');
  readonly className = input<string>('');

  protected readonly showError = computed(() => this.invalid() && this.touched());

  protected readonly textareaClasses = computed(() => {
    const base = `w-full rounded-lg border px-4 py-2.5 text-sm shadow-theme-xs focus:outline-hidden ${this.className()} `;
    if (this.disabled()) {
      return base + 'bg-gray-100 opacity-50 text-gray-500 border-gray-300 cursor-not-allowed dark:bg-gray-800 dark:text-gray-400 dark:border-gray-700';
    }
    if (this.showError()) {
      return base + 'bg-transparent text-gray-900 border-error-500 focus:border-error-300 focus:ring-3 focus:ring-error-500/10 dark:border-error-500 dark:bg-gray-900 dark:text-white/90 dark:focus:border-error-800';
    }
    return base + 'bg-transparent text-gray-900 border-gray-300 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90 dark:focus:border-brand-400';
  });
}
