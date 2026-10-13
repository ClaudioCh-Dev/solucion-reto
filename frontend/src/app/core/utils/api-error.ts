import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

/** Formato ProblemDetails / ValidationProblemDetails que devuelve el backend. */
interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/** Convierte cualquier error HTTP en un mensaje comprensible para el usuario. */
export function toUserMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'Ocurrió un error inesperado.';

  const problem = error.error as ProblemDetails | null;
  switch (error.status) {
    case 0:
      return 'No se pudo conectar con el servidor. Verifica que el backend esté en ejecución.';
    case HttpStatusCode.BadRequest:
      return 'Revisa los datos ingresados.';
    case HttpStatusCode.Unauthorized:
      return problem?.title ?? 'Tu sesión expiró. Inicia sesión nuevamente.';
    case HttpStatusCode.NotFound:
      return 'El recurso solicitado no existe.';
    default:
      if (error.status >= 500) return 'Error del servidor. Inténtalo de nuevo en unos minutos.';
      return problem?.detail ?? problem?.title ?? 'Ocurrió un error inesperado.';
  }
}

/**
 * Errores por campo de un 400 (ValidationProblemDetails). Las claves se pasan a camelCase
 * para que coincidan con las propiedades del formulario: "CategoriaId" → "categoriaId".
 */
export function fieldErrors(error: unknown): Record<string, string[]> {
  if (!(error instanceof HttpErrorResponse) || error.status !== HttpStatusCode.BadRequest) return {};

  const errors = (error.error as ProblemDetails | null)?.errors ?? {};
  return Object.fromEntries(
    Object.entries(errors).map(([key, messages]) => [key.charAt(0).toLowerCase() + key.slice(1), messages]),
  );
}
