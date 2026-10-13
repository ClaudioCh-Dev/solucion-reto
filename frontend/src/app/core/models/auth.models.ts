export interface LoginRequest {
  email: string;
  password: string;
}

/**
 * Respuesta de POST /api/auth/login y /api/auth/refresh.
 * El refresh token no viene aquí: el backend lo deja en una cookie HttpOnly.
 */
export interface SessionResponse {
  accessToken: string;
  /** Vencimiento del access token en UTC (ISO 8601). */
  expiraEn: string;
  usuario: { id: number; email: string };
}

/** Sesión en memoria. El access token nunca se guarda en localStorage ni en cookies. */
export interface Session {
  accessToken: string;
  email: string;
  /** Vencimiento del access token en milisegundos (epoch). */
  expiresAt: number;
}
