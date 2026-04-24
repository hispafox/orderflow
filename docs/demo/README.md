# docs/demo

Documentación **operativa y de demostración** del proyecto OrderFlow.
Agrupa todo el material que no forma parte de la numeración del curso
(M1.1 … M7.4) pero que es imprescindible para poner en marcha, probar
o enseñar la solución.

## Índice

| Documento | Para qué sirve |
|---|---|
| [Setup-Local.md](Setup-Local.md) | Instalación del entorno local (Chocolatey, Erlang, RabbitMQ, LocalDB, Node.js, Git) + configuración del dev cert + verificación + DevLauncher. |
| [Demo-Frontend-React.md](Demo-Frontend-React.md) | SPA de demostración (React + Vite + TypeScript) en `web/`. Rutas, endpoints consumidos, visualización del Saga, troubleshooting. |
| [Inter-Service-Communications.md](Inter-Service-Communications.md) | Guía arquitectónica con diagramas Mermaid: topología, happy path, flujos de compensación, máquina de estados del Saga, Outbox, read model, correlation ID, Polly. |
| [Demo-Saga-Compensacion.md](Demo-Saga-Compensacion.md) | Guion paso a paso para probar en clase los 3 escenarios del Saga (happy path, compensación forzada, race condition). Incluye el cambio exacto en `FakePaymentGateway`. |
| [messaging-transport-switch.md](messaging-transport-switch.md) | Switch `Messaging:Transport` entre RabbitMQ e InMemory por servicio y sus limitaciones. |

## Cuándo usar cada documento

- **¿Es tu primera vez?** → empieza por `Setup-Local.md`.
- **¿Quieres entender cómo hablan los servicios?** → `Inter-Service-Communications.md`.
- **¿Vas a demostrar el flujo en una clase/reunión?** → `Demo-Frontend-React.md` para poner la SPA, y `Demo-Saga-Compensacion.md` para el guion de los tres escenarios.
- **¿RabbitMQ no está disponible y necesitas seguir adelante?** → `messaging-transport-switch.md`.

## Qué NO hay aquí

- Los módulos del curso (`M1.1…M7.4`) están en [../](../).
- La planificación detallada del temario está en [../Planificacion/](../Planificacion/).
- El material de apoyo "Código" del curso está en [../Codigo/](../Codigo/).
