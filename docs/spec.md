# Spec: fragrance-catalog (backend, fase 1)

Catálogo de solo consulta. Muestra todo lo que el proveedor tiene disponible, con el precio de venta definido por la administradora. Los clientes compran por WhatsApp; no hay carrito ni pagos en esta fase.

## 1. Proveedor: Magna Perfumes

- Sitio: `https://magnaperfumes.com` (BigCommerce, storefront Catalyst con Next.js, HTML renderizado en servidor).
- Hay permiso del proveedor para usar sus fotos y datos.
- Índice de sitemap: `https://magnaperfumes.com/sitemap.xml`. Contiene sub-sitemaps; el de productos es `sitemap.xml?type=products&page=N` (puede haber varias páginas).
- Fotos en `https://cdn11.bigcommerce.com/s-2vt02okold/images/stencil/{ancho}w/products/{productId}/{imageId}/{archivo}`.
  - El número después de `/products/` es el ID del producto en el proveedor.
  - Se puede pedir otro ancho cambiando `{ancho}w` (ej. `640w`, `1280w`).
- Precio: si hay oferta aparecen dos montos (normal y oferta, ej. `$3,269` y `$2,949`); si no, uno solo. Moneda MXN.
- Nombre de ejemplo: `Hawas Private Dulce Extrait de Parfum 100ml Unisex`, marca `Rasasi`.
- Disponibilidad: **por confirmar** en la fase 3 revisando páginas de productos agotados.

### Datos a extraer por producto

| Campo | Regla |
| --- | --- |
| SupplierProductId | De la URL de la foto (`/products/{id}/`). Si hay JSON-LD con `sku` o `productID`, preferirlo y documentar cuál se usó. |
| SourceUrl | URL de la página. |
| Name | Encabezado del producto. |
| Brand | Marca mostrada sobre el nombre. |
| RegularPrice | Precio normal. |
| SalePrice | Precio de oferta, o null. |
| Images | Todas las URLs de fotos únicas, en orden. |
| Description | Texto de la descripción (texto plano, sin HTML). |
| Gender | Del nombre: `Dama` -> Women, `Caballero` -> Men, `Unisex` -> Unisex, si no -> Unspecified. |
| SizeMl | Del nombre, patrón `(\d+)\s?ml`. Null si no aparece. |
| IsAvailable | Según el marcador que se confirme en la fase 3. |

Estrategia de extracción: primero JSON-LD `schema.org/Product` si existe; si no, selectores CSS definidos en configuración (`appsettings`), no en código.

### Reglas de scraping

- Pausa entre peticiones y timeout por petición: se leen de `SyncSettings` (sección 4.1) al iniciar cada sync.
- Reintentos con backoff ante errores transitorios (5xx, timeouts).
- User-Agent, URL del sitemap y selectores CSS: en `appsettings` (`MagnaOptions`). Son ajustables sin tocar código, pero no desde el panel, porque un valor mal puesto rompe el sync completo y eso le toca al desarrollador.

## 2. Sincronización

- Corre automático una vez al día a la hora y zona horaria de `SyncSettings` (default 4:00 a.m., `America/Mexico_City`), y bajo demanda con un endpoint del admin. El sync automático se puede apagar.
- Nunca corren dos syncs al mismo tiempo.
- Pasos:
  1. Leer todas las URLs de productos del sitemap.
  2. Leer y parsear cada producto. Si uno falla, registrar el error y continuar.
  3. Si el porcentaje de productos que fallan es **mayor** a `SyncSettings.FailureThresholdPercent` (default 20), el sync se marca `Failed` y **no se aplica ningún cambio** a la base de datos.
  4. Si no, aplicar en una transacción:
     - Producto nuevo -> crearlo (marca nueva -> crearla).
     - Producto existente -> actualizar datos; si cambió el precio, agregar fila a `SupplierPrice`.
     - Producto que ya no está en el sitemap -> `IsDiscontinued = true` (nunca se borra).
     - Producto descontinuado que vuelve a aparecer -> `IsDiscontinued = false`.
  5. `IsHidden` e `IsFeatured` los controla la administradora; el sync **nunca** los modifica.
- Cada ejecución guarda un `SyncRun` con: inicio, fin, disparador (Scheduled / Manual), estado (Running / Succeeded / Failed), conteos (creados, con cambio de precio, descontinuados, reactivados, errores) y log de errores por URL.

## 3. Precio de venta

El precio de venta **no se guarda**: se calcula con `PriceCalculator` cada vez, a partir del costo y las reglas vigentes.

### Costo

`Cost = SalePrice ?? RegularPrice` del precio más reciente del proveedor.

### Regla base (una activa a la vez)

- **Ranges**: lista de rangos de costo. Cada rango: `MinCost` (inclusivo), `MaxCost` (exclusivo, null = sin límite), `Action` (`AddAmount` o `AddPercent`), `Value`.
  - Validación al guardar: el primer rango empieza en 0, no hay huecos ni traslapes, solo el último tiene `MaxCost` null.
  - Config inicial: `[0, 1000) +$200` y `[1000, ∞) +$300`.
- **Percent**: un porcentaje para todo (`BasePercent`, ej. 10).
- Cambiar de estrategia no borra los rangos.

### Excepciones (`PricingRule`)

- `Scope`: `Brand` o `Product`.
- `Action`: `AddAmount`, `AddPercent` o `FixedPrice`.
- `Value`: monto o porcentaje (puede ser 0; no negativo).
- `StartsAt` / `EndsAt` opcionales (fecha y hora, UTC). Fuera de ese rango la regla no aplica.
- `IsActive`.
- Máximo una regla **activa** por marca y una por producto.

### Orden de cálculo

1. Buscar regla aplicable: Product, si no hay -> Brand, si no hay -> base. **Las reglas no se suman.**
2. Aplicar la acción. Los porcentajes se redondean a 2 decimales (`AwayFromZero`).
3. Aplicar redondeo (`RoundingMode` en settings):
   - `None`: sin cambio.
   - `UpToNearestTen`: hacia arriba al múltiplo de 10 (975 -> 980, 1070 -> 1070).
   - `EndsInNine`: el menor valor >= precio cuyo último dígito entero sea 9 (1372 -> 1379, 1389 -> 1389, 1390 -> 1399). Los decimales se descartan hacia arriba antes (1389.10 -> 1399).
   - `FixedPrice` **no** se redondea.
4. Resultado: precio final, ganancia (`precio - costo`), regla aplicada (tipo + id) y bandera `IsLowProfit` cuando ganancia < `MinProfit` (default 100). Ganancia baja o negativa **no** bloquea nada.

### Casos de prueba obligatorios (redondeo None salvo que se diga)

| Costo | Configuración | Precio esperado |
| --- | --- | --- |
| 775 | Rangos iniciales | 975 |
| 1089 | Rangos iniciales | 1389 |
| 1000 | Rangos iniciales | 1300 (límite inclusivo) |
| 999.99 | Rangos iniciales | 1199.99 |
| 1089 | Percent 10 | 1197.90 |
| 1089 | Percent 10, UpToNearestTen | 1200 |
| 775 | Rangos iniciales, UpToNearestTen | 980 |
| 2949 | Regla de producto FixedPrice 3299 + regla de marca | 3299 |
| 2500 | Regla de marca AddPercent 8 | 2700 |
| 2500 | Regla de marca vencida (EndsAt pasado) | 2800 (base) |

## 4. Administración

- Dos usuarios admin, creados por seed desde configuración (no hay registro público).
- Casos de uso:
  - Leer y guardar `PricingSettings` (estrategia activa, porcentaje, redondeo, ganancia mínima, % de alerta de costo) y rangos.
  - Crear, editar, activar/desactivar reglas por marca o producto.
  - **Vista previa**: dado un cambio propuesto (settings, rangos o regla), devolver cuántos productos cambian y la lista con precio actual vs nuevo, sin guardar.
  - Listar productos para admin: costo, precio, ganancia, regla aplicada, disponibilidad, oculto, destacado. Filtros: búsqueda, marca, solo ganancia baja, incluir descontinuados.
  - Ocultar / mostrar y destacar / quitar destacado.
  - Disparar sync manual, ver historial de `SyncRun` y el detalle de uno.
  - Leer y guardar `SyncSettings` (sección 4.1).
  - Leer y guardar `StoreSettings` (sección 4.2).
  - Alertas: productos con ganancia menor a `PricingSettings.MinProfit`, y productos cuyo costo cambió (subió o bajó) más de `PricingSettings.CostChangeAlertPercent` (default 15) en el último sync.

### 4.1 Parámetros ajustables

Ningún número de negocio u operación vive fijo en el código. Los que ella puede cambiar desde el panel:

| Parámetro | Dónde | Default | Rango permitido |
| --- | --- | --- | --- |
| Estrategia base, % global, redondeo | PricingSettings | Ranges, 10%, None | % entre 0 y 500 |
| Ganancia mínima para alerta | PricingSettings | $100 | 0 o más |
| % de cambio de costo para alerta | PricingSettings | 15 | 1 a 100 |
| Sync automático encendido | SyncSettings | Sí | - |
| Hora del sync diario | SyncSettings | 04:00 | cualquier hora, en minutos exactos |
| Zona horaria | SyncSettings | America/Mexico_City | IDs IANA válidos |
| % máximo de fallas antes de cancelar | SyncSettings | 20 | 1 a 100 |
| Pausa entre peticiones | SyncSettings | 1000 ms | 250 a 10000 ms |
| Timeout por petición | SyncSettings | 30 s | 5 a 120 s |

- Al guardar `SyncSettings`, el job diario se reprograma (o se quita si se apagó) sin reiniciar la API.
- Un sync que ya está corriendo termina con los valores con los que empezó; los nuevos aplican desde el siguiente.
- Los valores fuera de rango se rechazan con un error claro.

### 4.2 Datos de la tienda y WhatsApp

Los clientes no compran en el catálogo: cada perfume tiene un botón que abre WhatsApp hacia el número de la tienda con un mensaje ya escrito. Todo se edita desde el panel en `StoreSettings`.

| Campo | Default | Validación |
| --- | --- | --- |
| StoreName | `Dolce Aroma` (provisional) | 1 a 80 caracteres |
| WhatsAppNumber | vacío | Solo dígitos, con lada de país, 10 a 15 dígitos (ej. `523312345678`). Al guardar se quitan espacios, guiones, paréntesis y `+`. |
| WhatsAppMessageTemplate | `Hola, me interesa el {product} ({price})` | 1 a 500 caracteres. Solo se permiten los marcadores de abajo; uno desconocido se rechaza. |

Marcadores del mensaje:

| Marcador | Valor | Ejemplo |
| --- | --- | --- |
| `{product}` | Marca + nombre | Rasasi Hawas Private Dulce Extrait de Parfum 100ml Unisex |
| `{brand}` | Marca | Rasasi |
| `{size}` | Tamaño, o vacío si no hay | 100ml |
| `{price}` | Precio de venta en formato `es-MX`, sin decimales si son ,00 | $1,390 |

- El backend arma el enlace: `https://wa.me/{WhatsAppNumber}?text={mensaje codificado para URL}`. Así el frontend no repite la lógica y se puede probar.
- Si `WhatsAppNumber` está vacío, el enlace es `null` y el frontend oculta el botón.

## 5. Catálogo público

- Solo productos con `IsAvailable && !IsDiscontinued && !IsHidden`.
- Listado paginado con búsqueda por nombre y filtros: marca, género, rango de precio de venta, solo en oferta del proveedor.
- Detalle por slug: nombre, marca, género, tamaño, descripción, fotos, precio final.
- Endpoints de marcas y destacados.
- Cada producto (en listado y detalle) incluye `WhatsAppUrl` armado según la sección 4.2.
- Endpoint de datos de la tienda: nombre y si hay WhatsApp configurado (sin exponer el template).
- **Nunca** exponer costo, ganancia, regla, URL o nombre del proveedor.
- El filtro y orden por precio usan el precio calculado. Con unos cientos de productos se puede calcular en memoria; si crece se agrega una columna cacheada.

## 6. Modelo de datos

| Entidad | Campos |
| --- | --- |
| Supplier | Id, Name, BaseUrl, IsActive |
| Brand | Id, Name, Slug (único) |
| Product | Id, SupplierId, SupplierProductId, SourceUrl, Name, Slug (único), BrandId, Gender, SizeMl?, Description, IsAvailable, IsDiscontinued, IsHidden, IsFeatured, CreatedAt, LastSeenAt. Único: (SupplierId, SupplierProductId) |
| ProductImage | Id, ProductId, Url, SortOrder |
| SupplierPrice | Id, ProductId, RegularPrice, SalePrice?, CapturedAt |
| PricingSettings | Id (fila única), ActiveBaseStrategy, BasePercent, RoundingMode, MinProfit, CostChangeAlertPercent |
| StoreSettings | Id (fila única), StoreName, WhatsAppNumber, WhatsAppMessageTemplate |
| SyncSettings | Id (fila única), IsDailySyncEnabled, DailySyncTime, TimeZoneId, FailureThresholdPercent, DelayBetweenRequestsMs, RequestTimeoutSeconds |
| PriceRange | Id, MinCost, MaxCost?, Action, Value |
| PricingRule | Id, Scope, BrandId?, ProductId?, Action, Value, StartsAt?, EndsAt?, IsActive, CreatedAt |
| SyncRun | Id, StartedAt, FinishedAt?, Trigger, Status, CreatedCount, PriceChangedCount, DiscontinuedCount, ReactivatedCount, ErrorCount |
| SyncRunError | Id, SyncRunId, Url, Message |

Dinero: `numeric(12,2)`. Fechas: `timestamptz` en UTC.
