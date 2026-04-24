import OutboxPanel from '../components/OutboxPanel';

export default function EventsPage() {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold">Eventos</h1>
        <p className="text-sm text-slate-500">
          Actividad del Outbox de <span className="font-mono">Orders.API</span> en vivo. Cada fila es un
          evento de integración publicado desde esta API hacia RabbitMQ. Incluye tanto eventos propios
          (<span className="font-mono">OrderCreated</span>, <span className="font-mono">OrderConfirmed</span>,
          <span className="font-mono">OrderFailed</span>…) como comandos emitidos por el Saga
          (<span className="font-mono">ReserveStock</span>, <span className="font-mono">ProcessPayment</span>,
          <span className="font-mono">ReleaseStock</span>).
        </p>
      </div>

      <OutboxPanel poll limit={50} />

      <section className="card p-4 text-xs text-slate-600 space-y-2">
        <h2 className="font-semibold text-slate-800 text-sm">¿Qué NO verás aquí?</h2>
        <ul className="list-disc list-inside space-y-1">
          <li>
            Eventos emitidos por <span className="font-mono">Products.API</span>
            (<span className="font-mono">StockReserved</span>, <span className="font-mono">StockReleased</span>) —
            esos viven en el Outbox de Products, no de Orders.
          </li>
          <li>
            Eventos emitidos por <span className="font-mono">Payments.API</span>
            (<span className="font-mono">PaymentProcessed</span>, <span className="font-mono">PaymentFailed</span>).
          </li>
          <li>
            Mensajes ya publicados y limpiados por MassTransit (<span className="font-mono">QueryDelay</span>
            = 1 s).
          </li>
        </ul>
        <p className="mt-2">
          Para la vista global absoluta con tasas de entrega, DLQ y consumers activos, abre la UI de
          RabbitMQ en{' '}
          <a
            href="http://localhost:15672/#/queues"
            target="_blank"
            rel="noopener noreferrer"
            className="text-brand-600 underline"
          >
            http://localhost:15672/#/queues
          </a>{' '}
          (guest/guest).
        </p>
      </section>
    </div>
  );
}
