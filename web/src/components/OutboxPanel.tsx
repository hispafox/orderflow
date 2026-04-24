import { useQuery } from '@tanstack/react-query';
import { getRecentOutbox } from '../api/orders';
import type { OutboxMessageDto } from '../api/types';

interface OutboxPanelProps {
  correlationId?: string;
  poll?: boolean;
  limit?: number;
}

function shortenType(raw: string): string {
  if (!raw) return '—';
  const first = raw.split('\n')[0].trim();
  const lastSegment = first.split(':').pop() ?? first;
  return lastSegment.replace(/^urn:message:.*:/, '');
}

function shortenAddress(raw: string | null): string {
  if (!raw) return '';
  try {
    const url = new URL(raw);
    return url.pathname.replace(/^\//, '') || raw;
  } catch {
    return raw;
  }
}

function formatTime(iso: string): string {
  try {
    return new Date(iso).toLocaleTimeString('es-ES', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  } catch {
    return iso;
  }
}

function matches(msg: OutboxMessageDto, correlationId?: string): boolean {
  if (!correlationId) return false;
  return msg.correlationId === correlationId || msg.conversationId === correlationId;
}

export default function OutboxPanel({
  correlationId,
  poll = true,
  limit = 30,
}: OutboxPanelProps) {
  const query = useQuery({
    queryKey: ['outbox', { limit }],
    queryFn: ({ signal }) => getRecentOutbox(limit, signal),
    refetchInterval: poll ? 2_000 : false,
    placeholderData: (prev) => prev,
  });

  return (
    <div className="card p-4 space-y-3">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="font-semibold text-slate-900">Outbox de Orders.API</h3>
          <p className="text-xs text-slate-500">
            Últimos {limit} mensajes de <span className="font-mono">[orders].[OutboxMessage]</span>.
            {correlationId && ' Los del pedido actual aparecen resaltados.'}
          </p>
        </div>
        {query.isFetching && !query.isLoading && (
          <span className="text-xs text-brand-600 flex items-center gap-1">
            <span className="inline-block w-3 h-3 border-2 border-brand-300 border-t-brand-600 rounded-full animate-spin" />
            live
          </span>
        )}
      </div>

      {query.isLoading && <div className="text-xs text-slate-500">Cargando…</div>}

      {query.error && (
        <div className="text-xs text-red-700 bg-red-50 border border-red-100 rounded p-2">
          No se pudo leer el Outbox: {(query.error as Error).message}
        </div>
      )}

      {query.data && query.data.length === 0 && (
        <div className="text-xs text-slate-500 py-4 text-center">
          No hay mensajes todavía. Crea un pedido para activarlo.
        </div>
      )}

      {query.data && query.data.length > 0 && (
        <ul className="divide-y divide-slate-100 max-h-[22rem] overflow-y-auto -mx-4">
          {query.data.map((m) => {
            const mine = matches(m, correlationId);
            return (
              <li
                key={m.sequenceNumber}
                className={`px-4 py-2 text-xs ${mine ? 'bg-brand-50/60' : ''}`}
              >
                <div className="flex items-baseline justify-between gap-2">
                  <span className={`font-medium ${mine ? 'text-brand-700' : 'text-slate-800'}`}>
                    {shortenType(m.messageType)}
                  </span>
                  <span className="text-slate-400 font-mono">{formatTime(m.sentTime)}</span>
                </div>
                <div className="flex items-center gap-2 mt-0.5 text-slate-500">
                  <span className="font-mono truncate">{shortenAddress(m.destinationAddress)}</span>
                  {m.correlationId && (
                    <span
                      title="CorrelationId"
                      className={`font-mono text-[10px] ml-auto truncate max-w-[10rem] ${mine ? 'text-brand-600' : 'text-slate-400'}`}
                    >
                      {m.correlationId.slice(0, 8)}…
                    </span>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
