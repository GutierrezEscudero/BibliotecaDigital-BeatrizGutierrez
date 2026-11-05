# Biblioteca Digital

API REST para consultar y gestionar el catálogo de una biblioteca y sus préstamos. El proyecto está desarrollado con ASP.NET Core y .NET 10.

## Requisitos

- .NET SDK 10.0

## Ejecutar la API

Desde la raíz del repositorio:

```powershell
dotnet restore .\BibliotecaDigitalE2E.sln
dotnet run --project .\src\Biblioteca.Api
```

El perfil de desarrollo inicia la API en `https://localhost:52748` y `http://localhost:52749`. La interfaz de Swagger está disponible en `/swagger` cuando la aplicación se ejecuta en el entorno `Development`.

Comprobación de estado:

```text
GET http://localhost:52749/api/health
```

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/books` | Lista los libros del catálogo. |
| `GET` | `/api/books/{id}` | Obtiene un libro; responde `404` si no existe. |
| `POST` | `/api/books` | Crea un libro; responde `409` si el ISBN ya está registrado y valida los campos de la solicitud. |
| `DELETE` | `/api/books/{id}` | Elimina un libro sin préstamos activos; responde `409` si tiene alguno. |
| `GET` | `/api/loans` | Lista los préstamos. |
| `GET` | `/api/loans/{id}` | Obtiene un préstamo; responde `404` si no existe. |
| `POST` | `/api/loans` | Crea un préstamo de un libro con ejemplares disponibles. |
| `PUT` | `/api/loans/{id}/return` | Registra la devolución y devuelve un ejemplar al catálogo. |
| `GET` | `/api/health` | Comprueba que la API está activa. |

Las fechas de préstamo y devolución se envían como `YYYY-MM-DD`. Al crear un préstamo, la fecha de devolución debe ser posterior a la fecha de préstamo. Los endpoints de creación devuelven `201 Created` cuando la operación se completa correctamente.

## Datos

El catálogo inicial contiene tres libros. Tanto los libros como los préstamos se guardan en repositorios en memoria: los cambios no persisten al reiniciar la API. Es una implementación de demostración/pruebas, no una base de datos de producción.

## Pruebas

Pruebas unitarias e integración:

```powershell
dotnet test .\tests\Biblioteca.UnitTests
dotnet test .\tests\Biblioteca.IntegrationTests
```

Las pruebas E2E se conectan por HTTP a una API Kestrel ya iniciada. En una terminal, inicia la API con el comando de arriba; en otra:

```powershell
dotnet test .\tests\Biblioteca.E2ETests
```

Por defecto, las E2E usan `http://localhost:52749`, el mismo puerto HTTP que el perfil de lanzamiento. Para probar otra instancia, define `E2E_BASE_URL` antes de ejecutar las pruebas.

## Estructura

- `src/Biblioteca.Api`: API, controladores, contratos, servicios y repositorios en memoria.
- `tests/Biblioteca.UnitTests`: pruebas de las reglas de negocio.
- `tests/Biblioteca.IntegrationTests`: pruebas HTTP con `WebApplicationFactory`.
- `tests/Biblioteca.E2ETests`: flujos completos contra una API iniciada con Kestrel.
