import { HttpErrorResponse, HttpInterceptorFn, HttpRequest, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';

import { API_BASE_URL } from '../config/api.config';
import { AuthStore } from '../services/auth.store';

/**
 * - Agrega "Authorization: Bearer <access token>" a las peticiones al backend.
 * - Si el backend responde 401 (access token vencido), pide uno nuevo con /refresh y reintenta una vez.
 *   Si el refresh también falla, la sesión terminó: vuelve al login.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthStore);
  const baseUrl = inject(API_BASE_URL);

  if (!req.url.startsWith(baseUrl)) return next(req);

  // login, refresh y logout usan la cookie, no el access token; sus 401 los maneja AuthStore.
  const isSessionEndpoint = ['login', 'refresh', 'logout'].some((p) => req.url === `${baseUrl}/auth/${p}`);
  if (isSessionEndpoint) return next(req);

  const withToken = (request: HttpRequest<unknown>) => {
    const token = auth.accessToken();
    return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
  };

  return next(withToken(req)).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== HttpStatusCode.Unauthorized) {
        return throwError(() => error);
      }
      return auth.refresh().pipe(
        switchMap((ok) => {
          if (ok) return next(withToken(req));
          auth.logout('expired');
          return throwError(() => error);
        }),
      );
    }),
  );
};
