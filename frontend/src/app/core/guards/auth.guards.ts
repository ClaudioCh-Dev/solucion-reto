import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthStore } from '../services/auth.store';

/** Solo usuarios autenticados. Si no hay sesión, redirige al login. */
export const authGuard: CanActivateFn = () =>
  inject(AuthStore).isAuthenticated() || inject(Router).createUrlTree(['/login']);

/** Solo usuarios sin sesión (pantalla de login). Si ya hay sesión, va al catálogo. */
export const guestGuard: CanActivateFn = () =>
  !inject(AuthStore).isAuthenticated() || inject(Router).createUrlTree(['/products']);
