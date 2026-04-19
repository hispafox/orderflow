# ROADMAP COMPLETO — 27 Presentaciones

## Microservicios con .NET 10

---

## HILO CONDUCTOR: Proyecto OrderFlow

A lo largo de las 27 presentaciones construimos **OrderFlow**, un sistema de e-commerce que evoluciona desde un monolito hasta una arquitectura de microservicios completa en producción.

### La historia de OrderFlow

**La empresa ficticia:** TechShop, una tienda online de productos tecnológicos que ha crecido rápido. Empezó con 3 developers y un monolito ASP.NET. Ahora tiene 18 developers, 50.000 pedidos/mes, y el monolito les está frenando.

**El reto del curso:** Acompañamos a TechShop en su migración de monolito a microservicios. Cada presentación avanza un paso en esa migración.

### Los 4 microservicios que construimos

|Servicio|Responsabilidad|Enfoque API|Base de datos|Se introduce en|
|---|---|---|---|---|
|**Orders.API**|Ciclo de vida de pedidos|**Controllers** (MVC)|SQL Server (EF Core 10)|M2.1 (esqueleto) → M3.1 (dominio) → M3.2 (Controllers)|
|**Products.API**|Catálogo y stock|**Minimal APIs**|SQL Server (EF Core 10)|M4.1 (nuevo servicio)|
|**Payments.API**|Procesamiento de pagos|**Minimal APIs**|SQL Server (EF Core 10)|M4.3 (saga)|
|**Notifications.API**|Emails y alertas|**Minimal APIs**|SQL Server (EF Core 10)|M4.2 (eventos)|

**¿Por qué Controllers en Orders y Minimal APIs en el resto?**

- Orders es el servicio más complejo: muchos endpoints, CQRS, saga orchestrator, validaciones ricas. Controllers aportan estructura con filters, convenciones REST y organización por controlador.
- Products, Payments y Notifications son servicios más ligeros con menos endpoints. Minimal APIs les da menos ceremony y más velocidad de desarrollo.
- En la vida real, ambos enfoques conviven en el mismo ecosistema. El alumno ve las dos aproximaciones en contexto.

**Base de datos:**

- **Desarrollo local:** SQL Server LocalDB (incluido con Visual Studio, cero configuración)
- **Producción:** Azure SQL Database
- EF Core 10 en todos los servicios con migrations independientes por servicio

### Progresión del proyecto por presentación

```
M1.1  → Conocemos TechShop y su problema (narrativa)
M1.2  → Vemos su monolito actual, analizamos los dolores
M1.3  → Diseñamos los bounded contexts de OrderFlow (Event Storming)
M2.1  → Creamos el esqueleto de Orders.API (.NET 10, SQL Server LocalDB)
M2.2  → Añadimos configuración, DI avanzada, health checks
M2.3  → Añadimos Serilog + OpenTelemetry
M2.4  → Escribimos tests unitarios e integración
M3.1  → Diseñamos el dominio de Orders (entidades, value objects)
M3.2  → Construimos los endpoints de Orders con Controllers (MVC)
M3.3  → Enseñamos Minimal APIs (Products/Payments/Notifications las usarán)
M3.4  → Implementamos CQRS + MediatR en Orders (Controllers + MediatR)
M3.5  → Añadimos Polly para resiliencia
M3.6  → Laboratorio: Orders.API funcional completo (Controllers)
M4.1  → Creamos Products.API con Minimal APIs, comunicación Orders→Products
M4.2  → Añadimos RabbitMQ + MassTransit, creamos Notifications.API (Minimal APIs)
M4.3  → Creamos Payments.API (Minimal APIs), implementamos Saga de pedidos
M4.4  → Ponemos YARP como API Gateway delante de todo
M5.1  → Añadimos JWT + OAuth 2.0 al sistema
M5.2  → mTLS entre servicios, Key Vault para secretos
M5.3  → Rate limiting, validación, auditoría
M6.1  → SQL Server con EF Core 10 en profundidad para cada servicio
M6.2  → Outbox pattern en Orders para garantizar eventos
M6.3  → Read model separado con proyecciones en Orders
M6.4  → Simulamos migración del monolito de TechShop al nuevo esquema
M7.1  → Aspire en profundidad + Docker/K8s como conceptos teóricos
M7.2  → Desplegamos en Azure: App Services + Azure SQL + Azure Service Bus
M7.3  → Pipelines CI/CD con GitHub Actions → Azure App Services
M7.4  → Observabilidad con Application Insights + Azure Monitor
```

---

# ROADMAP DETALLADO POR PRESENTACIÓN

## Regla de no repetición

Cada concepto se explica UNA SOLA VEZ — en la presentación donde tiene más sentido. Las presentaciones posteriores lo USAN pero no lo re-explican. Si una slide necesita referir un concepto anterior, dice "como vimos en M2.3" y sigue adelante.

---

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

# MÓDULO 1 — INTRODUCCIÓN (2.5h)

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

## M1.1 — ¿Qué son los microservicios y por qué existen? (45 min)

**Hilo conductor:** Presentamos TechShop y su problema de crecimiento.

**Temas EXCLUSIVOS de esta presentación (no se repiten después):**

- Definición de microservicio y lo que "micro" NO significa
- Heurísticas de tamaño correcto (regla del equipo, de la reescritura, del nombre)
- Historia completa: mainframes → cliente-servidor → SOA → microservicios → madurez
- Casos históricos: memo de Bezos (Amazon), Netflix (migración 7 años), Spotify (squads)
- Caso de vuelta atrás: Segment (de microservicios a monolito)
- Las 6 características fundamentales (definición de cada una)
- Ley de Conway y la Inverse Conway Maneuver
- "You build it, you run it" como filosofía
- Las 8 falacias de la computación distribuida (Peter Deutsch)
- Beneficios reales con datos DORA
- Costes reales (resumen ejecutivo — cada coste se profundiza en su módulo)
- Cuándo SÍ y cuándo NO usar microservicios (framework de decisión)
- El espectro de arquitecturas (monolito → modular → macro → micro → nano)
- Checklist de preparación

**NO se toca aquí (se ve después):**

- ✗ Comparativa detallada monolito vs micro (→ M1.2)
- ✗ Strangler Fig Pattern (→ M1.2)
- ✗ Patrones de arquitectura en profundidad (→ M1.3)
- ✗ Ningún código ni demo
- ✗ DDD en profundidad (→ M1.3 intro, M3.1 profundidad)

---

## M1.2 — Monolito vs. Microservicios: la comparativa honesta (45 min)

**Hilo conductor:** Analizamos el monolito actual de TechShop. Mostramos su estructura, identificamos los dolores, evaluamos alternativas.

**Temas EXCLUSIVOS de esta presentación:**

- Anatomía de un monolito .NET bien hecho (capas, módulos, una sola DB)
- Ventajas reales del monolito (simplicidad, debugging, ACID, refactoring)
- Señales de que el monolito empieza a doler (build lento, deploys de miedo, tabla monstruo)
- Comparativa detallada aspecto por aspecto: · Desarrollo y productividad del equipo · Despliegue y release management · Escalado horizontal vs vertical · Consistencia de datos (ACID vs eventual) — solo concepto, no implementación · Debugging y diagnóstico — solo concepto, no herramientas · Testing — solo comparativa, no herramientas concretas · Coste de infraestructura · Onboarding de nuevos developers
- El monolito modular como arquitectura intermedia: · Definición y reglas · Implementación en .NET 10 (módulos como proyectos, DbContext separados) · Cuándo es suficiente y cuándo no
- Strangler Fig Pattern: · Concepto y fases · Ejemplo concreto con YARP como proxy (solo concepto, YARP se profundiza en M4.4) · Migración gradual del monolito de TechShop
- Cuándo evolucionar de modular a microservicios (señales)
- El monolito distribuido: qué es, señales, cómo evitarlo

**NO se toca aquí:**

- ✗ Patrones específicos (Circuit Breaker, Saga, CQRS) — solo se mencionan en la tabla comparativa
- ✗ Herramientas concretas (Polly, MassTransit, YARP) — se nombran, no se explican
- ✗ Código funcional — solo diagramas y pseudocódigo

---

## M1.3 — Patrones fundamentales de arquitectura (60 min)

**Hilo conductor:** Diseñamos la arquitectura de OrderFlow. Hacemos Event Storming para identificar bounded contexts, mapeamos los patrones que vamos a necesitar.

**Temas EXCLUSIVOS de esta presentación:**

- DDD para microservicios (introducción operativa, NO teórica): · Bounded Context como criterio de partición · Ubiquitous Language — cada contexto habla su idioma · Aggregates como unidad de consistencia (concepto — código en M3.1) · Context Mapping: relaciones entre contextos (Shared Kernel, Customer-Supplier, Conformist, Anti-corruption Layer)
- Event Storming como workshop de diseño: · Cómo hacerlo (post-its, flujo, resultado) · Event Storming aplicado a OrderFlow: eventos, comandos, aggregates, bounded contexts · Resultado: mapa de 4 servicios con sus interacciones
- Catálogo de patrones (SOLO visión general y cuándo usar cada uno — cada patrón se profundiza en su módulo): · API Gateway → visión general, se profundiza en M4.4 · Service Discovery → cómo se encuentran, se profundiza en M7.2 (Kubernetes DNS) · Circuit Breaker → qué hace, se profundiza en M3.5 · Saga → qué resuelve, se profundiza en M4.3 · CQRS → concepto, se profundiza en M3.3 y M6.3 · Event Sourcing → concepto, se profundiza en M6.3 · Sidecar / Dapr → concepto, se menciona en M4.2 · Outbox → concepto, se profundiza en M6.2 · BFF (Backend for Frontend) → concepto completo aquí
- Mapa de arquitectura de OrderFlow: · Diagrama completo con los 4 servicios · Flujo de un pedido de principio a fin · Interacciones síncronas y asíncronas identificadas · Tecnologías elegidas para cada servicio (justificación)

**NO se toca aquí:**

- ✗ Implementación de ningún patrón (solo concepto + cuándo usarlo)
- ✗ Código de ningún tipo
- ✗ Herramientas concretas (Polly, MassTransit, YARP, etc.)
- ✗ DDD táctico en detalle (entities, value objects, domain events → M3.1)

---

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

# MÓDULO 2 — FUNDAMENTOS DE .NET 10 (3h)

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

## M2.1 — El ecosistema .NET 10 para microservicios (45 min)

**Hilo conductor:** Creamos el esqueleto de Orders.API con SQL Server LocalDB. Primer código del curso.

**Temas EXCLUSIVOS:**

- .NET 10 como plataforma para microservicios: · Novedades relevantes: LTS, C# 14, NativeAOT mejorado, rendimiento JIT · ASP.NET Core 10: modelo de hosting, pipeline de middleware · Benchmarks de rendimiento (TechEmpower)
- Minimal APIs vs Controllers: · Comparativa con código real · Cuándo usar cada uno (recomendación: Minimal APIs para microservicios) · NO se profundiza en Minimal APIs (→ M3.2)
- Estructura de proyecto de un microservicio: · Program.cs como punto de entrada · Estructura de carpetas: Features vs Layers (presentar, no decidir — decisión en M3.1) · .csproj y dependencias base
- .NET Aspire 9.1: · Qué es y qué resuelve (composición en desarrollo) · AppHost, ServiceDefaults, Dashboard · Configuración de Aspire para OrderFlow · Aspire como orquestador local (reemplaza docker-compose en desarrollo)
- Primer código: esqueleto de Orders.API · Crear proyecto con `dotnet new webapi` · Program.cs mínimo con un endpoint de prueba · Verificar que arranca y responde

**NO se toca aquí:**

- ✗ DI avanzada (→ M2.2)
- ✗ Health checks (→ M2.2)
- ✗ Logging (→ M2.3)
- ✗ Testing (→ M2.4)
- ✗ Minimal APIs en profundidad (→ M3.2)
- ✗ Dockerfile (→ M7.1)

---

## M2.2 — Inyección de dependencias y configuración avanzada (45 min)

**Hilo conductor:** Añadimos configuración robusta y health checks a Orders.API.

**Temas EXCLUSIVOS:**

- DI nativa en .NET — más allá de lo básico: · Scoped, Transient, Singleton: cuándo cada uno con diagrama de lifecycle · Trampas: captive dependencies (Singleton que captura Scoped) · Registros con interfaces vs implementaciones concretas · Keyed Services: registrar múltiples implementaciones con clave · Ejemplo: registrar servicios de dominio en Orders.API
- Patrón Options: · IOptions<T>, IOptionsSnapshot<T>, IOptionsMonitor<T>: diferencias · Validación de opciones con DataAnnotations · Validación custom con IValidateOptions · Configuration binding desde appsettings · Ejemplo: OrdersSettings con validación
- Configuración por entorno: · appsettings.Development.json vs appsettings.Production.json · User Secrets para desarrollo local · Variables de entorno en contenedores (concepto — Docker en M7.1) · Jerarquía de configuración y sobreescritura
- Secrets management (introducción conceptual): · Por qué NO en appsettings.json · Azure Key Vault, AWS Secrets Manager, HashiCorp Vault (nombrar) · Integración con .NET: AddAzureKeyVault (ejemplo básico — se profundiza en M5.2)
- Health Checks: · Liveness vs Readiness vs Startup probes (concepto — Kubernetes los consume en M7.2) · Implementación de health checks custom: verificar DB, servicios externos · AspNetCore.Diagnostics.HealthChecks como librería · Health check de la conexión SQL Server de Orders · UI de health checks · Endpoint /health y /health/ready en Orders.API

**NO se toca aquí:**

- ✗ Secrets management en profundidad (→ M5.2)
- ✗ Cómo Kubernetes consume los probes (→ M7.2)
- ✗ Configuración en Docker/Kubernetes (→ M7.1, M7.2)

---

## M2.3 — Logging, observabilidad y diagnóstico (45 min)

**Hilo conductor:** Añadimos observabilidad completa a Orders.API. Al final de esta presentación, cada petición queda registrada con logs estructurados y trazas.

**Temas EXCLUSIVOS:**

- ¿Por qué structured logging? · Console.WriteLine vs logging framework: ejemplo antes/después · Propiedades con nombre vs strings concatenados · Búsqueda en logs: por OrderId, por UserId, por CorrelationId
- Serilog como estándar de facto: · Configuración en .NET 10 (Program.cs) · Sinks: Console, Seq, Elasticsearch, File · Enrichers: Machine, Thread, Environment, Property · Log levels: cuándo usar cada uno (la gente abusa de Information) · Request logging middleware · Ejemplo: configurar Serilog completo en Orders.API
- Correlation IDs: · El problema: una petición cruza 5 servicios, ¿cómo la rastreo? · Implementación: middleware que genera/propaga el CorrelationId · Header X-Correlation-Id en HTTP · Enriquecer Serilog con el CorrelationId
- OpenTelemetry — trazas y métricas: · Los tres pilares: logs, traces, metrics · Activity y ActivitySource en .NET · Instrumentación automática: HTTP, EF Core, gRPC · Exporters: Jaeger, Zipkin, OTLP, Console · Configuración del pipeline de telemetría en Program.cs · Ejemplo: OpenTelemetry completo en Orders.API
- Métricas con System.Diagnostics.Metrics: · Counters, Histograms, Gauges · Métricas de negocio: pedidos creados, tiempo de procesamiento · Exportar a Prometheus (concepto — dashboard en M7.4)
- Dashboard de .NET Aspire 9.1: · Ver trazas, logs y métricas en el dashboard local · Cómo Aspire recolecta telemetría automáticamente

**NO se toca aquí:**

- ✗ Grafana, Prometheus, Loki, Tempo como stack de producción (→ M7.4)
- ✗ Alerting (→ M7.4)
- ✗ Distributed tracing entre múltiples servicios (→ se ve cuando hay múltiples servicios en M4.1+)
- ✗ Sampling strategies (→ M7.4)

---

## M2.4 — Testing en microservicios .NET (45 min)

**Hilo conductor:** Escribimos la suite de tests de Orders.API. Al final, tenemos tests unitarios, de integración y un contract test.

**Temas EXCLUSIVOS:**

- La pirámide de testing en microservicios: · Unitarios → Integración → Contract → E2E · Por qué la capa de contract tests es nueva y esencial · El antipatrón: pirámide invertida (todo E2E, nada unitario)
- Unit testing con xUnit: · Estructura Arrange-Act-Assert · Naming conventions · Ejemplo: tests del dominio de Orders (validaciones, cambios de estado)
- Mocking con NSubstitute: · Por qué NSubstitute sobre Moq (syntax más limpia) · Mocking de interfaces de repositorio · Verificación de llamadas · Ejemplo: testear OrderService mockeando el repositorio
- Integration testing con WebApplicationFactory: · Levantar el servidor en memoria · Custom WebApplicationFactory para inyectar mocks · Ejecutar peticiones HTTP reales contra la API · Verificar respuestas (status, body, headers) · Ejemplo: test de integración de POST /api/orders
- LocalDB para bases de datos reales: · Por qué bases de datos reales en tests (no InMemory) · Configuración de LocalDbFixture con SQL Server LocalDB · Ejemplo: test que verifica persistencia real
- Contract testing con Pact: · El problema: cambias un endpoint y rompes al consumidor · Consumer-driven contract testing · PactNet para .NET · Flujo: consumidor define contrato → productor verifica · Ejemplo: contrato entre Orders y Products (que crearemos en M4.1)
- Testing de resiliencia (introducción): · Simular fallos, timeouts, circuit breakers · Concepto — implementación con Polly en M3.5

**NO se toca aquí:**

- ✗ Tests de resiliencia con Polly (→ M3.4)
- ✗ Tests E2E con múltiples servicios (→ se hacen cuando hay múltiples servicios)
- ✗ Chaos engineering (→ M7.4)

---

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

# MÓDULO 3 — DESARROLLO DE MICROSERVICIOS (5h 45min)

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

## M3.1 — Diseño de un microservicio: del dominio al código (60 min)

**Hilo conductor:** Diseñamos e implementamos el modelo de dominio de Orders.API. De los bounded contexts del M1.3 al código real.

**Temas EXCLUSIVOS:**

- DDD Táctico (implementación en código): · Entities: identidad, igualdad por Id, ciclo de vida · Value Objects: igualdad por valor, inmutabilidad (records en C# 14) · Aggregates: reglas de consistencia, Aggregate Root · Domain Events: eventos que ocurren dentro del bounded context · Implementación en C# 14: records, init properties, required members · Ejemplo completo: Order (Aggregate Root), OrderLine (Entity), Money/Address (Value Objects)
- Clean Architecture (Uncle Bob): · Diagrama: capas concéntricas — Domain, Application, Infrastructure, API · La Dependency Rule: el código solo apunta hacia adentro · Domain no conoce EF Core ni HTTP — solo define interfaces · El dominio NO sabe de HTTP, EF Core, ni infraestructura · Estructura de carpetas: Domain, Application, Infrastructure, API · Ejemplo: estructura de Orders.API con Clean Architecture
- Vertical Slice Architecture como alternativa: · Organizar por feature en vez de por capa · Cada slice: handler + validator + repositorio + endpoint · Cuándo Vertical Slices gana a Clean Architecture: servicios pequeños · Ejemplo: un slice completo de CreateOrder
- Rich Domain Model vs Anemic Domain Model: · Anemic: entidades con solo getters/setters, lógica fuera · Rich: entidades con comportamiento, invariantes protegidas · Por qué Rich Model para el write side, Anemic para el read side · Ejemplo: Order.AddLine() con validación de negocio vs OrderDto plano
- Invariantes de negocio en el dominio: · Reglas de negocio como código: "Un pedido no puede tener más de 50 líneas" · Guard clauses y domain exceptions · Ejemplo: reglas de negocio de Order implementadas

**NO se toca aquí:**

- ✗ Event Storming (ya hecho en M1.3)
- ✗ Bounded contexts (ya definidos en M1.3)
- ✗ Controllers ni Minimal APIs (→ M3.2 y M3.3)
- ✗ MediatR / CQRS (→ M3.4)
- ✗ Persistencia con EF Core (→ se conecta en M3.2/M3.3 para la API y M6.1 para profundidad)

---

## M3.2 — APIs con Controllers: el enfoque clásico de MVC (45 min)

**Hilo conductor:** Construimos los endpoints de Orders.API con Controllers. El alumno entiende el patrón MVC completo antes de ver la alternativa moderna. Al final de esta presentación, la API funciona con Controllers — en M3.3 la migraremos a Minimal APIs.

**Temas EXCLUSIVOS:**

- El patrón MVC en ASP.NET Core: · Model-View-Controller: el flujo de una petición HTTP · Routing por convención vs routing por atributos · El pipeline de middleware: cómo llega una petición al Controller · Diagrama: HTTP Request → Middleware → Routing → Controller → Response
- Anatomía de un Controller: · Herencia de ControllerBase vs Controller (cuándo cada uno) · [ApiController] attribute: qué activa (model validation automática, binding, ProblemDetails) · Inyección de dependencias en el constructor · Ejemplo: OrdersController completo con todos los endpoints CRUD
- Routing en Controllers: · [Route("api/[controller]")]: convención · [HttpGet], [HttpPost], [HttpPut], [HttpDelete]: verbos HTTP · [HttpGet("{id}")]: parámetros de ruta · [FromBody], [FromQuery], [FromRoute], [FromHeader]: binding explícito · Route constraints: {id:int}, {id:guid} · Ejemplo: rutas de OrdersController
- Model Binding y validación: · Cómo ASP.NET Core bindea el request al modelo · DataAnnotations: [Required], [StringLength], [Range] · Validación automática con [ApiController] · ModelState y validación manual · Ejemplo: CreateOrderRequest con DataAnnotations
- ActionResult y tipos de respuesta: · ActionResult<T> vs IActionResult · Ok(), Created(), NotFound(), BadRequest(), NoContent() · CreatedAtAction() para POST con Location header · Respuestas tipadas vs no tipadas · Ejemplo: cada endpoint devolviendo el tipo correcto
- Filters en MVC: · Authorization Filters · Action Filters: antes/después de la acción · Exception Filters: manejo centralizado de errores · Result Filters: antes/después del resultado · El pipeline de filtros: orden de ejecución · Ejemplo: ExceptionFilter que convierte excepciones de dominio en ProblemDetails · Ejemplo: LoggingActionFilter que registra cada petición
- Content Negotiation: · JSON como formato por defecto · System.Text.Json vs Newtonsoft.Json · Configuración de serialización: camelCase, null handling, enums as strings · Formatters custom
- Swagger / OpenAPI con Controllers: · Generación automática desde los Controllers · XML comments como documentación · [ProducesResponseType] para documentar respuestas · Ejemplo: OrdersController documentado con Swagger
- Patrones comunes con Controllers: · Controller gordo vs Controller delegador (recomendación: delegar al servicio) · Un Controller por recurso REST · Agrupación con [ApiExplorerSettings] y [Tags] · Areas para organizar Controllers en APIs grandes
- Limitaciones de Controllers para microservicios: · Ceremony: herencia, atributos, convenciones implícitas · Más código para lo mismo (comparación de líneas) · Binding magic: a veces difícil de debuggear · Arranque: reflexión para descubrir Controllers (impacto en startup) · Introducción a por qué Minimal APIs nació como alternativa

**NO se toca aquí:**

- ✗ Minimal APIs (→ M3.3 — contraste directo)
- ✗ FluentValidation (→ M3.3 — como mejora sobre DataAnnotations)
- ✗ Versionado de APIs (→ M3.4)
- ✗ Rate limiting (→ M3.4)
- ✗ gRPC (→ M4.1)
- ✗ Autenticación [Authorize] (→ M5.1)

---

## M3.3 — APIs con Minimal APIs y OpenAPI (60 min)

**Hilo conductor:** Aprendemos Minimal APIs construyendo endpoints de ejemplo y preparando el terreno para Products, Payments y Notifications, que usarán este enfoque. Comparamos con los Controllers de Orders.API — el alumno ve el contraste directo y entiende cuándo usar cada uno.

**Temas EXCLUSIVOS:**

- De Controllers a Minimal APIs — el contraste: · Lado a lado: el mismo endpoint en Controller vs Minimal API · Qué desaparece: herencia, atributos, convenciones implícitas · Qué ganas: explícito, funcional, menos ceremony · Benchmark: startup time y throughput (Controllers vs Minimal APIs) · Cuándo Controllers (APIs complejas como Orders) vs Minimal APIs (servicios ligeros como Products) · Decisión para OrderFlow: Orders se queda con Controllers, el resto usará Minimal APIs
- Minimal APIs en profundidad: · Route groups y organización por feature · Extension methods en IEndpointRouteBuilder para separar en clases · Parameter binding: route, query, body, header, DI (contraste con [FromX] de Controllers) · TypedResults: Ok(), Created(), NotFound() — tipado en compilación · AsParametersAttribute para binding complejo · Ejemplo: construir endpoints de prueba que simularán la API de Products
- Endpoint Filters (el equivalente moderno de Action Filters): · Antes/después del handler · Composición de filtros · Contraste con Action Filters de MVC · Ejemplo: ValidationFilter, LoggingFilter
- FluentValidation como mejora sobre DataAnnotations: · Por qué FluentValidation > DataAnnotations para microservicios · Reglas complejas, validación condicional, mensajes ricos · Integración con Minimal APIs via endpoint filter · Integración con Controllers via action filter (también se puede usar en Orders) · Ejemplo: CreateProductValidator (preparando Products.API)
- Problem Details (RFC 9457): · Formato estándar de errores HTTP · Configuración en .NET 10 (funciona con Controllers y Minimal APIs) · Mapeo de excepciones de dominio a Problem Details · Middleware unificado para ambos enfoques
- OpenAPI 3.1: · Generación automática con Minimal APIs · Contraste con XML comments de Controllers (M3.2) · .WithDescription(), .WithTags(), .Produces<T>(), .WithOpenApi() · Generar clientes tipados desde la spec (Kiota, NSwag) — concepto, se usa en M4.1
- Versionado de APIs: · Asp.Versioning: URL versioning (/v1/orders), header versioning · Funciona con Controllers y Minimal APIs · Estrategia de deprecación · Ejemplo: versionado aplicado a OrderFlow
- Rate limiting nativo: · Fixed window, Sliding window, Token bucket, Concurrency limiter · Configuración por endpoint o global · Funciona con ambos enfoques · Nota: rate limiting en Gateway vs en servicio (→ M4.4)
- Convivencia Controllers + Minimal APIs: · Ambos pueden coexistir en el mismo proyecto · Cómo organizar: Controllers en /Controllers, Minimal APIs en /Endpoints · Ejemplo: Orders.API con Controllers + un endpoint de health check con Minimal API

**NO se toca aquí:**

- ✗ Anatomía de Controllers (→ ya en M3.2)
- ✗ Action Filters clásicos (→ ya en M3.2)
- ✗ DataAnnotations (→ ya en M3.2, aquí FluentValidation como evolución)
- ✗ Generación de clientes para consumir otra API (→ M4.1)
- ✗ gRPC (→ M4.1)
- ✗ Autenticación/autorización en endpoints (→ M5.1)
- ✗ Rate limiting en API Gateway (→ M4.4)

---

## M3.4 — Patrones de implementación: CQRS y MediatR (60 min)

**Hilo conductor:** Refactorizamos Orders.API para usar CQRS con MediatR. Separamos comandos de queries con pipeline behaviors.

**Temas EXCLUSIVOS:**

- CQRS en profundidad (implementación, NO concepto que ya se vio en M1.3): · Write Model vs Read Model: por qué modelos diferentes · Commands: operaciones que cambian estado (CreateOrder, CancelOrder) · Queries: operaciones que leen datos (GetOrderById, ListOrders) · Separación en el mismo servicio (misma DB por ahora — DB separada en M6.3)
- MediatR como implementación: · IRequest, IRequestHandler, INotification, INotificationHandler · Registrar MediatR en .NET 10 · El flujo: Endpoint → MediatR.Send(command) → Handler → Resultado · Ejemplo completo: CreateOrderCommand + CreateOrderHandler
- Commands y Queries como records: · Inmutabilidad, serialización, pattern matching · Ejemplo: todos los commands y queries de Orders
- Pipeline Behaviors: · Cross-cutting concerns sin repetir código · Behavior de validación: FluentValidation automático antes del handler · Behavior de logging: registrar cada command/query · Behavior de transacciones: envolver en una transacción de BD · Behavior de caching para queries frecuentes · Ejemplo: pipeline completo (Validation → Logging → Transaction → Handler)
- Domain Events con MediatR: · INotification como domain event · Publicar desde el Aggregate Root · Handlers que reaccionan (OrderCreatedDomainEvent → actualizar read model) · Diferencia entre domain events (internos) e integration events (entre servicios → M4.2)
- Conectar CQRS con Minimal APIs: · Endpoints que solo hacen MediatR.Send() — cero lógica en el endpoint · Ejemplo: Orders endpoints refactorizados con MediatR

**NO se toca aquí:**

- ✗ Integration events / mensajería entre servicios (→ M4.2)
- ✗ Event Sourcing como event store (→ M6.3)
- ✗ Read model en BD separada (→ M6.3)
- ✗ Proyecciones (→ M6.3)
- ✗ Controllers (→ ya migrados a Minimal APIs en M3.3)

---

## M3.5 — Resiliencia y manejo de errores (60 min)

**Hilo conductor:** Añadimos resiliencia a Orders.API para cuando necesite llamar a otros servicios (Products, Payments). Preparamos la infraestructura de resiliencia antes de crear los otros servicios.

**Temas EXCLUSIVOS:**

- Polly v8 — la nueva API: · ResiliencePipelineBuilder: composición de estrategias · Pipeline = Retry + CircuitBreaker + Timeout (en ese orden) · Configuración declarativa vs programática
- Retry con exponential backoff y jitter: · Por qué backoff (no saturar al servicio caído) · Por qué jitter (evitar thundering herd) · Configuración: maxRetries, delay, backoffType, jitter · Gráfica: reintentos en el tiempo
- Circuit Breaker en profundidad (implementación): · Estados: cerrado, abierto, semi-abierto (diagrama de estados) · Configuración: failureRatio, samplingDuration, minimumThroughput, breakDuration · Ejemplo: circuit breaker para llamadas a Products.API
- Timeout — aggressive vs optimistic: · Aggressive: cancela la operación inmediatamente · Optimistic: respeta CancellationToken · Cuándo usar cada uno
- Bulkhead — aislamiento de recursos: · Limitar ejecuciones concurrentes · Evitar que un servicio lento consuma todos los threads · Configuración: maxParallelization, maxQueuingActions
- Fallback strategies: · Datos cacheados, respuesta degradada, valor por defecto · Ejemplo: si Products no responde, devolver producto de caché
- HttpClientFactory + Polly: · Named clients vs Typed clients · Añadir resilience handler al HttpClient · Ejemplo: ProductsApiClient con retry + circuit breaker + timeout
- Idempotencia: · Si reintentas, la operación debe ser segura de repetir · Idempotency keys: concepto e implementación · Header Idempotency-Key · Ejemplo: idempotencia en CreateOrder
- Testing de resiliencia: · Simular fallos con HttpMessageHandler falso · Verificar que el circuit breaker se abre · Verificar que el fallback devuelve datos correctos

**NO se toca aquí:**

- ✗ Outbox pattern (→ M6.2)
- ✗ Saga como patrón de resiliencia entre servicios (→ M4.3)
- ✗ Chaos engineering en producción (→ M7.4)
- ✗ Health degradation (→ M7.4, como parte de monitorización)

---

## M3.6 — Práctica guiada: microservicio completo (60 min)

**Hilo conductor:** Consolidamos todo el Módulo 2 y 3 en un laboratorio. Orders.API debe quedar 100% funcional.

**Temas EXCLUSIVOS (no hay conceptos nuevos — es integración):**

- Checklist de lo que Orders.API debe tener: · Dominio: Order, OrderLine, value objects, reglas de negocio (M3.1) · API: **Controllers** con DataAnnotations + FluentValidation, Swagger, Problem Details (M3.2) · CQRS: Commands y Queries con MediatR (M3.4) · Persistencia: EF Core 10 con SQL Server LocalDB (setup aquí, profundidad en M6.1) · Observabilidad: Serilog + OpenTelemetry (M2.3) · Health checks (M2.2) · Resiliencia: Polly configurado para llamadas externas (M3.5) · Tests: unitarios (dominio), integración (WebApplicationFactory), contract (Pact) (M2.4)
- Ejercicio guiado paso a paso: · Conectar EF Core con SQL Server LocalDB (migration, DbContext) · Verificar el flujo completo: crear pedido → persistir → leer → listar · Ejecutar la suite de tests · Ver las trazas en el dashboard de Aspire
- Puntos de checkpoint cada 10 min
- Troubleshooting de errores comunes
- Extensiones para quien vaya rápido

---

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

# MÓDULO 4 — COMUNICACIÓN (4h)

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

## M4.1 — Comunicación síncrona: REST y gRPC (60 min)

**Hilo conductor:** Creamos Products.API y establecemos la comunicación síncrona Orders→Products. Primer momento en que dos servicios se hablan.

**Temas EXCLUSIVOS:**

- REST con HttpClient en .NET: · IHttpClientFactory: por qué no crear HttpClient a mano · Typed clients: encapsular llamadas en una clase · Serialización/deserialización con System.Text.Json
- Refit como alternativa declarativa: · Define la interfaz, Refit genera el código · Ejemplo: IProductsApiClient con Refit
- gRPC en .NET 10: · Protocol Buffers: definir contratos en .proto · Grpc.AspNetCore (server) y Grpc.Net.Client (client) · Tipos de streaming: unary, server, client, bidirectional · Code generation desde .proto files
- REST vs gRPC — comparativa: · Rendimiento, formato, tooling, use cases · Recomendación: REST público, gRPC interno
- Generación de clientes tipados: · Kiota/NSwag desde OpenAPI spec (para REST) · dotnet-grpc desde .proto files (para gRPC) · Service references en .NET
- Crear Products.API: · Esqueleto del servicio con **Minimal APIs** (contraste con Orders que usa Controllers) · Endpoints: GET /products, GET /products/{id}, PUT /products/{id}/stock · SQL Server con EF Core 10 (LocalDB en desarrollo) · ProductDbContext independiente de OrderDbContext (database per service con SQL Server)
- Comunicación Orders → Products: · Orders (Controllers) necesita verificar stock al crear pedido · Implementar ProductsApiClient con HttpClientFactory + Polly · Ver la traza distribuida en Aspire (dos servicios)
- Patrones de comunicación síncrona: · Request/Response directo · API Composition: agregar datos de varios servicios

**NO se toca aquí:**

- ✗ Mensajería asíncrona (→ M4.2)
- ✗ BFF pattern (ya visto en M1.3)
- ✗ API Gateway (→ M4.4)

---

## M4.2 — Comunicación asíncrona: mensajería y eventos (60 min)

**Hilo conductor:** Añadimos RabbitMQ + MassTransit al sistema. Creamos Notifications.API. Los servicios empiezan a comunicarse por eventos.

**Temas EXCLUSIVOS:**

- ¿Por qué comunicación asíncrona? · Acoplamiento temporal: si B está caído, A también falla · Mensajería desacopla en tiempo · Cuándo síncrono, cuándo asíncrono (criterios de decisión)
- Conceptos de mensajería: · Messages vs Events · Commands (dirigidos) vs Events (publicados) · Topics, Queues, Subscriptions, Consumer Groups
- RabbitMQ para desarrollo local: · Configurar RabbitMQ como recurso de .NET Aspire (cero instalación manual) · Exchanges: direct, topic, fanout, headers · Queues, bindings, routing · Durable queues, persistent messages, acknowledgements · Dead Letter Queues (DLQ) para mensajes fallidos
- MassTransit como abstracción: · Por qué no usar el cliente de RabbitMQ directo · Consumers, publish, send · Configuración en .NET 10 · Retry, circuit breaker integrados · **La ventaja clave: cambiar de RabbitMQ a Azure Service Bus = cambiar configuración** · Ejemplo: configurar MassTransit para RabbitMQ en local
- Azure Service Bus para producción (concepto): · Qué es: servicio managed de mensajería en Azure · Topics, Subscriptions, Queues · Por qué Azure Service Bus en producción: managed, escalable, sin mantenimiento · MassTransit: cambiar `.UsingRabbitMq()` por `.UsingAzureServiceBus()` — una línea · Se configura en el deploy (→ M7.2)
- Kafka (introducción): · Cuándo Kafka vs RabbitMQ: alto volumen, replay, event sourcing · Confluent.Kafka para .NET · Consumer groups, particiones, offsets · Solo concepto — no lo implementamos en OrderFlow
- Integration Events: · Diferencia con Domain Events (vistos en M3.3) · Definir contratos de eventos: OrderCreated, OrderCancelled · Publicar desde Orders.API con MassTransit · Namespace compartido de contratos (NuGet package)
- Crear Notifications.API: · Servicio reactivo con **Minimal APIs**: solo consume eventos, pocos endpoints · Consumer de OrderCreated → envía email (simulado) · Consumer de OrderCancelled → envía notificación · SQL Server con EF Core 10 para almacenar estado de notificaciones (NotificationDbContext)
- Patrones de mensajería: · Publish/Subscribe · Competing Consumers · Message Deduplication (por MessageId) · Poison Message Handling (mover a DLQ tras N intentos)

**NO se toca aquí:**

- ✗ Outbox pattern (→ M6.2)
- ✗ Saga pattern (→ M4.3)
- ✗ Event Sourcing (→ M6.3)
- ✗ Kafka en profundidad (solo intro aquí)

---

## M4.3 — Saga Pattern: transacciones distribuidas (60 min)

**Hilo conductor:** Creamos Payments.API e implementamos la saga completa de creación de pedido: Orders → Products (reservar stock) → Payments (cobrar) → Notifications (confirmar).

**Temas EXCLUSIVOS:**

- El problema de las transacciones distribuidas: · No hay ACID entre servicios · Two-Phase Commit (2PC): qué es y por qué no escala · Las sagas como alternativa
- Choreography-based Sagas (implementación): · Cada servicio reacciona a eventos y publica los suyos · Flujo completo de OrderFlow con choreography · Compensaciones: StockReleased, PaymentRefunded · Ejemplo: implementar el flujo con MassTransit consumers
- Orchestration-based Sagas (implementación): · Orquestador central con state machine · MassTransit Sagas con Automatonymous · Definir estados, eventos, transiciones · Persistencia del estado de la saga (EF Core con SQL Server) · Ejemplo: OrderSaga state machine completa
- Choreography vs Orchestration: · Comparativa con ejemplos · Cuándo cada una (>4 pasos → orchestration)
- Crear Payments.API: · Esqueleto del servicio con **Minimal APIs** · Endpoints: POST /payments, GET /payments/{id} · Consumer de ProcessPayment command · Publicar PaymentProcessed o PaymentFailed · SQL Server con EF Core 10 (PaymentDbContext)
- Implementar la saga en OrderFlow: · Decidir: orchestration (porque tenemos 4 pasos) · OrderSaga state machine en Orders.API · Flujo feliz: crear → reservar stock → cobrar → notificar · Flujo con fallo: cobro rechazado → compensar stock → cancelar pedido · Testing de la saga: simular fallo en cada paso
- Gestión de errores en sagas: · Retry dentro de la saga · Compensación que falla (¿qué haces?) · Timeout de saga: máximo tiempo antes de abortar · Alertas para sagas atascadas

**NO se toca aquí:**

- ✗ Consistencia eventual a nivel de datos (→ M6.2)
- ✗ Outbox para garantizar publicación de eventos (→ M6.2)

---

## M4.4 — API Gateway con YARP (60 min)

**Hilo conductor:** Ponemos YARP como punto de entrada único delante de los 4 servicios de OrderFlow. El frontend ya no llama a los servicios directamente.

**Temas EXCLUSIVOS:**

- YARP (Yet Another Reverse Proxy): · Qué es: reverse proxy de Microsoft para .NET · Rendimiento y benchmarks · Diferencia entre reverse proxy y API Gateway
- Configuración de YARP: · Routes, Clusters, Destinations · Configuración desde appsettings.json · Configuración desde código (custom providers) · Hot reload sin reiniciar · Ejemplo: YARP configurado para los 4 servicios de OrderFlow
- Load balancing: · Algoritmos: Round Robin, Least Requests, Random, Power of Two Choices · Configuración por cluster · Health checks de backends
- Request/Response transformations: · Añadir headers (X-Correlation-Id, Authorization forwarding) · Path rewriting · Query string manipulation · Ejemplo: transformaciones para OrderFlow
- YARP como API Gateway: · Routing basado en path, host, headers · Rate limiting por ruta (middleware) · Authentication forwarding vs authentication en gateway · Aggregation de requests (con middleware custom)
- Session affinity: · Sticky sessions para servicios con estado · Cookie-based, header-based
- YARP vs Ocelot vs Azure API Management: · Comparativa: rendimiento, features, complejidad, coste · Cuándo elegir cada uno
- Crear el API Gateway de OrderFlow: · Proyecto Gateway.API con YARP · Rutas: /api/orders → Orders.API, /api/products → Products.API, etc. · Health check agregado del gateway · Rate limiting global · Añadir a Aspire como servicio

**NO se toca aquí:**

- ✗ Authentication/Authorization en el gateway (→ M5.1)
- ✗ mTLS (→ M5.2)
- ✗ WAF (→ M5.3)

---

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

# MÓDULO 5 — SEGURIDAD (3h)

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

## M5.1 — Autenticación y autorización con JWT y OAuth 2.0 (60 min)

**Hilo conductor:** Securizamos OrderFlow. Añadimos Keycloak como Identity Provider, JWT en todos los servicios, autorización por roles y policies.

**Temas EXCLUSIVOS:**

- OAuth 2.0 y OpenID Connect: · Authorization Code + PKCE para SPAs y mobile · Client Credentials para service-to-service · Refresh tokens: flujo y seguridad · El papel del Authorization Server
- JWT (JSON Web Tokens): · Estructura: header, payload, signature · Claims estándar y custom · Validación: firma, audience, issuer, expiración · Por qué no meter datos sensibles (codificado ≠ encriptado)
- Identity Providers: · Keycloak (open source) — lo usamos en OrderFlow · Duende IdentityServer, Azure AD / Entra ID, Auth0 · Configurar Keycloak en docker-compose / Aspire · Crear realm, clients, roles, usuarios
- ASP.NET Core authentication: · AddAuthentication().AddJwtBearer() · Configuración del middleware · Validar tokens en cada servicio · Ejemplo: configuración completa en Orders.API
- Autorización en .NET: · Policy-based authorization · Claims-based authorization · Role-based vs Permission-based (recomendación: permissions) · Requirement handlers custom · [Authorize] en Minimal APIs: RequireAuthorization() · Ejemplo: policies para OrderFlow (admin, customer, service)
- Token propagation entre servicios: · Forwarding del JWT en llamadas HTTP downstream · Token exchange para service-to-service · Scopes y audiences por servicio · Ejemplo: Orders propaga el token al llamar a Products
- Autenticación en YARP: · Authentication en el gateway vs en los servicios · ForwardAuthentication en YARP · Ejemplo: añadir auth al gateway de OrderFlow

**NO se toca aquí:**

- ✗ mTLS (→ M5.2)
- ✗ Secrets management (→ M5.2)
- ✗ Rate limiting como seguridad (→ M5.3)

---

## M5.2 — Seguridad en comunicación: HTTPS, mTLS y secretos (45 min)

**Hilo conductor:** Securizamos la comunicación entre servicios de OrderFlow y eliminamos secretos del código.

**Temas EXCLUSIVOS:**

- HTTPS: · Certificate management en .NET · Kestrel con certificados · Let's Encrypt y cert-manager en Kubernetes (concepto — K8s en M7.2) · HSTS headers
- mTLS (Mutual TLS): · Autenticación bidireccional: servidor verifica cliente y viceversa · Por qué mTLS entre microservicios: zero trust en la red interna · Service Mesh para mTLS automático (Istio, Linkerd) · Dapr con mTLS automático (concepto) · Ejemplo: configurar mTLS entre Orders y Products (simplificado)
- Criptografía post-cuántica en .NET 10: · ML-KEM (FIPS 203), ML-DSA (FIPS 204), SLH-DSA (FIPS 205) · "Harvest now, decrypt later": por qué importa en un LTS · Cuándo aplicarlo en microservicios
- Secrets management en profundidad: · El antipatrón: secretos en appsettings.json commiteado · Azure Key Vault:
    - Integración con .NET: AddAzureKeyVault()
    - Rotación de secretos sin reiniciar (IOptionsMonitor)
    - Ejemplo: connection strings de OrderFlow en Key Vault · AWS Secrets Manager (alternativa) · HashiCorp Vault (alternativa) · Kubernetes Secrets: por qué NO son secretos sin encryption at rest · Sealed Secrets para GitOps
- CORS en microservicios: · Configuración por servicio vs en el API Gateway · Recomendación: CORS en el gateway · Ejemplo: CORS en YARP para OrderFlow
- Security headers: · Content Security Policy, X-Content-Type-Options, X-Frame-Options · Middleware o NWebSec · Ejemplo: headers en el gateway

**NO se toca aquí:**

- ✗ WAF (→ M5.3)
- ✗ Rate limiting como medida de seguridad (→ M5.3)
- ✗ Input validation (→ M5.3)
- ✗ GDPR (→ M5.3)

---

## M5.3 — Seguridad avanzada: rate limiting, WAF y Zero Trust (45 min)

**Hilo conductor:** Añadimos las últimas capas de seguridad a OrderFlow. Al final de esta presentación, el sistema tiene seguridad defense-in-depth.

**Temas EXCLUSIVOS:**

- Rate limiting como medida de seguridad: · Proteger contra abuso y DDoS · Algoritmos: fixed window, sliding window, token bucket · Rate limiting por IP, por usuario, por API key · Configuración avanzada en .NET (más allá de lo visto en M3.2) · Ejemplo: rate limiting diferenciado por rol en OrderFlow
- Web Application Firewall (WAF): · Azure WAF, AWS WAF, Cloudflare · Reglas OWASP: SQL injection, XSS, CSRF · WAF delante del API Gateway: arquitectura · Ejemplo: configuración conceptual para OrderFlow
- Zero Trust Architecture: · Principios: no confiar, verificar todo, mínimo privilegio · Cada request se autentica incluso en red interna · Network policies en Kubernetes: segmentación de red · Ejemplo: network policies para OrderFlow
- OWASP Top 10 en microservicios: · Cómo se manifiestan en arquitecturas distribuidas · Prevención específica para cada punto
- Input validation y sanitización: · Validar en cada servicio, no solo en el gateway · FluentValidation como primera barrera (reforzar lo de M3.2) · Sanitización de HTML con HtmlSanitizer · Validación de payloads de eventos (no solo HTTP)
- Auditoría y compliance: · Audit logging: quién hizo qué, cuándo, desde dónde · Implementación con middleware y events · Ejemplo: audit trail en Orders.API
- GDPR en microservicios: · Derecho al olvido con datos distribuidos · Estrategias: centralizar PII, event store con eliminación, anonymización · El dolor real: encontrar datos de un usuario en 10 servicios

**NO se toca aquí:**

- ✗ Nada nuevo de autenticación (→ ya en M5.1)
- ✗ Nada nuevo de certificados/mTLS (→ ya en M5.2)

---

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

# MÓDULO 6 — GESTIÓN DE DATOS (4h)

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

## M6.1 — Database per Service con SQL Server y EF Core 10 (60 min)

**Hilo conductor:** Configuramos la persistencia definitiva de cada servicio de OrderFlow. Todos usan SQL Server pero con bases de datos aisladas. Profundizamos en EF Core 10.

**Temas EXCLUSIVOS:**

- Database per Service con SQL Server: · Cada servicio con su propia base de datos (o esquema aislado) · Estrategias de aislamiento en SQL Server:
    - Bases de datos separadas (recomendado): OrdersDb, ProductsDb, PaymentsDb, NotificationsDb
    - Esquemas separados dentro de la misma base de datos (alternativa ligera) · Naming conventions para bases de datos de microservicios · Ejemplo: configuración de 4 DbContexts apuntando a 4 bases de datos en LocalDB
- Entorno de desarrollo con LocalDB: · Qué es LocalDB y por qué es ideal para desarrollo · Connection strings para LocalDB · Gestión de instancias LocalDB · Limitaciones vs SQL Server completo
- Ruta a producción con Azure SQL Database: · Azure SQL Database vs SQL Server en VM vs Azure SQL Managed Instance · Pricing tiers y cuándo usar cada uno · Elastic pools para microservicios (una pool, varias databases) · Configuración de connection strings por entorno · Seguridad: Azure AD authentication, managed identity
- Entity Framework Core 10 en profundidad: · DbContext por servicio: configuración, conventions, interceptors · Novedades EF Core 10: vector search, JSON nativo, named query filters, full-text search · Connection resilience con SQL Server: EnableRetryOnFailure · Interceptors: auditoría automática (CreatedAt, ModifiedAt, CreatedBy) · Soft delete con global query filters · Temporal tables en SQL Server para historial automático · Ejemplo: OrderDbContext completo con interceptors y conventions
- Migrations en equipo: · Crear, aplicar, gestionar migrations en desarrollo · Migration bundles para CI/CD (→ M6.4 profundiza) · Resolver conflictos de migrations entre developers · Ejemplo: migrations de Orders.API
- Dapper como complemento (no reemplazo): · Cuándo EF Core, cuándo Dapper (EF Core para writes, Dapper para reads complejas) · Dapper para read models rápidos y queries de reporting · Convivencia EF Core + Dapper en el mismo servicio · Ejemplo: query de listado de pedidos con Dapper
- Caching con IMemoryCache y IDistributedCache: · IMemoryCache para caché in-process (simple, rápido) · SQL Server como distributed cache (sí, SQL Server puede ser cache distribuida) · Estrategias: cache-aside pattern · Invalidación de caché: el problema más difícil · Ejemplo: caché de catálogo de productos en Orders.API
- Polyglot persistence como concepto (mención sin implementar): · Qué es: cada servicio elige su BD ideal · Cuándo tiene sentido: MongoDB para documentos, Redis para caché, Elasticsearch para búsqueda · Por qué en este curso usamos SQL Server para todo: simplicidad, entorno enterprise, Azure ecosystem · Si el alumno lo necesita en su proyecto, el patrón Database per Service lo permite

**NO se toca aquí:**

- ✗ Consistencia eventual (→ M6.2)
- ✗ Outbox pattern (→ M6.2)
- ✗ CQRS con BD separadas (→ M6.3)
- ✗ Migrations en CI/CD (→ M6.4)
- ✗ Azure SQL Database setup real (→ M7.2, al desplegar)

---

## M6.2 — Consistencia eventual y el patrón Outbox (60 min)

**Hilo conductor:** Resolvemos el problema de la doble escritura en Orders.API. Garantizamos que cada pedido creado genera su evento de forma fiable.

**Temas EXCLUSIVOS:**

- Teorema CAP: · Consistency, Availability, Partition tolerance · En la práctica: P siempre ocurre, eliges entre C y A · Microservicios: AP con consistencia eventual
- Consistencia eventual (profundidad): · No es "datos incorrectos" — es "datos correctos eventualmente" · Ejemplo: Amazon confirma el pedido, el cargo llega minutos después · Diseñar UX para consistencia eventual
- El problema de la doble escritura: · Guardar en DB + publicar evento: si uno falla, inconsistencia · Ejemplo: Order guardado pero evento no publicado → Notifications nunca se entera · Las soluciones incorrectas: try/catch, transacciones distribuidas
- Transactional Outbox Pattern (implementación completa): · Guardar el evento en tabla "outbox" en la misma transacción · Outbox Publisher: proceso que lee la tabla y publica al broker · Garantiza at-least-once delivery · Implementación con MassTransit + EF Core: configuración · Ejemplo completo en Orders.API
- Inbox Pattern: · El complemento del outbox en el consumidor · Deduplicación automática por MessageId · Implementación con MassTransit
- Event-carried state transfer: · El evento trae los datos que el consumidor necesita · OrderCreated trae customerName, no solo customerId · Trade-off: eventos más grandes vs menos llamadas entre servicios · Ejemplo: enriquecer eventos de OrderFlow
- Idempotencia en consumidores: · El consumidor puede recibir el mismo mensaje dos veces · Strategies: check-then-act, idempotent operations, deduplication table

**NO se toca aquí:**

- ✗ CQRS con BD separadas (→ M6.3)
- ✗ Event Sourcing como store (→ M6.3)

---

## M6.3 — CQRS avanzado y proyecciones (60 min)

**Hilo conductor:** Separamos el read model de Orders.API en una base de datos optimizada. Implementamos proyecciones que se actualizan por eventos.

**Temas EXCLUSIVOS:**

- CQRS con bases de datos separadas: · Write DB: SQL Server normalizado (EF Core) · Read DB: SQL Server desnormalizado con vistas materializadas o tablas de proyección · Alternativa: segunda base de datos SQL Server optimizada para lectura · Sincronización vía domain events · Cuándo merece la pena: high read/write ratio · Ejemplo: Orders write model vs read model en SQL Server
- Proyecciones: · Construir read models desde eventos · Proyecciones síncronas (mismo proceso) vs asíncronas (vía mensajería) · Rebuilding: reconstruir el read model reprocesando eventos · Ejemplo: OrderSummaryProjection que mantiene una vista desnormalizada
- Event Sourcing (implementación): · Event store como fuente de verdad · Append-only log de eventos · Reconstruir estado reproduciendo eventos · Snapshots para rendimiento
- Event Store con SQL Server: · Tabla de eventos como event store (diseño de tabla, índices) · Append-only pattern en SQL Server · Temporal Tables como historial automático (feature nativa de SQL Server) · Ejemplo: EventStore table en Orders con EF Core
- Marten y EventStoreDB (alternativas — mención sin implementar): · Marten: PostgreSQL como event store + document DB · EventStoreDB: event store dedicado con gRPC API · Cuándo considerar una alternativa a SQL Server para event sourcing
- Queries complejas en el read model: · Búsqueda, filtrado, paginación, ordenación · Endpoints de query optimizados
- GraphQL como alternativa (introducción): · HotChocolate para .NET · Cuándo GraphQL aporta valor en microservicios · Solo concepto — no implementamos
- Implementar CQRS completo en Orders: · Write side: CreateOrder command → EF Core → SQL Server · Event: OrderCreatedDomainEvent → proyección · Read side: GetOrders query → Dapper → vista/tabla desnormalizada en SQL Server · Verificar que write y read están sincronizados

**NO se toca aquí:**

- ✗ Nada de outbox (→ ya en M6.2)
- ✗ Nada de consistencia eventual (→ ya en M6.2)
- ✗ Migrations (→ M6.4)

---

## M6.4 — Data migration y refactoring de datos (60 min)

**Hilo conductor:** Simulamos la migración de la base de datos del monolito de TechShop a los esquemas de los 4 microservicios. El último paso de la migración narrativa.

**Temas EXCLUSIVOS:**

- Schema migrations en microservicios: · EF Core migrations en CI/CD: migration bundles · Quién ejecuta las migrations: init container, job, bundle · Flyway/Liquibase como alternativas agnósticas · Ejemplo: pipeline con migration bundle para Orders
- Blue-green database migrations: · Aplicar migration antes del deploy · La nueva versión de código funciona con el esquema nuevo y el viejo · Zero-downtime schema changes
- Expand and Contract pattern: · Paso 1 — Expand: añadir columna nueva sin eliminar la vieja · Paso 2 — Migrate: copiar datos, dual-write · Paso 3 — Contract: eliminar la vieja · Ejemplo: renombrar columna en Orders sin downtime
- Data partitioning entre servicios: · El monolito de TechShop tiene una BD con todas las tablas · Estrategias para dividir:
    - Duplicar datos con sync
    - Referencia por ID con API calls
    - Event-carried state transfer · Ejemplo: extraer tabla Products del monolito al nuevo servicio
- Datos compartidos — alternativas: · Shared kernel (DDD): mínimos datos comunes · Reference data service: catálogos compartidos como servicio · Data replication vía eventos
- Data consistency patterns: · Read-your-writes: dar al usuario ilusión de consistencia inmediata · Causal consistency: garantizar orden de operaciones relacionadas · Ejemplo: usuario crea pedido y ve su pedido inmediatamente
- Ejercicio: migrar esquema de TechShop · Base de datos monolítica simulada · Extraer Orders, Products, Payments a esquemas separados · Verificar que la migración no pierde datos · Ejecutar tests de integración contra los nuevos esquemas

**NO se toca aquí:**

- ✗ Nada de EF Core (→ ya en M6.1)
- ✗ Nada de outbox/inbox (→ ya en M6.2)
- ✗ Nada de proyecciones (→ ya en M6.3)

---

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

# MÓDULO 7 — IMPLEMENTACIÓN Y DESPLIEGUE (3.5h)

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

## M7.1 — .NET Aspire y contenedores como concepto (45 min)

**Hilo conductor:** Profundizamos en .NET Aspire como orquestador de desarrollo que ya hemos usado durante todo el curso. Además, explicamos Docker y Kubernetes como conceptos teóricos para que el alumno los conozca aunque no los usemos en el lab.

**Temas EXCLUSIVOS:**

- .NET Aspire en profundidad: · AppHost: el proyecto que orquesta todos los servicios · ServiceDefaults: configuración compartida (OpenTelemetry, health checks, resiliencia) · Recursos: SQL Server, RabbitMQ, Redis — Aspire los levanta automáticamente · Dashboard: logs, trazas, métricas en un solo panel · Cómo Aspire resuelve service discovery (inyección automática de connection strings) · Cómo Aspire resuelve configuración por entorno · Ejemplo: AppHost completo de OrderFlow con los 4 servicios + SQL Server + RabbitMQ
- Aspire vs Docker Compose: · Tabla comparativa: qué hace cada uno · Aspire: experiencia .NET nativa, F5 y funciona, integrado con Visual Studio · Docker Compose: agnóstico de lenguaje, más flexible, más manual · Por qué Aspire para cursos y desarrollo .NET, Docker para producción heterogénea
- Docker como concepto teórico (NO práctico): · Qué es un contenedor: aislamiento, portabilidad, reproducibilidad · Dockerfile: concepto de multi-stage build · Imágenes: layers, registries, tags · docker-compose: composición de servicios · Por qué la industria usa contenedores: "funciona en mi máquina" resuelto · Relación Docker ↔ Aspire: Aspire puede generar Dockerfiles y manifests
- Kubernetes como concepto teórico (NO práctico): · Qué problema resuelve: orquestación de contenedores a escala · Pods, Deployments, Services, Ingress — los 4 recursos básicos · Scaling automático (HPA, KEDA) · Service discovery nativo (DNS) · Self-healing: restart automático · Cuándo tiene sentido Kubernetes vs App Services:
    - K8s: muchos servicios, equipo DevOps maduro, multi-cloud
    - App Services: menos ops, equipo centrado en desarrollo, ecosistema Azure
- NativeAOT y optimización (concepto): · Compilación ahead-of-time para arranque rápido · Imágenes de contenedor mínimas (<50MB) · Cuándo aplicarlo en microservicios · Trimming para reducir tamaño
- La decisión de despliegue para OrderFlow: · Desarrollo local: .NET Aspire (lo que usamos) · Producción: Azure App Services (lo que vamos a configurar en M7.2) · Futuro: si el proyecto crece, la migración a contenedores/K8s está preparada

**NO se toca aquí:**

- ✗ Labs con Docker (solo conceptos)
- ✗ Labs con Kubernetes (solo conceptos)
- ✗ Azure App Services (→ M7.2)
- ✗ CI/CD (→ M7.3)

---

## M7.2 — Despliegue en Azure: App Services, SQL y Service Bus (60 min)

**Hilo conductor:** Desplegamos OrderFlow completo en Azure. Los 4 servicios en App Services, Azure SQL Database, Azure Service Bus. Sin contenedores, sin Kubernetes.

**Temas EXCLUSIVOS:**

- Arquitectura de despliegue en Azure: · Diagrama: App Services + Azure SQL + Azure Service Bus + API Management/YARP · Resource Group para OrderFlow · Naming conventions en Azure · Estimación de costes (tier más económico viable)
- Azure App Services: · Qué es: PaaS para aplicaciones web sin gestionar infraestructura · Crear un App Service para cada microservicio · Deployment slots: staging y production · Configuración: App Settings y Connection Strings por entorno · Scaling: manual y auto-scale rules · Health checks en App Services (integración con los health checks de .NET) · Ejemplo: desplegar Orders.API en App Service desde Visual Studio
- Azure SQL Database: · Crear bases de datos: OrdersDb, ProductsDb, PaymentsDb, NotificationsDb · Elastic pools: compartir recursos entre las 4 bases de datos (ahorro de coste) · Firewall rules y Private Endpoints · Connection strings con Azure AD authentication (managed identity) · Migrations: ejecutar migration bundles contra Azure SQL · Ejemplo: configurar Azure SQL para OrderFlow
- Azure Service Bus: · Crear namespace y topics/queues · Configurar MassTransit para Azure Service Bus (cambiar `.UsingRabbitMq()` → `.UsingAzureServiceBus()`) · Connection string vs Managed Identity · Dead letter queues en Service Bus · Ejemplo: OrderFlow publicando/consumiendo eventos en Azure Service Bus
- API Gateway en Azure: · Opción A: YARP desplegado como App Service (lo que usamos) · Opción B: Azure API Management (para APIs públicas, más caro) · Opción C: Azure Front Door (para distribución global) · Ejemplo: YARP como App Service gateway para OrderFlow
- Managed Identity: · Qué es: identidad gestionada por Azure, sin secretos en configuración · App Service → Azure SQL sin connection string con password · App Service → Azure Service Bus sin connection string con key · App Service → Key Vault sin secretos en appsettings · Ejemplo: configurar managed identity para Orders.API
- Networking: · VNet integration para App Services · Private Endpoints para SQL y Service Bus · Seguridad de red sin Kubernetes network policies

**NO se toca aquí:**

- ✗ CI/CD (→ M7.3, aquí es deploy manual para entender el proceso)
- ✗ Observabilidad en Azure (→ M7.4)
- ✗ Kubernetes (→ conceptos en M7.1)
- ✗ Contenedores (→ conceptos en M7.1)

---

## M7.3 — CI/CD para microservicios (60 min)

**Hilo conductor:** Automatizamos el despliegue de OrderFlow. Del commit al App Service de forma automática con GitHub Actions.

**Temas EXCLUSIVOS:**

- Estrategias de repositorio: · Monorepo vs Multi-repo: pros, contras, recomendación · Monorepo con selective builds (trigger por carpeta) · Estructura del repo de OrderFlow (monorepo con carpetas por servicio)
- Pipeline de CI con GitHub Actions: · Trigger: push/PR por carpeta de servicio · Steps: restore → build → test → coverage → publish · Matrix builds: varios servicios en paralelo · Caching de NuGet packages · Contract tests como step obligatorio · Ejemplo: workflow completo para Orders.API
- Pipeline de CD hacia Azure App Services: · GitHub Actions + Azure Web App Deploy action · Publish profile vs Service Principal vs OIDC (federated credentials) · Deploy a staging slot → smoke test → swap a production · Ejemplo: workflow completo CI+CD para Orders.API
- Estrategias de despliegue: · **Deployment slots (App Services):** staging + production + swap · **Blue-Green con slots:** desplegar en staging, verificar, swap · **Canary con Traffic Routing:** enviar % del tráfico al slot de staging · Feature flags: LaunchDarkly, Unleash, flags en App Configuration · Ejemplo: canary deployment con traffic routing en App Service
- Database migrations en el pipeline: · Migration bundle como step del pipeline · Ejecutar contra Azure SQL Database · Rollback de migrations · Ejemplo: migration step en el workflow de Orders.API
- Smoke tests post-deploy: · Verificar health checks del App Service · Tests HTTP automatizados contra el servicio desplegado · Rollback automático (swap back) si falla
- GitOps como concepto (mención): · ArgoCD, Flux — para entornos con Kubernetes · Por qué no aplica directamente a App Services · Azure App Configuration como alternativa para gestión de config
- Entornos: · dev (local, Aspire) → staging (App Service slot) → production (App Service) · Environment-specific configuration con App Settings · Azure App Configuration para configuración centralizada

**NO se toca aquí:**

- ✗ Observabilidad de los deploys (→ M7.4)
- ✗ ArgoCD/Flux en profundidad (solo mención — son para K8s)

---

## M7.4 — Observabilidad en producción y operaciones (45 min)

**Hilo conductor:** Configuramos Application Insights y Azure Monitor para OrderFlow en producción. Dashboards, alertas, diagnóstico de problemas reales.

**Temas EXCLUSIVOS:**

- Observabilidad en Azure — el stack: · Application Insights: trazas, métricas, logs, exceptions · Azure Monitor: alertas, dashboards, Log Analytics · Relación con OpenTelemetry (configurado en M2.3):
    - En local: OpenTelemetry → dashboard de Aspire
    - En producción: OpenTelemetry → Application Insights (Azure Monitor Exporter)
    - Mismo código, diferente exportador
- Application Insights en .NET 10: · Configuración: AddOpenTelemetry() con Azure Monitor Exporter · Auto-instrumentación: HTTP, EF Core, MassTransit · Custom telemetry: métricas de negocio · Live Metrics: ver peticiones en tiempo real · Ejemplo: configurar Application Insights para Orders.API
- Distributed tracing en producción: · Application Map: visualizar las dependencias entre los 4 servicios · Transaction search: trazar un pedido de principio a fin · End-to-end transaction details: ver cada servicio en el timeline · Performance: identificar bottlenecks por servicio · Failures: agrupar errores, diagnosticar causas · Ejemplo: diagnosticar un pedido que falló en el paso de pago
- Dashboards operacionales: · Azure Monitor Workbooks: dashboards custom · Métricas clave por servicio: latencia P50/P95/P99, error rate, throughput · RED method (Rate, Errors, Duration) · Dashboard de OrderFlow: los 4 servicios · Ejemplo: crear un workbook para OrderFlow
- Alerting: · SLOs (Service Level Objectives): definir qué es "OK" · Error budget: cuánto margen antes de impactar al usuario · Metric alerts: latencia > 500ms, error rate > 5% · Log alerts: excepciones específicas · Action Groups: email, SMS, webhook · Alert fatigue: cómo evitarla · Ejemplo: alertas para Orders.API
- Log Analytics: · Kusto Query Language (KQL) para consultar logs · Queries útiles: errores por servicio, latencia por endpoint, usuarios afectados · Ejemplo: queries KQL para diagnosticar problemas en OrderFlow
- Grafana + Prometheus como alternativa (mención): · Stack open source: Grafana + Loki + Tempo + Prometheus · Cuándo elegirlo sobre Azure Monitor: multi-cloud, on-premise, control total · Se integra con OpenTelemetry de la misma forma
- Incident management: · Runbooks: qué hacer cuando X falla · Post-mortems blameless: cultura de aprender de los fallos · Template de post-mortem para OrderFlow
- Chaos engineering (concepto): · Azure Chaos Studio · Romper cosas a propósito para verificar resiliencia · Experimentos: reiniciar un App Service, añadir latencia, cortar acceso a SQL · Concepto — no lab práctico
- Cierre del curso: · OrderFlow completo: del monolito de TechShop al sistema distribuido en Azure · Recap visual: la evolución a lo largo de las 28 presentaciones · Diagrama final: Aspire (local) → Azure (producción) · Próximos pasos: Docker/K8s cuando el proyecto crezca, AKS como siguiente nivel · Recursos para seguir aprendiendo

---

# MATRIZ DE CONCEPTOS — DÓNDE SE EXPLICA CADA TEMA

|Concepto|Se DEFINE en|Se USA en (sin re-explicar)|
|---|---|---|
|Microservicio (definición)|M1.1|Todo el curso|
|Ley de Conway|M1.1|—|
|8 falacias|M1.1|M3.5, M4.1|
|Monolito modular|M1.2|M6.4|
|Strangler Fig|M1.2|M6.4|
|DDD / Bounded Context|M1.3 (concepto)|M3.1 (código)|
|Event Storming|M1.3|—|
|API Gateway (concepto)|M1.3|M4.4 (implementación)|
|Circuit Breaker (concepto)|M1.3|M3.5 (implementación)|
|Saga (concepto)|M1.3|M4.3 (implementación)|
|CQRS (concepto)|M1.3|M3.4 (básico), M6.3 (avanzado)|
|.NET 10 / Aspire|M2.1|Todo|
|DI / Options|M2.2|Todo|
|Health Checks|M2.2|M7.2 (K8s probes)|
|Serilog / Logging|M2.3|Todo|
|OpenTelemetry|M2.3|M7.4 (producción)|
|Métricas|M2.3|M7.4 (dashboards)|
|xUnit / NSubstitute|M2.4|Todo|
|WebApplicationFactory|M2.4|Todo|
|Contract Testing / Pact|M2.4|M7.3 (en pipeline)|
|DDD Táctico (código)|M3.1|—|
|Clean Architecture / Vertical Slice|M3.1|—|
|Controllers MVC|M3.2|— (se migra a Minimal APIs en M3.3)|
|Action Filters|M3.2|M3.3 (contraste con Endpoint Filters)|
|DataAnnotations|M3.2|M3.3 (contraste con FluentValidation)|
|Minimal APIs (profundidad)|M3.3|Todo|
|FluentValidation|M3.3|M5.3 (refuerzo)|
|Endpoint Filters|M3.3|Todo|
|OpenAPI 3.1|M3.3|M4.1 (generar clientes)|
|Problem Details|M3.3|Todo|
|MediatR|M3.4|Todo|
|Domain Events|M3.4|M6.2 (outbox)|
|Polly v8|M3.5|M4.1 (HttpClient)|
|Idempotencia|M3.5|M6.2 (refuerzo)|
|HttpClient / gRPC|M4.1|—|
|Refit|M4.1|—|
|RabbitMQ|M4.2|—|
|MassTransit|M4.2|M4.3 (saga), M6.2 (outbox)|
|Integration Events|M4.2|M6.2|
|Saga (implementación)|M4.3|—|
|YARP|M4.4|M5.1 (auth), M7.2 (K8s)|
|JWT / OAuth 2.0|M5.1|—|
|Keycloak|M5.1|—|
|mTLS|M5.2|—|
|Key Vault / Secrets|M5.2|—|
|CORS|M5.2|—|
|Zero Trust|M5.3|—|
|GDPR|M5.3|—|
|EF Core 10 (profundidad)|M6.1|—|
|SQL Server / LocalDB|M6.1|M7.2 (Azure SQL)|
|Dapper (complemento)|M6.1|—|
|Caching (IMemoryCache)|M6.1|—|
|Outbox / Inbox|M6.2|—|
|CAP Theorem|M6.2|—|
|Event Sourcing (impl)|M6.3|—|
|SQL Server Event Store|M6.3|—|
|Expand & Contract|M6.4|—|
|.NET Aspire (profundidad)|M7.1|M2.1 (intro)|
|Docker (concepto teórico)|M7.1|—|
|Kubernetes (concepto teórico)|M7.1|—|
|Azure App Services|M7.2|—|
|Azure SQL Database|M7.2|M6.1 (concepto)|
|Azure Service Bus|M7.2|M4.2 (concepto)|
|Managed Identity|M7.2|—|
|GitHub Actions|M7.3|—|
|Deployment Slots|M7.3|—|
|Application Insights|M7.4|—|
|Azure Monitor / KQL|M7.4|—|
|Chaos Engineering (concepto)|M7.4|—|