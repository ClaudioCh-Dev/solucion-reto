import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { form, FormField, submit } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { fieldErrors, toUserMessage } from '../../../core/utils/api-error';
import { ToastService } from '../../../core/services/toast.service';
import { ComponentCardComponent } from '../../../shared/components/common/component-card/component-card.component';
import { PageBreadcrumbComponent } from '../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';
import { FieldErrorsComponent } from '../../../shared/components/form/field-errors/field-errors.component';
import { InputFieldComponent } from '../../../shared/components/form/input/input-field.component';
import { TextAreaComponent } from '../../../shared/components/form/input/text-area.component';
import { LabelComponent } from '../../../shared/components/form/label/label.component';
import { Option, SelectComponent } from '../../../shared/components/form/select/select.component';
import { AlertComponent } from '../../../shared/components/ui/alert/alert.component';
import { ICONS } from '../../../shared/icons/icons';
import { SafeHtmlPipe } from '../../../shared/pipe/safe-html.pipe';
import { Product } from '../models/product.models';
import { ProductsApi } from '../services/products.api';
import { EMPTY_PRODUCT_FORM, ProductFormModel, productFormSchema, toFormModel, toUpdateRequest } from './product-form';

@Component({
  selector: 'app-product-edit-page',
  imports: [
    AlertComponent,
    ComponentCardComponent,
    CurrencyPipe,
    FieldErrorsComponent,
    FormField,
    InputFieldComponent,
    LabelComponent,
    PageBreadcrumbComponent,
    RouterLink,
    SafeHtmlPipe,
    SelectComponent,
    TextAreaComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-edit.page.html',
})
export class ProductEditPage {
  private readonly api = inject(ProductsApi);
  private readonly toast = inject(ToastService);
  protected readonly icons = ICONS;

  /** Parámetro :id de la ruta (withComponentInputBinding). */
  readonly id = input.required<string>();

  protected readonly productId = computed(() => {
    const id = Number(this.id());
    return Number.isInteger(id) && id > 0 ? id : undefined;
  });

  /** Producto desde la base local. Se vuelve a pedir si cambia el :id. */
  protected readonly product = rxResource({
    params: () => this.productId(),
    stream: ({ params: id }) => this.api.get(id),
  });

  /** Categorías locales (importadas antes desde Fake Store API): no se consulta la API externa. */
  protected readonly categories = rxResource({
    stream: () => this.api.categories(),
    defaultValue: [],
  });

  protected readonly categoryOptions = computed<Option[]>(() =>
    this.categories.hasValue() ? this.categories.value().map((c) => ({ value: String(c.id), label: c.nombre })) : [],
  );

  /** Modelo del formulario: se reinicia con los datos del producto cada vez que este cambia. */
  private readonly model = linkedSignal<ProductFormModel>(() =>
    this.product.hasValue() ? toFormModel(this.product.value()) : EMPTY_PRODUCT_FORM,
  );

  protected readonly productForm = form(this.model, productFormSchema);

  protected readonly loadError = computed(() => {
    if (this.productId() === undefined) return 'El producto solicitado no existe.';
    const error = this.product.error() ?? this.categories.error();
    return error ? toUserMessage(error) : null;
  });

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    const id = this.productId();
    if (id === undefined) return;

    await submit(this.productForm, async () => {
      try {
        const updated = await firstValueFrom(this.api.update(id, toUpdateRequest(this.model())));
        this.applySaved(updated);
        this.toast.success('Cambios guardados correctamente.');
        return undefined;
      } catch (error) {
        return this.toSubmitErrors(error);
      }
    });
  }

  /** Descarta los cambios no guardados y vuelve a los datos de la base. */
  protected discard(): void {
    if (this.product.hasValue()) this.productForm().reset(toFormModel(this.product.value()));
  }

  private applySaved(updated: Product): void {
    this.product.set(updated); // el linkedSignal recalcula el modelo con los datos guardados
    this.productForm().reset();
  }

  /** Errores de validación del backend → error en cada campo. Otros errores → aviso flotante. */
  private toSubmitErrors(error: unknown) {
    const fields = {
      nombre: this.productForm.nombre,
      descripcion: this.productForm.descripcion,
      precio: this.productForm.precio,
      stock: this.productForm.stock,
      categoriaId: this.productForm.categoriaId,
    };

    const errors = Object.entries(fieldErrors(error)).flatMap(([key, messages]) => {
      if (!(key in fields)) return [];
      const fieldTree = fields[key as keyof typeof fields];
      return messages.map((message) => ({ kind: 'server', message, fieldTree }));
    });

    if (errors.length === 0) this.toast.error(`No se guardaron los cambios. ${toUserMessage(error)}`);
    return errors;
  }
}
