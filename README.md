# Entrevista API

API REST en **.NET 9** con **Entity Framework Core 9** y **SQL Server**.

> Estado actual: backend con autenticación JWT, importación desde FakeStore API y edición de productos. Falta el frontend.

## Requisitos

- [.NET SDK 9](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para el SQL Server de desarrollo)

## Estructura

```
ENTREVISTA/
├── Entrevista.sln
├── docker-compose.yml           # SQL Server para desarrollo
├── docker/
│   └── sqlserver/
│       └── init.sql             # Crea la base y el usuario de la app
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
docker compose up -d
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
dotnet run --project src/Entrevista.Api --launch-profile dev
```

- API: http://localhost:5054 (en dev solo HTTP, no hace falta certificado)
- OpenAPI (solo en dev): http://localhost:5054/openapi/v1.json

La dirección y el uso de HTTPS se configuran en `appsettings.{Entorno}.json`:

| Clave | Dev | Por defecto (`appsettings.json`) | Qué hace |
|---|---|---|---|
| `Urls` | `http://localhost:5054` | — | Direcciones donde escucha la app (varias separadas por `;`) |
| `ForzarHttps` | `false` | `true` | Si es `true`, redirige las peticiones HTTP a HTTPS |

Para usar HTTPS en dev: `"Urls": "https://localhost:7022;http://localhost:5054"`, `"ForzarHttps": true` y confiar en el certificado de desarrollo con `dotnet dev-certs https --trust`.

### 3. Autenticación (JWT)

Todos los endpoints requieren token excepto `POST /api/auth/login`. Sin token, o con uno inválido o vencido, responden **401**.

#### Usuario de demostración

Se crea solo al arrancar en dev (perfil `dev`), después de aplicar las migraciones. Si ya existe, no se toca, así que basta con ejecutar la app:

| Email | Contraseña |
|---|---|
| `demo@entrevista.local` | `Demo#2026` |

Las credenciales salen de la sección `UsuarioDemo` de `appsettings.Development.json`. Para usar otras, cámbialas ahí y reinicia (se crea un usuario nuevo; el anterior no se borra).

#### Contraseñas

Nunca se guardan en texto plano. Se hashean con `PasswordHasher` de ASP.NET Core Identity (paquete `Microsoft.Extensions.Identity.Core`): PBKDF2 con HMAC-SHA512, 100.000 iteraciones y una sal aleatoria por usuario, guardada dentro del propio hash en `Usuarios.PasswordHash`.

#### Iniciar sesión

```powershell
curl -X POST http://localhost:5054/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{ "email": "demo@entrevista.local", "password": "Demo#2026" }'
```

- **200**: `{ "token": "eyJ...", "expiraEn": "2026-10-13T15:00:00Z" }` (UTC).
- **401**: credenciales inválidas. El mensaje es el mismo si el email no existe o la contraseña es incorrecta.
- **400**: email con formato inválido o contraseña vacía.

El token se envía en cada petición:

```powershell
curl http://localhost:5054/api/products -H "Authorization: Bearer eyJ..."
```

`GET /api/auth/me` devuelve `{ id, email }` del usuario del token; sirve para comprobar si la sesión sigue activa.

#### Expiración y cierre de sesión

- El token vence a los **60 minutos** (`Jwt:ExpiracionMinutos`). La tolerancia de reloj está en 0: deja de valer exactamente en `expiraEn`.
- **No hay revocación ni endpoint de logout.** Cerrar sesión consiste en que el cliente borre el token y vuelva al login.
- ⚠️ **El token sigue siendo válido hasta que vence**, aunque se haya cerrado sesión: si alguien lo copió antes, puede usarlo hasta `expiraEn`. Por eso la expiración es corta.

#### Configuración

| Clave | Dónde | Valor |
|---|---|---|
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiracionMinutos` | `appsettings.json` | Comunes a todos los entornos |
| `Jwt:Key` | `appsettings.{Entorno}.json` | Clave secreta de firma (HMAC-SHA256), **mínimo 32 caracteres** |

Si falta `Jwt:Key` o es más corta, la app no arranca. La clave de dev es solo para desarrollo; producción debe usar una propia.

### 4. Importar productos de FakeStore API

En dev la importación corre sola al arrancar (sin token, porque se llama internamente). También se puede lanzar a mano en cualquier entorno, con token:

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

Body del `PUT` (`ExternalId` no se edita):

```json
{
  "nombre": "Mochila",
  "descripcion": "Texto opcional",
  "precio": 109.95,
  "stock": 10,
  "imagenUrl": "https://fakestoreapi.com/img/81fPKd-2AYL._AC_SL1500_.jpg",
  "categoriaId": 1
}
```

Devuelve **400** si algún campo no es válido (nombre vacío o demasiado largo, precio o stock negativos, URL inválida) o si `categoriaId` no existe.

## Producción

### 1. Crear la configuración

Si no existe, copiar la plantilla:

```powershell
Copy-Item src/Entrevista.Api/appsettings.Production.example.json src/Entrevista.Api/appsettings.Production.json
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
dotnet run --project src/Entrevista.Api --launch-profile prod
```

Al publicar (`dotnet publish`), el archivo `appsettings.Production.json` se copia junto a la app. En el servidor, la app debe correr con `ASPNETCORE_ENVIRONMENT=Production` (es el valor por defecto si no se define).

## Comandos útiles

```powershell
docker compose up -d          # Levantar SQL Server
docker compose down           # Apagar (los datos se conservan en el volumen)
docker compose down -v        # Apagar y BORRAR los datos
docker compose logs -f sqlserver

dotnet build
```

## Pendiente

- Frontend (login, guardar/borrar el token, rutas protegidas).
