# Frontend · Tejidos Alma

Angular 22 (standalone, zoneless, signals) con Tailwind CSS 4. La documentación completa del proyecto está en el [README principal](../README.md).

## Requisitos

- Node.js 22.x y npm.
- El backend corriendo en http://localhost:5054 (ver el README principal).

## Ejecutar

```powershell
npm install
npm start
```

Abre http://localhost:4200 e inicia sesión con `demo@entrevista.local` / `Demo#2026`.

`npm start` usa `proxy.conf.json`: las peticiones a `/api/*` se reenvían al backend, así frontend y API comparten origen (necesario para la cookie del refresh token con `SameSite=Strict`).

## Scripts

| Comando | Qué hace |
|---|---|
| `npm start` | Servidor de desarrollo con proxy en http://localhost:4200 |
| `npm run build` | Build de producción en `dist/frontend/browser` |
| `npm run watch` | Build de desarrollo en modo observación |

## Estructura

```
src/app/
├── app.ts · app.config.ts · app.routes.ts
├── core/                       Transversal a toda la app
│   ├── config/                 URL base de la API
│   ├── guards/                 authGuard (rutas privadas), guestGuard (login)
│   ├── interceptors/           Bearer token y renovación automática ante 401
│   ├── models/                 Tipos de autenticación
│   ├── services/               AuthApi, AuthStore (sesión con signals), ToastService
│   └── utils/                  Errores HTTP → mensajes comprensibles
├── features/
│   ├── auth/sign-in/           Pantalla de login
│   └── products/
│       ├── models/             Product, Category, UpdateProductRequest
│       ├── services/           ProductsApi
│       ├── product-list/       Catálogo (rxResource)
│       └── product-edit/       Edición (rxResource + Signal Forms) y esquema de validación
└── shared/
    ├── components/             common, form (FormValueControl), header, ui
    ├── layout/                 Layout del panel, header, sidebar, layout del login
    ├── services/               Tema claro/oscuro y estado del sidebar
    ├── icons/ · pipe/
```

## Sesión

- El **access token** vive solo en memoria (`AuthStore`), nunca en `localStorage`.
- El **refresh token** es una cookie `HttpOnly` que gestiona el navegador.
- Al cargar la app, `provideAppInitializer` llama a `/api/auth/refresh` para recuperar la sesión.
- El interceptor agrega `Authorization: Bearer` y, ante un 401, renueva el token y reintenta una vez.
