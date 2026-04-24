import EventLogPanel from '../components/EventLogPanel';
import OutboxPanel from '../components/OutboxPanel';

export default function EventsPage() {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold">Eventos</h1>
        <p className="text-sm text-slate-500">
          Dos vistas de la actividad de <span className="font-mono">Orders.API</span> sobre el bus
          MassTransit:
        </p>
        <ul className="text-sm text-slate-600 list-disc list-inside mt-1">
          <li>
            <b>Historial persistente</b> — tabla propia alimentada por observers MassTransit. Guarda
            todo lo publicado y todo lo consumido, sin limpieza.
          </li>
          <li>
            <b>Outbox crudo</b> — vista directa a <span className="font-mono">[orders].[OutboxMessage]</span>,
            que MassTransit purga ~1s después de publicar. Útil para pillar mensajes en vuelo.
          </li>
        </ul>
      </div>

      <EventLogPanel poll limit={100} />

      <OutboxPanel poll limit={30} />

      <section className="card p-4 text-xs text-slate-600 space-y-2">
        <h2 className="font-semibold text-slate-800 text-sm">¿Qué NO verás aquí?</h2>
        <ul className="list-disc list-inside space-y-1">
          <li>
            Eventos publicados por <span className="font-mono">Products.API</span> o
            <span className="font-mono"> Payments.API</span> — los observers solo están en
            <span className="font-mono"> Orders.API</span>. Aun así, los eventos de esos servicios que
            Orders consume (<span className="font-mono">StockReserved</span>,
            <span className="font-mono"> PaymentProcessed</span>, …) aparecen como
            <b> Consumed</b> aquí.
          </li>
          <li>
            Eventos entre <span className="font-mono">Products.API</span> y
            <span className="font-mono"> Payments.API</span> que no involucren a Orders (no hay en el
            sistema actual).
          </li>
        </ul>
        <p className="mt-2">
          Para la vista global absoluta del broker con tasas de entrega, DLQ y consumers, abre{' '}
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
