import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, finalize, firstValueFrom, map, Observable, of, shareReplay, tap } from 'rxjs';

import { LoginRequest, Session, SessionResponse } from '../models/auth.models';
import { AuthApi } from './auth.api';

export type LogoutReason = 'manual' | 'expired';

/** Se renueva el access token este tiempo antes de que venza. */
const REFRESH_MARGIN_MS = 60_000;

/**
 * Estado de la sesión con signals: única fuente de verdad sobre si el usuario está autenticado.
 *
 * - Access token: solo en memoria (este signal). Se pierde al recargar la página.
 * - Refresh token: cookie HttpOnly que gestiona el navegador; JavaScript no puede leerla.
 *   Al arrancar la app y antes de que venza el access token se llama a /refresh para obtener uno nuevo.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);
  private readonly router = inject(Router);

  private readonly session = signal<Session | null>(null);
  private refreshTimer?: ReturnType<typeof setTimeout>;
  private refreshInFlight$?: Observable<boolean>;

  readonly accessToken = computed(() => this.session()?.accessToken ?? null);
  readonly email = computed(() => this.session()?.email ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.refreshTimer));
  }

  login(request: LoginRequest): Observable<void> {
    const email = request.email.trim().toLowerCase();
    return this.api.login({ email, password: request.password }).pipe(
      tap((response) => this.setSession(response)),
      map(() => undefined),
    );
  }

  /**
   * Pide un access token nuevo con la cookie del refresh token. Devuelve false si la sesión ya no es válida.
   * Si varias peticiones lo piden a la vez, comparten la misma llamada: el backend rota el refresh
   * token en cada uso, y dos llamadas simultáneas con el mismo token se tomarían como reúso.
   */
  refresh(): Observable<boolean> {
    this.refreshInFlight$ ??= this.api.refresh().pipe(
      tap((response) => this.setSession(response)),
      map(() => true),
      catchError(() => {
        this.setSession(null);
        return of(false);
      }),
      finalize(() => (this.refreshInFlight$ = undefined)),
      shareReplay(1),
    );
    return this.refreshInFlight$;
  }

  /** Al arrancar la app: recupera la sesión si la cookie del refresh token sigue vigente. */
  async restore(): Promise<void> {
    await firstValueFrom(this.refresh());
  }

  /** Revoca el refresh token en el servidor, borra la sesión del cliente y vuelve al login. */
  logout(reason: LogoutReason = 'manual'): void {
    this.api.logout().subscribe({ error: () => undefined }); // si falla, la cookie vence sola
    this.setSession(null);
    void this.router.navigate(['/login'], {
      queryParams: reason === 'expired' ? { expired: 1 } : {},
    });
  }

  private setSession(response: SessionResponse | null): void {
    this.session.set(
      response && {
        accessToken: response.accessToken,
        email: response.usuario.email,
        expiresAt: Date.parse(response.expiraEn),
      },
    );
    this.scheduleRefresh();
  }

  /** Renueva el access token un minuto antes de que venza. Si no se puede, la sesión terminó. */
  private scheduleRefresh(): void {
    clearTimeout(this.refreshTimer);
    const session = this.session();
    if (!session) return;

    const delay = Math.max(session.expiresAt - Date.now() - REFRESH_MARGIN_MS, 0);
    this.refreshTimer = setTimeout(() => {
      this.refresh().subscribe((ok) => {
        if (!ok) this.logout('expired');
      });
    }, delay);
  }
}
