# Plan del backend por fases

Cada fase termina con build sin warnings, pruebas en verde y un commit. Revisa con la checklist antes de pasar a la siguiente. Los prompts están listos para pegar en Claude Code.

| Fase | Qué sale | Depende de |
| --- | --- | --- |
| 0 | Esqueleto de la solución | - |
| 1 | Dominio y cálculo de precios | 0 |
| 2 | Persistencia (EF Core + PostgreSQL) | 1 |
| 3 | Lector del catálogo de Magna | 1 |
| 4 | Caso de uso de sincronización | 2, 3 |
| 5 | Administración de precios y productos | 2 |
| 6 | Catálogo público | 2 |
| 7 | API, auth y jobs | 4, 5, 6 |
| 8 | Pulido: logs, health checks, README | 7 |

Las fases 2 y 3 se pueden hacer en cualquier orden.

---

## Fase 0: Esqueleto

**Prompt**

```
Lee CLAUDE.md, docs/spec.md y docs/backend-plan.md. Haz solo la Fase 0.

Crea la solución FragranceCatalog.slnx con .NET 10 y la estructura de CLAUDE.md:
4 proyectos en src/ y 4 de pruebas en tests/, con las referencias según la regla de dependencias.

Agrega:
- Directory.Build.props: Nullable, ImplicitUsings, TreatWarningsAsErrors, LangVersion latest,
  GenerateDocumentationFile en src/ con CS1591 como error; desactivado en tests/.
- Directory.Packages.props con Central Package Management y las versiones de los paquetes permitidos.
- .editorconfig con file-scoped namespaces, var cuando el tipo es obvio, y reglas de nombres estándar de .NET.
- global.json fijando el SDK 10.
- docker-compose.yml con PostgreSQL 17 (usuario, password y base desde variables de entorno con defaults).
- .gitignore de .NET.

No agregues código de negocio. Cada proyecto de pruebas debe tener una prueba trivial para confirmar que corre.
Al terminar: dotnet build, dotnet test, resumen y detente.
```

**Revisa**

- [ ] Las referencias entre proyectos respetan Domain <- Application <- Infrastructure <- Api.
- [ ] Domain no tiene ningún paquete NuGet.
- [ ] Las versiones de paquetes viven solo en `Directory.Packages.props`.
- [ ] `dotnet build` sin warnings.

---

## Fase 1: Dominio y cálculo de precios

**Prompt**

```
Haz solo la Fase 1 de docs/backend-plan.md.

En FragranceCatalog.Domain crea las entidades, enums y value objects de la sección 6 de docs/spec.md,
con setters privados, fábricas que validan invariantes y métodos con intención
(MarkAsDiscontinued, Reactivate, Hide, Show, Feature, Unfeature, RecordSupplierPrice, etc.).

Crea PriceCalculator según la sección 3 del spec:
- Función pura. Recibe el costo, la marca y el producto, las reglas aplicables, los settings,
  los rangos y el instante actual; no consulta nada externo.
- Devuelve un PriceQuote (record) con SalePrice, Profit, IsLowProfit y AppliedRule
  (qué tipo de regla aplicó y su id, si tiene).
- El redondeo vive en su propia clase o método fácil de leer.
- La validación de rangos (empieza en 0, sin huecos, sin traslapes, solo el último abierto)
  es un método de dominio reutilizable que devuelve los errores encontrados.

En FragranceCatalog.Domain.Tests cubre todos los casos de la tabla "Casos de prueba obligatorios"
del spec, más: cada modo de redondeo, FixedPrice sin redondeo, regla con StartsAt futuro,
regla inactiva, IsLowProfit con ganancia negativa, y validación de rangos (hueco, traslape,
no empieza en 0, dos rangos abiertos).

Sin EF Core ni atributos de persistencia en el dominio.
Al terminar: build, test, resumen y detente.
```

**Revisa**

- [ ] `PriceCalculator` se entiende leyéndolo de arriba a abajo, sin saltar entre muchas clases.
- [ ] No hay `DateTime.Now` en el dominio; el instante entra como parámetro.
- [ ] Las entidades no se pueden dejar en estado inválido desde afuera.
- [ ] Los nombres de las pruebas cuentan el caso sin leer el cuerpo.
- [ ] Cero comentarios `//`; XML Docs en todo lo público.

---

## Fase 2: Persistencia

**Prompt**

```
Haz solo la Fase 2 de docs/backend-plan.md.

En FragranceCatalog.Infrastructure:
- FragranceCatalogDbContext con EF Core 10 + Npgsql.
- Una IEntityTypeConfiguration por entidad, en Persistence/Configurations/.
  Tablas y columnas en snake_case, dinero numeric(12,2), fechas timestamptz,
  índices únicos del spec, relaciones con DeleteBehavior explícito.
- Seed de datos iniciales: el Supplier Magna, PricingSettings, SyncSettings y StoreSettings
  con los defaults de las secciones 4.1 y 4.2 del spec, y los dos rangos iniciales.
- Migración inicial.
- Método de extensión AddPersistence(IServiceCollection, IConfiguration) para registrar todo.

En Application define solo las interfaces que Infrastructure necesita implementar en esta fase
(si hacen falta); no crees casos de uso todavía.

En FragranceCatalog.Infrastructure.Tests, pruebas de integración con Testcontainers.PostgreSql:
aplicar migraciones, guardar y leer un producto con imágenes y precios, y confirmar que el
índice único (SupplierId, SupplierProductId) rechaza duplicados.
Al terminar: build, test, resumen y detente.
```

**Revisa**

- [ ] El dominio sigue sin saber nada de EF Core.
- [ ] La migración generada se lee bien (nombres de tablas e índices).
- [ ] Las pruebas de integración levantan y tiran su propio contenedor.

---

## Fase 3: Lector del catálogo de Magna

**Prompt**

```
Haz solo la Fase 3 de docs/backend-plan.md.

Primero investiga (tenemos permiso del proveedor):
1. Descarga el índice de sitemap de Magna y una página del sitemap de productos.
2. Descarga el HTML de 5 productos: uno con oferta, uno sin oferta, uno agotado,
   un estuche/set y uno de marca árabe. Encuentra un producto agotado navegando categorías si hace falta.
3. Guárdalos en tests/FragranceCatalog.Infrastructure.Tests/Fixtures/Magna/.
4. Revisa si tienen JSON-LD schema.org/Product y qué campos trae.
5. Encuentra cómo se marca un producto agotado.
Muéstrame lo que encontraste (selectores, JSON-LD, marcador de agotado) y espera mi confirmación
antes de escribir el parser.

Después de confirmar:
- En Application: interfaz ISupplierCatalogSource con
  GetProductUrlsAsync y GetProductAsync, y el record SupplierProduct con los campos de la
  sección 1 del spec.
- En Infrastructure/Suppliers/Magna/: MagnaCatalogSource, un SitemapReader y un
  MagnaProductPageParser separados. El parser recibe HTML y devuelve SupplierProduct;
  no hace HTTP. Selectores, User-Agent y URL del sitemap en MagnaOptions (appsettings).
  HttpClient tipado con resiliencia estándar. La pausa entre peticiones y el timeout
  los recibe como parámetros (vienen de SyncSettings); no los lee de appsettings.
- Pruebas del parser con cada fixture y del SitemapReader con un XML de ejemplo.
  Ninguna prueba toca la red.
Al terminar: build, test, resumen y detente.
```

**Revisa**

- [ ] El parser es testeable sin red y las pruebas usan HTML real guardado.
- [ ] Si Magna cambia su HTML, se ajusta en `appsettings` o en un solo archivo.
- [ ] Extracción de género y tamaño cubierta con casos raros (sin ml, "Estuche", "Set").
- [ ] La pausa entre peticiones y el timeout llegan como parámetros, sin valores fijos en el código.

---

## Fase 4: Sincronización

**Prompt**

```
Haz solo la Fase 4 de docs/backend-plan.md.

En Application/Sync/ crea el caso de uso SyncSupplierCatalog según la sección 2 del spec:
- Recibe el disparador (Scheduled o Manual).
- Lee SyncSettings al inicio y usa esos valores durante todo el sync.
- Lee todo del ISupplierCatalogSource, junta errores por URL y aplica FailureThresholdPercent.
- Aplica los cambios en una sola transacción (crear, actualizar, historial de precio
  solo si cambió, descontinuar, reactivar, crear marcas nuevas con slug).
- Nunca toca IsHidden ni IsFeatured.
- Guarda el SyncRun con conteos y errores.
- Usa TimeProvider.

Divide la lógica para que se lea fácil: por ejemplo, un paso que obtiene los datos
y otro que calcula el plan de cambios (función pura y testeable) antes de aplicarlo.

Pruebas en Application.Tests con un ISupplierCatalogSource falso: producto nuevo,
cambio de precio, mismo precio (sin fila nueva), descontinuado, reactivado,
con umbral 20: 21% de fallas (nada cambia, run Failed) y 19% (sí aplica);
con umbral 50: 40% de fallas sí aplica; oculto que sigue oculto.
Al terminar: build, test, resumen y detente.
```

**Revisa**

- [ ] El cálculo del plan de cambios está separado del I/O.
- [ ] Con más del 20% de errores la base queda intacta.
- [ ] El `SyncRun` se guarda también cuando falla.

---

## Fase 5: Administración de precios y productos

**Prompt**

```
Haz solo la Fase 5 de docs/backend-plan.md.

En Application crea los casos de uso de la sección 4 del spec, excepto auth y el disparo del sync
(eso va en la Fase 7):
- Pricing/: GetPricingConfiguration, UpdatePricingSettings, ReplacePriceRanges (valida con el
  método de dominio), CreatePricingRule, UpdatePricingRule, SetPricingRuleActive,
  PreviewPricingChange.
- Products/: ListAdminProducts (filtros del spec), SetProductVisibility, SetProductFeatured.
- Alerts/: GetPricingAlerts (ganancia menor a MinProfit y cambios de costo mayores a
  CostChangeAlertPercent).
- Settings/: GetSyncSettings y UpdateSyncSettings con la validación de rangos de la
  sección 4.1. UpdateSyncSettings avisa del cambio por una interfaz ISyncScheduler
  (definida aquí, implementada en la Fase 7) para que el job se reprograme.
- Settings/: GetStoreSettings y UpdateStoreSettings con la validación de la sección 4.2
  (normalizar el número, rechazar marcadores desconocidos).

Reglas:
- Errores esperados con el Result propio (definirlo aquí si no existe), con códigos claros
  (RangesHaveGap, DuplicateActiveRule, ProductNotFound, etc.).
- PreviewPricingChange no guarda nada: calcula precios actuales y propuestos con el mismo
  PriceCalculator.
- DTOs de admin en su feature; no se reutilizan para lo público.
- Validación sin excepciones: hoy los métodos Update de PricingSettings, SyncSettings y
  StoreSettings, y las fábricas y Update de PricingRule y PriceRange, lanzan excepciones.
  Sigue el patrón de PriceRangeValidator: agrega en el dominio un método estático Validate
  por cada uno, que reciba los mismos valores y devuelva la lista de errores, con un código
  por problema (enum) y el valor que lo causó. Reutiliza las constantes de límites que ya existen.
  Los casos de uso llaman a Validate primero y, si hay errores, devuelven Result con esos
  códigos sin tocar la entidad. Las excepciones de Update y de las fábricas se quedan solo
  como red de seguridad, y deben usar la misma validación para no duplicar reglas.
- Si hay varios errores, se devuelven todos juntos, para que el panel los muestre de una vez.

Pruebas en Application.Tests de cada caso de uso, incluyendo validaciones y la vista previa.
Pruebas en Domain.Tests de cada método Validate: un caso por código de error, un caso válido
y un caso con varios errores a la vez.
Al terminar: build, test, resumen y detente.
```

**Revisa**

- [ ] La vista previa y el precio real usan exactamente el mismo cálculo.
- [ ] No se puede tener dos reglas activas para la misma marca o producto.
- [ ] Los códigos de error son entendibles para mostrarlos después en el panel.
- [ ] Ningún caso de uso usa try/catch para validar; los errores salen de los métodos Validate.
- [ ] Cada regla de validación existe en un solo lugar (Validate), no repetida en Update.

---

## Fase 6: Catálogo público

**Prompt**

```
Haz solo la Fase 6 de docs/backend-plan.md.

En Application/Catalog/ crea: SearchCatalog (paginado, búsqueda, filtros y orden de la sección 5),
GetProductBySlug, ListBrands, ListFeaturedProducts y GetStoreInfo.

Crea un WhatsAppLinkBuilder (clase pequeña y pura) que reciba StoreSettings, el producto y su
precio de venta, y devuelva el enlace según la sección 4.2 del spec, o null si no hay número.
Cada DTO público de producto incluye WhatsAppUrl.

- Solo productos disponibles, no descontinuados y no ocultos.
- DTOs públicos que no tengan costo, ganancia, regla, URL del proveedor ni nombre del proveedor.
- Filtro y orden por precio usando el precio calculado.

Pruebas: visibilidad (oculto, descontinuado, agotado no salen), filtros, orden por precio,
paginación; WhatsAppLinkBuilder (cada marcador, producto sin tamaño, formato de precio,
caracteres especiales y acentos codificados en la URL, número vacío -> null);
y una prueba que verifique por reflexión que los DTOs públicos no tienen
propiedades llamadas Cost, Profit, Supplier* ni SourceUrl.
Al terminar: build, test, resumen y detente.
```

**Revisa**

- [ ] Ningún DTO público puede filtrar datos del proveedor.
- [ ] La paginación devuelve total de resultados y páginas.

---

## Fase 7: API, auth y jobs

**Prompt**

```
Haz solo la Fase 7 de docs/backend-plan.md.

En FragranceCatalog.Api:
- Minimal APIs agrupadas por feature en archivos de extensión (MapCatalogEndpoints,
  MapAdminPricingEndpoints, MapAdminProductEndpoints, MapAdminSyncEndpoints).
  Públicos bajo /api/catalog, admin bajo /api/admin con autorización.
- ASP.NET Core Identity con cookies; seed de dos admins desde configuración/user secrets.
  Sin registro público. Endpoints de login y logout.
- Mapeo de Result a respuestas: éxito -> 200/201/204, errores -> ProblemDetails con el código.
- OpenAPI + Scalar solo en Development.
- CORS configurable para el futuro frontend Angular.
- Hangfire con Hangfire.PostgreSql: recurring job diario según SyncSettings (hora, zona
  horaria, encendido/apagado), programado al arrancar la API. Implementa ISyncScheduler
  para reprogramarlo o quitarlo cuando cambian los settings, sin reiniciar.
  Endpoint POST /api/admin/sync que encola el mismo job. DisableConcurrentExecution.
  Dashboard de Hangfire solo para admins.
- Endpoints GET/PUT para PricingSettings, SyncSettings y StoreSettings (admin),
  y GET /api/catalog/store (público).
- Toda la composición de DI en métodos de extensión por capa; Program.cs corto y legible.

Pruebas en Api.Tests con WebApplicationFactory + Testcontainers: endpoint público sin auth,
admin sin auth -> 401, login y acceso admin, un error de validación devuelve ProblemDetails,
y la respuesta pública del detalle no trae campos del proveedor.
Al terminar: build, test, resumen y detente.
```

**Revisa**

- [ ] `Program.cs` se lee en menos de un minuto.
- [ ] Ningún endpoint devuelve entidades.
- [ ] Las credenciales de los admins no están en el repo.

---

## Fase 8: Pulido

**Prompt**

```
Haz solo la Fase 8 de docs/backend-plan.md.

- Logging estructurado con el logger de .NET en sync y scraping (inicio, fin, conteos, errores).
- Health checks: /health con PostgreSQL.
- Revisa todo el repo contra CLAUDE.md: comentarios prohibidos, XML Docs faltantes o que repiten
  el nombre, métodos largos, nombres poco claros. Corrige lo que encuentres y lista los cambios.
- README.md: qué es, cómo correrlo local (docker compose, user secrets, migraciones), cómo correr
  pruebas y cómo disparar un sync.
Al terminar: build, test, resumen y detente.
```

**Revisa**

- [ ] Alguien nuevo puede levantar el proyecto solo con el README.
- [ ] `grep -rn "//" src/` no encuentra comentarios (solo URLs dentro de strings).
