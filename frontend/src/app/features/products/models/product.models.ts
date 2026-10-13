/** Producto tal como lo devuelve GET /api/products y /api/products/{id}. */
export interface Product {
  id: number;
  externalId: string;
  nombre: string;
  descripcion: string | null;
  precio: number;
  stock: number;
  imagenUrl: string | null;
  categoriaId: number;
  categoriaNombre: string;
}

/** Body de PUT /api/products/{id}: solo los campos editables. */
export interface UpdateProductRequest {
  nombre: string;
  descripcion: string | null;
  precio: number;
  stock: number;
  categoriaId: number;
}

export interface Category {
  id: number;
  nombre: string;
}
