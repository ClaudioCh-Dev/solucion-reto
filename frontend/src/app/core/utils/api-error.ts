import { HttpErrorResponse } from '@angular/common/http';

/** Códigos de error del backend (Common/Errores/ErroresApi.cs). Son estables: no cambian si cambia el texto. */
export type ApiErrorCode =
  | 'validacion'
  | 'no_autenticado'
  | 'credenciales_invalidas'
  | 'sesion_expirada'
  | 'sin_permiso'
  | 'no_encontrado'
  | 'producto_no_encontrado'
  | 'metodo_no_permitido'
  | 'conflicto'
  | 'importacion_en_conflicto'
  | 'tipo_no_soportado'
  | 'error_interno'
  | 'servicio_externo'
  | 'fakestore_no_disponible';

/**
 * Formato de TODOS los errores del backend: ProblemDetails (RFC 9457) con dos campos propios.
 * - title: mensaje en español listo para mostrar. detail: más contexto, cuando lo hay.
 * - code: para decidir en el código. traceId: identifica la petición en el log del servidor.
 * - errors: solo en 400, mensajes por campo con las claves en camelCase (las del formulario).
 */
export interface ApiProblem {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  code?: ApiErrorCode;
  traceId?: string;
  errors?: Record<string, string[]>;
}

/** El ProblemDetails del backend, o null si el error no lo trae (sin conexión, proxy caído, error de JS…). */
export function apiProblem(error: unknown): ApiProblem | null {
  if (!(error instanceof HttpErrorResponse)) return null;
  const body = error.error as Partial<ApiProblem> | null;
  return body && typeof body === 'object' && typeof body.title === 'string' ? (body as ApiProblem) : null;
}

export function errorCode(error: unknown): ApiErrorCode | null {
  return apiProblem(error)?.code ?? null;
}

/** Convierte cualquier error en un mensaje para el usuario. Prefiere el texto que manda el backend. */
export function toUserMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'Ocurrió un error inesperado.';
  if (error.status === 0) return 'No se pudo conectar con el servidor. Verifica que el backend esté en ejecución.';

  const problem = apiProblem(error);
  if (problem) {
    const message = problem.detail ?? problem.title;
    // En errores del servidor se agrega la referencia para buscar el detalle en el log.
    return problem.status >= 500 && problem.traceId ? `${message} (Ref.: ${traceRef(problem.traceId)})` : message;
  }

  // Respuesta que no viene del backend (ej. el proxy de desarrollo no pudo conectar con él).
  return error.status >= 500
    ? 'El servidor no está disponible. Inténtalo de nuevo en unos minutos.'
    : 'Ocurrió un error inesperado.';
}

/** Errores por campo de un 400 de validación. Las claves ya coinciden con las del formulario. */
export function fieldErrors(error: unknown): Record<string, string[]> {
  const problem = apiProblem(error);
  return problem?.code === 'validacion' ? (problem.errors ?? {}) : {};
}

/** "00-<trace id>-<span id>-00" (formato W3C) → solo el trace id, que es lo que aparece en el log. */
function traceRef(traceId: string): string {
  return traceId.split('-')[1] ?? traceId;
}
