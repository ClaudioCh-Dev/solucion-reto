import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeEs from '@angular/common/locales/es-PE';
import {
  ApplicationConfig,
  inject,
  LOCALE_ID,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { AuthStore } from './core/services/auth.store';

registerLocaleData(localeEs);

// Aplicación zoneless (por defecto en Angular 22): la detección de cambios la disparan los signals.
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    // El access token vive en memoria: al cargar la página se recupera la sesión con la cookie del
    // refresh token antes de que los guards decidan si mostrar el login o el catálogo.
    provideAppInitializer(() => inject(AuthStore).restore()),
    { provide: LOCALE_ID, useValue: 'es-PE' },
  ],
};
