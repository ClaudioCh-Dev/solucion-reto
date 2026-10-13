import { InjectionToken } from '@angular/core';

/**
 * URL base del backend. Por defecto es relativa ("/api"): en desarrollo el proxy de Angular
 * (proxy.conf.json) la reenvía a http://localhost:5054, así no hace falta CORS.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});
