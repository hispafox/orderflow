# Configuración Gamma — 28 Presentaciones

## Microservicios con .NET 10

Campos a rellenar en Gamma antes de generar cada presentación.

---

## M1.1 — ¿Qué son los Microservicios y por qué existen?

**Escribe para:** Desarrolladores y arquitectos .NET con experiencia en ASP.NET Core que quieren aprender arquitecturas de microservicios desde cero

**Tono:** Claro, didáctico y profesional, en español de España, cercano pero técnico

**Instrucciones adicionales:** Presentación introductoria de 45 minutos. Usar el caso de TechShop (empresa ficticia) como hilo conductor. Incluir diagramas conceptuales donde sea posible. Cada sección ## es una diapositiva. No añadir contenido que no esté en el texto proporcionado.

---

## M1.2 — Monolito vs. Microservicios: la comparativa honesta

**Escribe para:** Desarrolladores .NET que ya conocen qué son los microservicios y quieren entender cuándo usarlos frente al monolito

**Tono:** Analítico y directo, en español de España, con comparativas visuales claras

**Instrucciones adicionales:** Presentación de 45 minutos. Continúa la historia de TechShop iniciada en M1.1. Incluir tablas comparativas y diagramas de arquitectura. Muchas diapositivas con comparativas lado a lado (monolito vs microservicios). Cada sección ## es una diapositiva.

---

## M1.3 — Patrones fundamentales de arquitectura

**Escribe para:** Desarrolladores y arquitectos .NET que ya entienden qué son los microservicios y quieren aprender a diseñar un sistema distribuido

**Tono:** Técnico y estructurado, en español de España, con énfasis en el diseño práctico

**Instrucciones adicionales:** Presentación de 60 minutos. Es la sesión de diseño de OrderFlow: el proyecto que se construirá durante todo el curso. Incluir diagramas de Event Storming, diagramas de arquitectura y mapas de patrones. El resultado de esta sesión es el diseño completo de los 4 microservicios. Cada sección ## es una diapositiva.

---

## M2.1 — El ecosistema .NET 10 para microservicios

**Escribe para:** Desarrolladores .NET que conocen ASP.NET Core y quieren ver las novedades de .NET 10 aplicadas a microservicios

**Tono:** Técnico y práctico, en español de España, con énfasis en código y herramientas concretas

**Instrucciones adicionales:** Presentación de 45 minutos. Primera sesión con código real: se crea el esqueleto de Orders.API. Incluir bloques de código C# y capturas de estructura de proyecto. Presentar .NET Aspire como orquestador local. Cada sección ## es una diapositiva.

---

## M2.2 — Inyección de dependencias y configuración avanzada

**Escribe para:** Desarrolladores .NET que usan DI básica y quieren profundizar en Scoped/Transient/Singleton, Options pattern y health checks

**Tono:** Técnico y detallado, en español de España, con ejemplos de código aplicados a Orders.API

**Instrucciones adicionales:** Presentación de 45 minutos. Continúa sobre el esqueleto de Orders.API creado en M2.1. Incluir bloques de código C# para cada concepto: Keyed Services, IOptions, validación de configuración, health checks custom. Cada sección ## es una diapositiva.

---

## M2.3 — Logging, observabilidad y diagnóstico

**Escribe para:** Desarrolladores .NET que quieren implementar observabilidad completa: structured logging, distributed tracing y métricas

**Tono:** Técnico y práctico, en español de España, con ejemplos de configuración reales

**Instrucciones adicionales:** Presentación de 45 minutos. Al terminar, Orders.API tiene Serilog y OpenTelemetry configurados. Incluir bloques de código de configuración en Program.cs y ejemplos de logs estructurados. Mostrar el dashboard de Aspire. Cada sección ## es una diapositiva.

---

## M2.4 — Testing en microservicios .NET

**Escribe para:** Desarrolladores .NET que conocen testing básico con xUnit y quieren aprender testing de integración con LocalDB y contract testing con Pact

**Tono:** Técnico y práctico, en español de España, con ejemplos de tests reales para Orders.API

**Instrucciones adicionales:** Presentación de 45 minutos. Incluir bloques de código de tests unitarios, tests de integración con WebApplicationFactory, y contract tests con PactNet. Mostrar la pirámide de testing para microservicios. Cada sección ## es una diapositiva.

---

## M3.1 — Diseño de un microservicio: del dominio al código

**Escribe para:** Desarrolladores .NET que quieren implementar DDD táctico: entidades, value objects, aggregates y Clean Architecture en C# 14

**Tono:** Técnico y estructurado, en español de España, con código C# 14 real y diagramas de arquitectura

**Instrucciones adicionales:** Presentación de 60 minutos. Implementamos el modelo de dominio de Orders.API usando C# 14 (records, init properties). Incluir bloques de código completos para Order (Aggregate Root), OrderLine (Entity), Money y Address (Value Objects). Diagrama de Clean Architecture. Cada sección ## es una diapositiva.

---

## M3.2 — APIs con Controllers: el enfoque clásico de MVC

**Escribe para:** Desarrolladores .NET que quieren entender en profundidad el patrón MVC con Controllers: routing, model binding, filters y ActionResult

**Tono:** Técnico y didáctico, en español de España, con código C# real y comparativas claras

**Instrucciones adicionales:** Presentación de 45 minutos. Construimos los endpoints de Orders.API con Controllers. Incluir bloques de código del OrdersController completo, ejemplos de filters, model binding y ActionResult. Al final de la sesión, la API funciona con Controllers (se migrará a Minimal APIs en M3.3). Cada sección ## es una diapositiva.

---

## M3.3 — APIs con Minimal APIs y OpenAPI

**Escribe para:** Desarrolladores .NET que ya conocen Controllers y quieren aprender Minimal APIs, FluentValidation, versionado y OpenAPI 3.1

**Tono:** Técnico y comparativo, en español de España, mostrando el contraste directo con Controllers

**Instrucciones adicionales:** Presentación de 60 minutos. Contrastar siempre Minimal APIs vs Controllers del M3.2. Products, Payments y Notifications usarán Minimal APIs en el proyecto. Incluir bloques de código con route groups, endpoint filters, FluentValidation y configuración de OpenAPI 3.1. Cada sección ## es una diapositiva.

---

## M3.4 — Patrones de implementación: CQRS y MediatR

**Escribe para:** Desarrolladores .NET que quieren implementar CQRS con MediatR: commands, queries, handlers y pipeline behaviors

**Tono:** Técnico y estructurado, en español de España, con código C# completo para Orders.API

**Instrucciones adicionales:** Presentación de 60 minutos. Refactorizamos Orders.API para usar CQRS con MediatR. Incluir bloques de código para CreateOrderCommand, CreateOrderHandler, pipeline behaviors (validación, logging, transacciones). Diagrama del flujo Endpoint → MediatR → Handler. Cada sección ## es una diapositiva.

---

## M3.5 — Resiliencia y manejo de errores con Polly

**Escribe para:** Desarrolladores .NET que quieren implementar resiliencia con Polly v8: retry, circuit breaker, timeout, bulkhead y fallback

**Tono:** Técnico y práctico, en español de España, con diagramas de estados y código real

**Instrucciones adicionales:** Presentación de 60 minutos. Añadimos resiliencia a Orders.API para las llamadas a Products y Payments. Incluir diagramas del Circuit Breaker (estados), gráficas de retry con backoff, y bloques de código de ResiliencePipelineBuilder. Cada sección ## es una diapositiva.

---

## M3.6 — Práctica guiada: Orders.API completo

**Escribe para:** Desarrolladores .NET que quieren consolidar todo lo aprendido en los módulos 2 y 3 construyendo Orders.API completo

**Tono:** Práctico y paso a paso, en español de España, con checkpoints claros y código verificable

**Instrucciones adicionales:** Presentación de 60 minutos. Es un laboratorio guiado: el alumno construye Orders.API completo con dominio, Controllers, CQRS, EF Core con LocalDB, Serilog, health checks y Polly. Incluir un checklist de entregables, pasos con checkpoints, y ejemplos de lo que debe verse al terminar. Cada sección ## es una diapositiva.

---

## M4.1 — Comunicación síncrona: REST y gRPC

**Escribe para:** Desarrolladores .NET que quieren implementar comunicación entre microservicios: HttpClientFactory, Refit, gRPC y generación de clientes tipados

**Tono:** Técnico y comparativo, en español de España, con código real de cliente HTTP y gRPC

**Instrucciones adicionales:** Presentación de 60 minutos. Creamos Products.API (Minimal APIs + SQL Server) y establecemos la primera comunicación entre servicios: Orders → Products. Incluir código de IHttpClientFactory, typed clients con Refit, definición de .proto para gRPC, y comparativa REST vs gRPC. Cada sección ## es una diapositiva.

---

## M4.2 — Comunicación asíncrona: mensajería y eventos

**Escribe para:** Desarrolladores .NET que quieren implementar mensajería con RabbitMQ y MassTransit: producers, consumers, exchanges y dead letter queues

**Tono:** Técnico y estructurado, en español de España, con diagramas de flujo de mensajes y código real

**Instrucciones adicionales:** Presentación de 60 minutos. Añadimos RabbitMQ (local vía instalación nativa) y MassTransit al proyecto. Creamos Notifications.API. Incluir diagramas de exchanges/queues de RabbitMQ, código de consumers MassTransit, y la diferencia entre Domain Events e Integration Events. Mencionar Azure Service Bus como alternativa de producción. Cada sección ## es una diapositiva.

---

## M4.3 — Saga Pattern: transacciones distribuidas

**Escribe para:** Desarrolladores .NET que quieren implementar el patrón Saga con MassTransit para coordinar transacciones entre microservicios

**Tono:** Técnico y detallado, en español de España, con diagramas de estado y código de state machine

**Instrucciones adicionales:** Presentación de 60 minutos. Creamos Payments.API y la saga completa: Orders → Products (reservar stock) → Payments (cobrar) → confirmación y notificación. Incluir diagramas de estados de la saga, código de OrderSaga con Automatonymous, y flujo de compensación cuando el pago falla. Cada sección ## es una diapositiva.

---

## M4.4 — API Gateway con YARP

**Escribe para:** Desarrolladores .NET que quieren implementar un API Gateway con YARP: routing, load balancing, transformaciones y rate limiting

**Tono:** Técnico y práctico, en español de España, con configuración YAML/JSON y código de middleware

**Instrucciones adicionales:** Presentación de 60 minutos. Ponemos YARP delante de los 4 servicios de OrderFlow. Incluir configuración completa de routes y clusters, ejemplos de transformaciones de headers, comparativa YARP vs Ocelot vs Azure APIM. Al terminar, todo el tráfico de OrderFlow pasa por YARP. Cada sección ## es una diapositiva.

---

## M5.1 — Autenticación y autorización con JWT y OAuth 2.0

**Escribe para:** Desarrolladores .NET que quieren implementar seguridad en microservicios: JWT, OAuth 2.0, Keycloak y policy-based authorization

**Tono:** Técnico y riguroso, en español de España, con diagramas de flujo OAuth y código de configuración

**Instrucciones adicionales:** Presentación de 60 minutos. Securizamos OrderFlow con Keycloak como Identity Provider. Incluir diagramas de flujo Authorization Code + PKCE y Client Credentials, código de AddJwtBearer en ASP.NET Core, ejemplos de policies y propagación de tokens entre servicios. Cada sección ## es una diapositiva.

---

## M5.2 — Seguridad en comunicación: HTTPS, mTLS y secretos

**Escribe para:** Desarrolladores .NET que quieren asegurar la comunicación entre microservicios y gestionar secretos correctamente

**Tono:** Técnico y concienzudo, en español de España, con énfasis en buenas prácticas de seguridad

**Instrucciones adicionales:** Presentación de 45 minutos. Implementamos mTLS conceptualmente, Key Vault para secretos con Managed Identity, y CORS en YARP. Incluir la sección de criptografía post-cuántica de .NET 10 (ML-KEM, ML-DSA). Código de AddAzureKeyVault y configuración de Managed Identity. Cada sección ## es una diapositiva.

---

## M5.3 — Seguridad avanzada: rate limiting, WAF y Zero Trust

**Escribe para:** Desarrolladores y arquitectos .NET que quieren completar la seguridad del sistema con rate limiting avanzado, WAF y modelo Zero Trust

**Tono:** Técnico y estratégico, en español de España, combinando código y decisiones arquitectónicas

**Instrucciones adicionales:** Presentación de 45 minutos. Añadimos las últimas capas de seguridad a OrderFlow: rate limiting diferenciado, OWASP Top 10 aplicado a microservicios, audit logging, y GDPR en sistemas distribuidos. Incluir diagrama de arquitectura de seguridad defense-in-depth. Cada sección ## es una diapositiva.

---

## M6.1 — Database per Service con SQL Server y EF Core 10

**Escribe para:** Desarrolladores .NET que quieren implementar persistencia correcta en microservicios: bases de datos aisladas, EF Core 10, Dapper y caching

**Tono:** Técnico y detallado, en español de España, con código de DbContext, migrations y queries

**Instrucciones adicionales:** Presentación de 60 minutos. Configuramos la persistencia definitiva de los 4 servicios de OrderFlow: 4 SQL Server databases (LocalDB en desarrollo, Azure SQL en producción). Incluir código completo de OrderDbContext con interceptors, migration bundles, consultas Dapper, y elastic pools de Azure SQL. Cada sección ## es una diapositiva.

---

## M6.2 — Consistencia eventual y el patrón Outbox

**Escribe para:** Desarrolladores .NET que quieren entender la consistencia eventual e implementar el Outbox pattern con MassTransit y EF Core

**Tono:** Técnico y conceptual, en español de España, con diagramas del problema y la solución

**Instrucciones adicionales:** Presentación de 60 minutos. Resolvemos el problema de la doble escritura en Orders.API con el Outbox pattern. Incluir diagrama del Teorema CAP, diagrama paso a paso del Outbox, código de configuración de MassTransit Outbox con EF Core, e Inbox Pattern para deduplicación. Cada sección ## es una diapositiva.

---

## M6.3 — CQRS avanzado y proyecciones

**Escribe para:** Desarrolladores .NET que quieren implementar CQRS con bases de datos separadas, proyecciones y Event Sourcing con SQL Server

**Tono:** Técnico y avanzado, en español de España, con diagramas de flujo y código de proyecciones

**Instrucciones adicionales:** Presentación de 60 minutos. Separamos el read model de Orders.API en una tabla desnormalizada de SQL Server. Implementamos Event Sourcing con tabla de eventos. Incluir código de proyecciones síncronas y asíncronas, consultas Dapper sobre el read model, y tabla de eventos en SQL Server. Cada sección ## es una diapositiva.

---

## M6.4 — Data migration y refactoring de datos

**Escribe para:** Desarrolladores .NET que necesitan migrar datos de un monolito a microservicios sin downtime usando Expand and Contract

**Tono:** Técnico y práctico, en español de España, con diagramas de migración paso a paso

**Instrucciones adicionales:** Presentación de 60 minutos. Simulamos la migración de la base de datos del monolito de TechShop a los 4 esquemas de OrderFlow. Incluir el patrón Expand and Contract explicado paso a paso con SQL, migration bundles en CI/CD, y estrategias de particionado de datos. Cada sección ## es una diapositiva.

---

## M7.1 — .NET Aspire en profundidad y contenedores como concepto

**Escribe para:** Desarrolladores .NET que quieren entender Aspire en profundidad y conocer Docker y Kubernetes como conceptos teóricos

**Tono:** Técnico y equilibrado, en español de España, práctico en Aspire y conceptual en Docker/K8s

**Instrucciones adicionales:** Presentación de 45 minutos. Profundizar en Aspire 9.1: AppHost, ServiceDefaults, recursos y dashboard. Docker y Kubernetes solo como conceptos teóricos — NO como prácticas del curso. Incluir código del AppHost completo de OrderFlow y comparativa Aspire vs Docker Compose. Cada sección ## es una diapositiva.

---

## M7.2 — Despliegue en Azure: App Services, SQL y Service Bus

**Escribe para:** Desarrolladores .NET que quieren desplegar microservicios en Azure App Services sin contenedores ni Kubernetes

**Tono:** Práctico y orientado a resultados, en español de España, con pasos claros de configuración de Azure

**Instrucciones adicionales:** Presentación de 60 minutos. Desplegamos OrderFlow completo en Azure: 4 App Services + Azure SQL Database (elastic pool) + Azure Service Bus. Incluir configuración de Managed Identity, cambio de MassTransit de RabbitMQ a Azure Service Bus (una línea), y deployment slots. Cada sección ## es una diapositiva.

---

## M7.3 — CI/CD para microservicios

**Escribe para:** Desarrolladores .NET que quieren automatizar el despliegue de microservicios con GitHub Actions hacia Azure App Services

**Tono:** Técnico y práctico, en español de España, con YAML de workflows completos

**Instrucciones adicionales:** Presentación de 60 minutos. Creamos los pipelines de CI/CD para todos los servicios de OrderFlow. Incluir YAML completo de GitHub Actions (CI + CD), estrategias de despliegue con deployment slots (Blue-Green, Canary), migration bundles en el pipeline, y smoke tests post-deploy. Cada sección ## es una diapositiva.

---

## M7.4 — Observabilidad en producción con Azure Monitor

**Escribe para:** Desarrolladores .NET que quieren configurar observabilidad completa en producción con Application Insights y Azure Monitor

**Tono:** Técnico y operacional, en español de España, con dashboards, queries KQL y gestión de incidentes

**Instrucciones adicionales:** Presentación de 45 minutos. Configuramos Application Insights para los 4 servicios de OrderFlow. Incluir configuración del Azure Monitor Exporter para OpenTelemetry, ejemplos de queries KQL para diagnóstico, Application Map con los 4 servicios, y alertas basadas en SLOs. Cerrar el curso con el recap visual de la evolución de TechShop. Cada sección ## es una diapositiva.

---

## Nota general para todas las presentaciones

**Idioma:** Español de España (vosotros, deploys, implementar, etc.)

**Número de diapositivas:** Cada sección marcada con ## es una diapositiva. No combinar ni dividir.

**Código:** Presentar en bloques de código con syntax highlighting cuando Gamma lo permita.

**Diagramas:** Pedir a Gamma que genere diagramas visuales cuando el texto lo indica (flujos, arquitecturas, comparativas).

**Hilo conductor:** TechShop es la empresa ficticia. OrderFlow es el proyecto que se construye. Mantener coherencia entre presentaciones.