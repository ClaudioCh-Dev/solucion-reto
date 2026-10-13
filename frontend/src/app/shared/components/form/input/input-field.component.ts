import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import { FormValueControl } from '@angular/forms/signals';

/**
 * Input con el estilo de TailAdmin. Implementa FormValueControl: se enlaza con Signal Forms
 * usando [formField], que le pasa el valor y el estado (disabled, invalid, touched).
 * Con type="number" el valor es number (o null si está vacío).
 */
@Component({
  selector: 'app-input-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative">
      <input
        [type]="type()"
        [id]="inputId()"
        [name]="name()"
        [placeholder]="placeholder()"
        #input
        [attr.min]="min()"
        [attr.step]="step()"
        [attr.autocomplete]="autocomplete()"
        [disabled]="disabled()"
        [readOnly]="readonly()"
        [attr.aria-invalid]="showError()"
        [class]="inputClasses()"
        (input)="onInput()"
        (blur)="touch.emit()"
      />
      @if (hint()) {
        <p
          class="mt-1.5 text-xs"
          [class]="{ 'text-error-500': showError(), 'text-success-500': success(), 'text-gray-500': !showError() && !success() }"
        >
          {{ hint() }}
        </p>
      }
    </div>
  `,
})
export class InputFieldComponent implements FormValueControl<string | number | null> {
  readonly value = model<string | number | null>('');
  readonly touched = input(false);
  readonly touch = output<void>();
  readonly disabled = input(false);
  readonly readonly = input(false);
  readonly invalid = input(false);

  readonly type = input<string>('text');
  // No se llama "id": Angular también dejaría ese atributo en el elemento host y el id quedaría duplicado.
  readonly inputId = input<string>('');
  readonly name = input<string>('');
  readonly placeholder = input<string>('');
  readonly min = input<string | number>();
  readonly step = input<string | number>();
  readonly autocomplete = input<string>();
  readonly success = input(false);
  readonly hint = input<string>('');
  readonly className = input<string>('');

  /** Solo se marca en rojo cuando el usuario ya pasó por el campo (o intentó guardar). */
  protected readonly showError = computed(() => this.invalid() && this.touched());

  protected readonly inputClasses = computed(() => {
    const base = `h-11 w-full rounded-lg border appearance-none px-4 py-2.5 text-sm shadow-theme-xs placeholder:text-gray-400 focus:outline-hidden focus:ring-3 dark:bg-gray-900 dark:text-white/90 dark:placeholder:text-white/30 ${this.className()}`;
    if (this.disabled() || this.readonly()) {
      return `${base} text-gray-500 border-gray-300 bg-gray-100 cursor-not-allowed dark:bg-gray-800 dark:text-gray-400 dark:border-gray-700`;
    }
    if (this.showError()) {
      return `${base} border-error-500 focus:border-error-300 focus:ring-error-500/20 dark:text-error-400 dark:border-error-500 dark:focus:border-error-800`;
    }
    if (this.success()) {
      return `${base} border-success-500 focus:border-success-300 focus:ring-success-500/20 dark:text-success-400 dark:border-success-500 dark:focus:border-success-800`;
    }
    return `${base} bg-transparent text-gray-800 border-gray-300 focus:border-brand-300 focus:ring-brand-500/20 dark:border-gray-700 dark:text-white/90 dark:focus:border-brand-400`;
  });

  private readonly inputRef = viewChild.required<ElementRef<HTMLInputElement>>('input');

  constructor() {
    // Modelo → DOM solo si el valor cambió desde fuera (carga, descartar, reset). Si se escribiera
    // siempre, al teclear "2." (que el navegador informa como vacío) se borraría lo que se está escribiendo.
    effect(() => {
      const value = this.value();
      const element = this.inputRef().nativeElement;
      if (this.parse(element) !== value) element.value = value == null ? '' : String(value);
    });
  }

  protected onInput(): void {
    this.value.set(this.parse(this.inputRef().nativeElement));
  }

  /** Con type="number" el valor es number, o null si el campo está vacío o incompleto. */
  private parse(element: HTMLInputElement): string | number | null {
    if (this.type() !== 'number') return element.value;
    return element.value === '' ? null : element.valueAsNumber;
  }
}
