import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/guards/auth.guards';
import { AppLayoutComponent } from './shared/layout/app-layout/app-layout.component';

const titulo = (pagina: string) => `${pagina} | Quality Products`;

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: titulo('Iniciar sesión'),
    loadComponent: () => import('./features/auth/sign-in/sign-in.page').then((m) => m.SignInPage),
  },
  {
    path: '',
    component: AppLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'products' },
      {
        path: 'products',
        title: titulo('Productos'),
        loadComponent: () =>
          import('./features/products/product-list/product-list.page').then((m) => m.ProductListPage),
      },
      {
        path: 'products/:id',
        title: titulo('Editar producto'),
        loadComponent: () =>
          import('./features/products/product-edit/product-edit.page').then((m) => m.ProductEditPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
