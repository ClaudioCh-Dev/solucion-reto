import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Lo mínimo que necesita del estado de un campo de Signal Forms (ej. productForm.nombre()). */
interface FieldErrorsState {
  touched(): boolean;
  errors(): readonly { message?: string }[];
}

/** Muestra los errores de un campo cuando el usuario ya interactuó con él (o intentó guardar). */
@Component({
  selector: 'app-field-errors',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @for (message of messages(); track message) {
      <p class="mt-1.5 text-xs text-error-500" role="alert">{{ message }}</p>
    }
  `,
})
export class FieldErrorsComponent {
  readonly state = input.required<FieldErrorsState>();

  protected readonly messages = computed(() => {
    const state = this.state();
    if (!state.touched()) return [];
    return [...new Set(state.errors().map((e) => e.message ?? 'Valor inválido.'))];
  });
}
