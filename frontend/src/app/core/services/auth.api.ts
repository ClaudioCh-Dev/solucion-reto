import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../config/api.config';
import { LoginRequest, SessionResponse } from '../models/auth.models';

/**
 * Acceso HTTP a /api/auth. Sin estado: la sesión vive en AuthStore.
 * El navegador envía y recibe la cookie del refresh token automáticamente (mismo origen gracias al proxy).
 */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  login(request: LoginRequest): Observable<SessionResponse> {
    return this.http.post<SessionResponse>(`${this.baseUrl}/auth/login`, request);
  }

  /** Usa la cookie del refresh token para obtener un access token nuevo (y rota la cookie). */
  refresh(): Observable<SessionResponse> {
    return this.http.post<SessionResponse>(`${this.baseUrl}/auth/refresh`, null);
  }

  /** Revoca el refresh token en el servidor y borra la cookie. */
  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/auth/logout`, null);
  }
}
