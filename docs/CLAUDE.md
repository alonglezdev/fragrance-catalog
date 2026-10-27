# fragrance-catalog

Catálogo de perfumes de solo consulta. Importa productos del proveedor (Magna Perfumes) por scraping, calcula el precio de venta con reglas de ganancia configurables y lo expone en una API para un frontend Angular (fase posterior).

- Especificación funcional: `docs/spec.md`
- Plan por fases: `docs/backend-plan.md`

Lee los dos antes de empezar cualquier fase.

## Forma de trabajo

- Trabaja **solo en la fase que se te pida**. No adelantes trabajo de fases siguientes.
- Al terminar una fase: `dotnet build` sin warnings, `dotnet test` en verde, y un resumen corto con los archivos creados o modificados y cualquier decisión que tomaste. Luego **detente** y espera revisión.
- Si algo del spec es ambiguo o contradictorio, pregunta antes de inventar.
- No agregues paquetes NuGet fuera de la lista de abajo sin preguntar.
- Un commit por fase, mensaje en inglés con formato Conventional Commits (`feat(domain): add price calculator`).

## Stack

- .NET 10 (LTS), C# con `Nullable` y `ImplicitUsings` habilitados.
- Solución `FragranceCatalog.slnx`.
- PostgreSQL con EF Core 10 + Npgsql.
- Jobs: Hangfire + Hangfire.PostgreSql.
- Scraping: `HttpClient` + AngleSharp, resiliencia con `Microsoft.Extensions.Http.Resilience`.
- API: Minimal APIs, OpenAPI integrado (`Microsoft.AspNetCore.OpenApi`) + Scalar, ProblemDetails.
- Auth del admin: ASP.NET Core Identity con cookies.
- Pruebas: xUnit v3, Shouldly, NSubstitute, Testcontainers.PostgreSql.

Paquetes permitidos: los anteriores y sus dependencias. **No usar** AutoMapper, MediatR, FluentAssertions ni librerías de "result" de terceros.

## Estructura

```
src/
  FragranceCatalog.Domain/          Entidades, value objects, enums, PriceCalculator. Sin dependencias.
  FragranceCatalog.Application/     Casos de uso, interfaces (puertos), DTOs. Depende solo de Domain.
  FragranceCatalog.Infrastructure/  EF Core, migraciones, MagnaCatalogSource, Hangfire. Implementa interfaces de Application.
  FragranceCatalog.Api/             Minimal APIs, auth, composición (DI), configuración.
tests/
  FragranceCatalog.Domain.Tests/
  FragranceCatalog.Application.Tests/
  FragranceCatalog.Infrastructure.Tests/   Incluye fixtures HTML de Magna e integración con Testcontainers.
  FragranceCatalog.Api.Tests/              WebApplicationFactory.
docs/
```

Regla de dependencias: Domain <- Application <- Infrastructure <- Api. Nunca al revés. Domain y Application no conocen EF Core, HTTP ni AngleSharp.

Organiza Application e Infrastructure **por feature** (`Pricing/`, `Sync/`, `Catalog/`), no por tipo técnico (`Services/`, `Repositories/`).

## Reglas de código

### Comentarios y documentación

- **Prohibidos los comentarios dentro del código**: nada de `//` ni `/* */`, tampoco código comentado ni `TODO`.
- **Solo XML Docs** (`/// <summary>`) en tipos y miembros públicos. Explican el *qué* y el *por qué* de negocio, no repiten el nombre.
  - Mal: `/// <summary>Gets the price.</summary>`
  - Bien: `/// <summary>Precio final para el cliente, después de aplicar la regla y el redondeo.</summary>`
- Si sientes la necesidad de un comentario, el código no es lo bastante claro: extrae un método o una variable con nombre explicativo.
- `GenerateDocumentationFile` está activo en los proyectos de `src/` y CS1591 es error, así que todo lo público debe tener XML Docs. En proyectos de pruebas está desactivado.
- XML Docs en español o inglés, pero consistente en todo el repo: **inglés**.

### Nombres

- Todo el código en inglés: clases, métodos, variables, tablas, columnas, endpoints.
- Nombres completos y descriptivos: `supplierProductId`, no `spId`; `CalculateSalePrice`, no `Calc`.
- Booleanos como pregunta: `IsAvailable`, `HasExpired`, `CanApply`.
- Async termina en `Async` y recibe `CancellationToken` como último parámetro.
- Sin abreviaturas salvo las universales (`Id`, `Url`, `Ml`, `Html`).

### Estilo

- File-scoped namespaces, un tipo público por archivo, nombre del archivo = nombre del tipo.
- Primary constructors para inyección de dependencias.
- `sealed` por defecto en clases que no están pensadas para herencia.
- `record` para DTOs y value objects.
- Métodos cortos (idealmente menos de 20 líneas) con guard clauses y early returns en lugar de `if` anidados.
- Sin números mágicos. Los valores de negocio u operación (umbrales, porcentajes, horarios, pausas, timeouts) viven en `PricingSettings` o `SyncSettings` en la base de datos, editables desde el panel (ver spec 4.1). Solo lo que puede romper el sistema si se configura mal (selectores, URLs, User-Agent) va en `appsettings`.
- `decimal` para dinero, siempre. Redondeo explícito con `MidpointRounding.AwayFromZero`.
- Nada de `DateTime.Now` / `UtcNow` directo: usar `TimeProvider` (inyectado) para que sea testeable.
- `var` cuando el tipo es obvio por el lado derecho.
- Expresiones `switch` sobre cadenas de `if/else` cuando aplique.

### Dominio

- Entidades con setters privados; el estado cambia con métodos que expresan intención (`MarkAsDiscontinued()`, `Hide()`, `UpdateSupplierPrice(...)`).
- Creación con métodos de fábrica estáticos o constructores que validan invariantes.
- `PriceCalculator` es una función pura: mismos datos de entrada, mismo resultado. Sin I/O.

### Errores

- Errores esperados de negocio (validación de reglas, producto no encontrado): un tipo `Result` / `Result<T>` propio y pequeño en Application, mapeado a ProblemDetails en la API.
- Excepciones solo para lo inesperado.
- Nunca tragar excepciones en silencio; el sync registra el error por producto y continúa (ver spec).

### Pruebas

- Nombre: `Method_Scenario_ExpectedResult` (`Calculate_ProductRuleExists_OverridesBrandRule`).
- Estructura Arrange / Act / Assert separada por líneas en blanco (sin comentarios).
- Pruebas unitarias para todo el dominio y casos de uso; integración con Testcontainers para EF Core.
- El parser de Magna se prueba con HTML guardado en `tests/FragranceCatalog.Infrastructure.Tests/Fixtures/`, nunca contra el sitio real.

### Seguridad de datos

- La API pública **nunca** devuelve costo del proveedor, ganancia, URL del proveedor ni el nombre del proveedor. Usa DTOs separados para público y admin; nunca serialices entidades directamente.

## Comandos

```
docker compose up -d            # PostgreSQL local
dotnet build
dotnet test
dotnet ef migrations add <Name> -p src/FragranceCatalog.Infrastructure -s src/FragranceCatalog.Api
dotnet run --project src/FragranceCatalog.Api
```
