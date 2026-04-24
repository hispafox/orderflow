# Cambiar entre RabbitMQ e InMemory en MassTransit

Cuando RabbitMQ no está corriendo en local (o simplemente no lo necesitas para la tarea en la que estás trabajando), puedes cambiar el transport de MassTransit a **InMemory** con una sola línea en el `appsettings.Development.json` del servicio.

---

## El flag

```json
{
  "Messaging": {
    "Transport": "InMemory"
  }
}
```

| Valor | Comportamiento |
|---|---|
| `"InMemory"` | MassTransit usa un bus en memoria. No necesita RabbitMQ. Los mensajes no sobreviven reinicios. |
| `"RabbitMQ"` | MassTransit conecta a `amqp://guest:guest@localhost:5672`. Requiere RabbitMQ corriendo. |

El valor por defecto en `appsettings.Development.json` es **`"InMemory"`** para que el entorno de desarrollo arranque sin dependencias externas.

---

## Cómo cambiarlo

Abre el `appsettings.Development.json` del servicio que quieras cambiar:

```
src/Orders.API/appsettings.Development.json
src/Products.API/appsettings.Development.json
src/Payments.API/appsettings.Development.json
src/Notifications.API/appsettings.Development.json
```

Cambia el valor:

```json
"Messaging": {
  "Transport": "RabbitMQ"
}
```

No hace falta reiniciar Aspire desde cero — basta con reiniciar el servicio afectado desde el Dashboard.

---

## Cómo funciona internamente

A partir de **M4.2**, cada servicio con MassTransit lee el flag en su `Program.cs`:

```csharp
var transport = builder.Configuration["Messaging:Transport"] ?? "RabbitMQ";

builder.Services.AddMassTransit(x =>
{
    // consumers registrados aquí...

    if (transport == "InMemory")
    {
        x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
    }
    else
    {
        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(builder.Configuration.GetConnectionString("messaging"));
            cfg.ConfigureEndpoints(ctx);
        });
    }
});
```

Cuando el transport es `"InMemory"`, tampoco se registran:
- El health check de RabbitMQ (`.AddRabbitMQ(...)`)
- El singleton `IConnection` de RabbitMQ.Client

---

## Limitaciones del modo InMemory

- Los mensajes son locales al proceso — **no hay comunicación real entre servicios**.
- Los Sagas (M4.3) funcionan si todos los servicios implicados están en el mismo proceso, lo que no es el caso aquí. Para probar Sagas necesitas RabbitMQ.
- Las pruebas de integración entre servicios (end-to-end) requieren RabbitMQ o un broker real.
- **La demo SPA end-to-end necesita RabbitMQ** (el flujo crear pedido → reservar stock → pagar → confirmar cruza tres servicios).

Para el desarrollo habitual de un servicio de forma aislada, InMemory es suficiente.

---

## Nota sobre la versión de MassTransit

El proyecto está pinneado a **MassTransit 8.5.2**. La 9.x requiere
licencia comercial y falla tanto en modo RabbitMQ como en InMemory con
`MassTransit.ConfigurationException: License must be specified`. No
actualizar — ver [Setup-Local.md](Setup-Local.md) sección 2.
