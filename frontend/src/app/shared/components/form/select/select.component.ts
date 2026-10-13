import { ChangeDetectionStrategy, Component, computed, input, model, output } from '@angular/core';
import { FormValueControl } from '@angular/forms/signals';

export interface Option {
  value: string;
  label: string;
}

/** Select con el estilo de TailAdmin. Se enlaza con Signal Forms usando [formField]. */
@Component({
  selector: 'app-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './select.component.html',
})
export class SelectComponent implements FormValueControl<string> {
  readonly value = model('');
  readonly touched = input(false);
  readonly touch = output<void>();
  readonly disabled = input(false);
  readonly invalid = input(false);

  // No se llama "id": Angular también dejaría ese atributo en el elemento host y el id quedaría duplicado.
  readonly inputId = input<string>('');
  readonly options = input<Option[]>([]);
  readonly placeholder = input<string>('Selecciona una opción');
  readonly className = input<string>('');

  protected readonly showError = computed(() => this.invalid() && this.touched());

  protected readonly selectClasses = computed(() => {
    const base = `h-11 w-full appearance-none rounded-lg border bg-transparent px-4 py-2.5 pe-11 text-sm shadow-theme-xs focus:outline-hidden focus:ring-3 dark:bg-gray-900 ${this.className()}`;
    const text = this.value() ? 'text-gray-800 dark:text-white/90' : 'text-gray-400 dark:text-gray-400';
    const border = this.showError()
      ? 'border-error-500 focus:border-error-300 focus:ring-error-500/10 dark:border-error-500'
      : 'border-gray-300 focus:border-brand-300 focus:ring-brand-500/10 dark:border-gray-700 dark:focus:border-brand-400';
    return `${base} ${text} ${border}`;
  });

  protected onChange(event: Event): void {
    this.value.set((event.target as HTMLSelectElement).value);
    this.touch.emit();
  }
}
