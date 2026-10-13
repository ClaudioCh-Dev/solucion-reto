# Entrevista API

API REST en **.NET 9** con **Entity Framework Core 9** y **SQL Server**.

> Estado actual: backend con autenticación JWT, importación desde FakeStore API y edición de productos. Falta el frontend.

## Requisitos

- [.NET SDK 9](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para el SQL Server de desarrollo)

## Estructura

```
ENTREVISTA/
├── frontend/                    # Angular
└── Backend/
    ├── Entrevista.sln
    ├── docker-compose.yml       # SQL Server para desarrollo
    ├── .config/
    │   └── dotnet-tools.json    # dotnet-ef como herramienta local
    └── src/Entrevista.Api/
        ├── Program.cs
        ├── appsettings.json                     # Configuración común
        ├── appsettings.Development.json         # Conexión de desarrollo (Docker)
        ├── appsettings.Production.json          # Conexión de producción (NO se sube a git)
        ├── appsettings.Production.example.json  # Plantilla de producción
        ├── Properties/
        │   └── launchSettings.json              # Perfiles: dev y prod
        ├── Common/
        │   └── RoutePrefixConvention.cs         # Prefijo global /api para todos los controladores
        ├── Data/
        │   ├── AppDbContext.cs
        │   └── Migrations/                      # Migraciones de EF Core
        └── Modules/
            ├── Auth/                            # Usuarios
            ├── Catalog/                         # Importación desde FakeStore API (POST api/catalog/import)
            ├── Categorias/
            └── Productos/
```

Cada módulo tiene la misma estructura:

| Carpeta | Responsabilidad |
|---|---|
| `Controllers/` | Endpoints HTTP |
| `Services/` | Lógica de negocio |
| `Repositories/` | Acceso a datos con EF Core |
| `Entities/` | Modelos que se mapean a tablas |
| `Configurations/` | Mapeo EF Core (`IEntityTypeConfiguration`): índices, longitudes, relaciones |
| `DTOs/` | Objetos de entrada/salida de la API |

## Modelo de datos

| Tabla | Campos | Restricciones |
|---|---|---|
| `Usuarios` | `Id`, `Email`, `PasswordHash` | `Email` único |
| `RefreshTokens` | `Id`, `UsuarioId`, `TokenHash`, `CreadoEn`, `ExpiraEn`, `RevocadoEn`, `ReemplazadoPorHash` | `TokenHash` único (SHA-256); FK a `Usuarios` (borrado en cascada) |
| `Categorias` | `Id`, `Nombre` | `Nombre` único |
| `Productos` | `Id`, `ExternalId`, `Nombre`, `Descripcion`, `Precio`, `Stock`, `CategoriaId`, `ImagenUrl` | `ExternalId` único; `Precio` decimal(18,2); FK a `Categorias` (sin borrado en cascada) |

## Configuración por entorno

Cada perfil lee su propio archivo JSON:

| Perfil | Entorno | Archivo | Base de datos | ¿En git? |
|---|---|---|---|---|
| `dev` | `Development` | `appsettings.Development.json` | SQL Server en Docker | Sí |
| `prod` | `Production` | `appsettings.Production.json` | SQL Server de producción | **No** (`.gitignore`) |

.NET carga primero `appsettings.json` y luego `appsettings.{Entorno}.json`, que pisa los valores comunes.

Si la cadena de conexión no está definida, la app no arranca y muestra un error indicando el entorno.

### Seguridad

- `appsettings.Production.json` está en `.gitignore`: las credenciales de producción no se suben al repositorio.
- Las credenciales de `appsettings.Development.json` y `docker-compose.yml` son solo para la base local de Docker.
- La app no usa `sa`. Se conecta con el usuario `entrevista_app`, que solo tiene permisos (`db_owner`) sobre su propia base.
- Docker es solo para desarrollo. Producción usa un SQL Server externo.

## Desarrollo

### 1. Levantar SQL Server

```powershell
docker compose -f Backend/docker-compose.yml up -d
```

Esto levanta dos servicios:

- `sqlserver`: SQL Server 2022 (edición Developer) en el puerto `1433`.
- `sqlserver-init`: corre una vez cuando SQL Server está listo y ejecuta `docker/sqlserver/init.sql`, que crea la base `EntrevistaDb_Dev` y el usuario `entrevista_app`. Se puede volver a ejecutar sin errores.

Credenciales de desarrollo:

| Usuario | Contraseña | Uso |
|---|---|---|
| `sa` | `Entrevista#2026` | Administración |
| `entrevista_app` | `EntrevistaApp#2026` | Conexión de la app |

### 2. Ejecutar

```powershell
dotnet run --project Backend/src/Entrevista.Api --launch-profile dev
```

- API: http://localhost:5054 (en dev solo HTTP, no hace falta certificado)
- OpenAPI (solo en dev): http://localhost:5054/openapi/v1.json

La dirección y el uso de HTTPS se configuran en `appsettings.{Entorno}.json`:

| Clave | Dev | Por defecto (`appsettings.json`) | Qué hace |
|---|---|---|---|
| `Urls` | `http://localhost:5054` | — | Direcciones donde escucha la app (varias separadas por `;`) |
| `ForzarHttps` | `false` | `true` | Si es `true`, redirige las peticiones HTTP a HTTPS |

Para usar HTTPS en dev: `"Urls": "https://localhost:7022;http://localhost:5054"`, `"ForzarHttps": true` y confiar en el certificado de desarrollo con `dotnet dev-certs https --trust`.

### 3. Autenticación (access token + refresh token)

Todos los endpoints requieren access token excepto `login`, `refresh` y `logout`. Sin token, o con uno inválido o vencido, responden **401**.

#### Usuario de demostración

Se crea solo al arrancar en dev (perfil `dev`), después de aplicar las migraciones. Si ya existe, no se toca, así que basta con ejecutar la app:

| Email | Contraseña |
|---|---|
| `demo@entrevista.local` | `Demo#2026` |

Las credenciales salen de la sección `UsuarioDemo` de `appsettings.Development.json`. Para usar otras, cámbialas ahí y reinicia (se crea un usuario nuevo; el anterior no se borra).

#### Contraseñas

Nunca se guardan en texto plano. Se hashean con `PasswordHasher` de ASP.NET Core Identity (paquete `Microsoft.Extensions.Identity.Core`): PBKDF2 con HMAC-SHA512, 100.000 iteraciones y una sal aleatoria por usuario, guardada dentro del propio hash en `Usuarios.PasswordHash`.

#### Iniciar sesión

Se usan dos tokens:

| | Access token | Refresh token |
|---|---|---|
| Qué es | JWT firmado (HMAC-SHA256) | 64 bytes aleatorios (no es un JWT) |
| Duración | **15 min** (`Jwt:AccessTokenMinutos`) | **7 días** (`Jwt:RefreshTokenDias`) |
| Cómo viaja | Body de la respuesta → el cliente lo envía en `Authorization: Bearer` | Cookie `refresh_token` (`HttpOnly`, `SameSite=Strict`, solo a `/api/auth`) |
| Dónde se guarda en el cliente | **Solo en memoria** (se pierde al recargar) | La gestiona el navegador; JavaScript no puede leerla |
| En el servidor | No se guarda: se valida por firma | Tabla `RefreshTokens`, **solo el hash SHA-256** |

```powershell
# -c guarda la cookie del refresh token, -b la envía (igual que el navegador)
curl -c cookies.txt -X POST http://localhost:5054/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{ "email": "demo@entrevista.local", "password": "Demo#2026" }'
```

- **200**: `{ "accessToken": "eyJ...", "expiraEn": "2026-10-13T15:00:00Z", "usuario": { "id": 1, "email": "..." } }` + cookie `refresh_token`.
- **401**: credenciales inválidas. El mensaje es el mismo si el email no existe o la contraseña es incorrecta.
- **400**: email con formato inválido o contraseña vacía.

El access token se envía en cada petición:

```powershell
curl http://localhost:5054/api/products -H "Authorization: Bearer eyJ..."
```

| Endpoint | Qué hace |
|---|---|
| `POST /api/auth/refresh` | Con la cookie, devuelve un access token nuevo y **rota** el refresh token (el anterior queda revocado). 401 si no hay cookie, venció o fue revocado |
| `POST /api/auth/logout` | **Revoca el refresh token en la base** y borra la cookie. Responde 204 |
| `GET /api/auth/me` | `{ id, email }` del usuario del access token |

```powershell
curl -b cookies.txt -c cookies.txt -X POST http://localhost:5054/api/auth/refresh
curl -b cookies.txt -c cookies.txt -X POST http://localhost:5054/api/auth/logout
```

**Detección de reúso**: si llega un refresh token que ya fue rotado, alguien más lo tiene (robo). Se revocan **todos** los refresh tokens del usuario y hay que volver a iniciar sesión.

#### Cómo lo usa el frontend

- Al cargar la página llama a `/refresh`: si la cookie sigue vigente, recupera la sesión sin pedir credenciales.
- Renueva el access token un minuto antes de que venza.
- Si una petición responde 401, llama a `/refresh` y la reintenta una vez. Si falla, vuelve al login con el aviso de sesión expirada.

#### Expiración y cierre de sesión

- Al cerrar sesión, el frontend llama a `/logout` (se revoca el refresh token y se borra la cookie), descarta el access token de memoria y vuelve al login.
- ⚠️ **El access token no se puede revocar**: el que ya se emitió sigue siendo válido hasta que vence (como máximo 15 minutos), aunque se haya cerrado sesión. Por eso su duración es corta. El refresh token, en cambio, deja de servir en el momento del logout.
- La tolerancia de reloj del access token está en 0: deja de valer exactamente en `expiraEn`.

#### Configuración

| Clave | Dónde | Valor |
|---|---|---|
| `Jwt:Issuer`, `Jwt:Audience` | `appsettings.json` | Comunes a todos los entornos |
| `Jwt:AccessTokenMinutos`, `Jwt:RefreshTokenDias` | `appsettings.json` | 15 y 7 por defecto |
| `Jwt:Key` | `appsettings.{Entorno}.json` | Clave secreta de firma (HMAC-SHA256), **mínimo 32 caracteres** |
| `Jwt:RefreshCookie` | `appsettings.json`, se puede pisar por perfil | Opciones de la cookie (ver abajo) |

`Jwt:RefreshCookie`:

| Clave | Por defecto | Dev | Qué hace |
|---|---|---|---|
| `Nombre` | `refresh_token` | | Nombre de la cookie |
| `Path` | `/api/auth` | | Solo se envía a refresh y logout, no en cada petición |
| `HttpOnly` | `true` | | JavaScript no puede leerla (protege ante XSS) |
| `SameSite` | `Strict` | | `Strict`, `Lax` o `None`. Con `Strict` no se envía desde otros sitios (protege ante CSRF) |
| `Secure` | `true` | `false` | Solo se envía por HTTPS. En dev se desactiva porque se usa HTTP |

La app no arranca si falta `Jwt:Key`, si es más corta de 32 caracteres, o si `SameSite` es `None` sin `Secure=true` (los navegadores rechazan esa combinación).

### 4. Importar productos de FakeStore API

En dev la importación corre sola al arrancar (sin token, porque se llama internamente). También se puede lanzar a mano en cualquier entorno, con access token:

```powershell
curl -X POST http://localhost:5054/api/catalog/import -H "Authorization: Bearer eyJ..."
```

Primero se importan las categorías (`/products/categories`) y luego los productos (`/products`).

- **200**: resumen por separado para `categorias` y `productos`, con `recibidos`, `importados`, `omitidos` y el motivo de cada omitido (ya existía, datos inválidos, repetido, categoría inexistente).
- **502**: la API no respondió o devolvió datos inválidos. No se guarda nada; se puede reintentar.
- **409**: otra importación se ejecutó al mismo tiempo. Se puede reintentar.

Reintentar no duplica datos: solo se insertan categorías cuyo `Nombre` no exista y productos cuyo `ExternalId` no exista.

### 5. Endpoints de productos y categorías

Todos requieren token (sin él: **401**).

| Método | Ruta | Uso | Respuestas |
|---|---|---|---|
| `GET` | `/api/products` | Listar productos locales | 200 |
| `GET` | `/api/products/{id}` | Obtener el producto a editar | 200, 404 |
| `PUT` | `/api/products/{id}` | Guardar cambios locales | 200 (producto actualizado), 400, 404 |
| `GET` | `/api/categories` | Cargar el selector de categorías (ordenadas por nombre) | 200 |

Body del `PUT` (`ExternalId` e `ImagenUrl` no se editan):

```json
{
  "nombre": "Mochila",
  "descripcion": "Texto opcional",
  "precio": 109.95,
  "stock": 10,
  "categoriaId": 1
}
```

Devuelve **400** si algún campo no es válido (nombre vacío o solo espacios, demasiado largo, precio o stock negativos), con mensajes en español, o si `categoriaId` no existe.

## Producción

### 1. Crear la configuración

Si no existe, copiar la plantilla:

```powershell
Copy-Item Backend/src/Entrevista.Api/appsettings.Production.example.json Backend/src/Entrevista.Api/appsettings.Production.json
```

Editar la conexión con los datos del servidor real y poner una clave JWT propia (mínimo 32 caracteres):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=TU_SERVIDOR,1433;Database=EntrevistaDb;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True"
},
"Jwt": {
  "Key": "TU_CLAVE_SECRETA_DE_AL_MENOS_32_CARACTERES"
}
```

Se puede generar una clave aleatoria con:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

En producción no se crea el usuario de demostración automáticamente.

### 2. Ejecutar

```powershell
dotnet run --project Backend/src/Entrevista.Api --launch-profile prod
```

Al publicar (`dotnet publish`), el archivo `appsettings.Production.json` se copia junto a la app. En el servidor, la app debe correr con `ASPNETCORE_ENVIRONMENT=Production` (es el valor por defecto si no se define).

## Comandos útiles

Desde la carpeta `Backend/`:

```powershell
docker compose up -d          # Levantar SQL Server
docker compose down           # Apagar (los datos se conservan en el volumen)
docker compose down -v        # Apagar y BORRAR los datos
docker compose logs -f sqlserver

dotnet build
```

## Pendiente

- Frontend (login, guardar/borrar el token, rutas protegidas).
