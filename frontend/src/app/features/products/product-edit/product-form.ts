import { maxLength, min, required, requiredError, schema, validate } from '@angular/forms/signals';

import { Product, UpdateProductRequest } from '../models/product.models';

/**
 * Modelo del formulario de edición. Los números pueden ser null mientras el input está vacío
 * y categoriaId es string porque así lo entrega un <select> nativo.
 */
export interface ProductFormModel {
  nombre: string;
  descripcion: string;
  precio: number | null;
  stock: number | null;
  categoriaId: string;
}

export const EMPTY_PRODUCT_FORM: ProductFormModel = {
  nombre: '',
  descripcion: '',
  precio: null,
  stock: null,
  categoriaId: '',
};

/** Las mismas reglas que valida el backend, para avisar antes de enviar. */
export const productFormSchema = schema<ProductFormModel>((p) => {
  validate(p.nombre, ({ value }) =>
    value().trim() ? undefined : requiredError({ message: 'El nombre es obligatorio.' }),
  );
  maxLength(p.nombre, 200, { message: 'El nombre no puede superar 200 caracteres.' });

  maxLength(p.descripcion, 2000, { message: 'La descripción no puede superar 2000 caracteres.' });

  required(p.precio, { message: 'El precio es obligatorio.' });
  min(p.precio, 0, { message: 'El precio debe ser mayor o igual a 0.' });
  validate(p.precio, ({ value }) => {
    const precio = value();
    // Margen por la imprecisión de los decimales en coma flotante (109.95 * 100 = 10994.999…).
    return precio === null || Math.abs(precio * 100 - Math.round(precio * 100)) < 1e-6
      ? undefined
      : { kind: 'decimals', message: 'El precio admite como máximo 2 decimales.' };
  });

  required(p.stock, { message: 'El stock es obligatorio.' });
  min(p.stock, 0, { message: 'El stock debe ser mayor o igual a 0.' });
  validate(p.stock, ({ value }) => {
    const stock = value();
    return stock === null || Number.isInteger(stock)
      ? undefined
      : { kind: 'integer', message: 'El stock debe ser un número entero.' };
  });

  required(p.categoriaId, { message: 'Selecciona una categoría.' });
});

export function toFormModel(product: Product): ProductFormModel {
  return {
    nombre: product.nombre,
    descripcion: product.descripcion ?? '',
    precio: product.precio,
    stock: product.stock,
    categoriaId: String(product.categoriaId),
  };
}

/** Solo se llama con el formulario válido, así que los campos requeridos ya tienen valor. */
export function toUpdateRequest(model: ProductFormModel): UpdateProductRequest {
  const descripcion = model.descripcion.trim();
  return {
    nombre: model.nombre.trim(),
    descripcion: descripcion || null,
    precio: model.precio!,
    stock: model.stock!,
    categoriaId: Number(model.categoriaId),
  };
}
