# Entrevista API

API REST en **.NET 9** con **Entity Framework Core 9** y **SQL Server**.

> Estado actual: estructura base. Los módulos están creados pero vacíos.

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

- HTTP: http://localhost:5054
- HTTPS: https://localhost:7022
- OpenAPI (solo en dev): http://localhost:5054/openapi/v1.json

### 3. Importar productos de FakeStore API

En dev la importación corre sola al arrancar. También se puede lanzar a mano (en cualquier entorno):

```powershell
curl -X POST http://localhost:5054/api/catalog/import
```

Primero se importan las categorías (`/products/categories`) y luego los productos (`/products`).

- **200**: resumen por separado para `categorias` y `productos`, con `recibidos`, `importados`, `omitidos` y el motivo de cada omitido (ya existía, datos inválidos, repetido, categoría inexistente).
- **502**: la API no respondió o devolvió datos inválidos. No se guarda nada; se puede reintentar.
- **409**: otra importación se ejecutó al mismo tiempo. Se puede reintentar.

Reintentar no duplica datos: solo se insertan categorías cuyo `Nombre` no exista y productos cuyo `ExternalId` no exista.

## Producción

### 1. Crear la configuración

Si no existe, copiar la plantilla:

```powershell
Copy-Item src/Entrevista.Api/appsettings.Production.example.json src/Entrevista.Api/appsettings.Production.json
```

Editar la conexión con los datos del servidor real:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=TU_SERVIDOR,1433;Database=EntrevistaDb;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True"
}
```

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

- Entidades, repositorios, servicios y controladores de cada módulo.
- Migraciones de EF Core.
