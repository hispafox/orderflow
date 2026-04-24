import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getEventLog } from '../api/orders';
import type { EventLogEntryDto } from '../api/types';

interface EventLogPanelProps {
  correlationId?: string;
  poll?: boolean;
  limit?: number;
  title?: string;
}

type DirFilter = 'All' | 'Published' | 'Consumed';

function shortenType(raw: string): string {
  if (!raw) return '—';
  const noNamespace = raw.split('.').pop() ?? raw;
  return noNamespace.replace(/\+.*$/, '');
}

function shortenAddress(raw: string | null): string {
  if (!raw) return '';
  try {
    const url = new URL(raw);
    return url.pathname.replace(/^\//, '') || url.host;
  } catch {
    return raw;
  }
}

function formatTime(iso: string): string {
  try {
    const needsZ = !/[zZ]$|[+-]\d{2}:?\d{2}$/.test(iso);
    return new Date(needsZ ? `${iso}Z` : iso).toLocaleTimeString('es-ES', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  } catch {
    return iso;
  }
}

function directionBadge(dir: string): { label: string; className: string } {
  if (dir === 'Published') {
    return { label: 'PUB', className: 'bg-emerald-100 text-emerald-700 ring-emerald-200' };
  }
  if (dir === 'Consumed') {
    return { label: 'CONS', className: 'bg-sky-100 text-sky-700 ring-sky-200' };
  }
  return { label: dir, className: 'bg-slate-100 text-slate-700 ring-slate-200' };
}

export default function EventLogPanel({
  correlationId,
  poll = true,
  limit = 100,
  title = 'Historial de eventos',
}: EventLogPanelProps) {
  const [dirFilter, setDirFilter] = useState<DirFilter>('All');

  const query = useQuery({
    queryKey: ['events-log', { correlationId, limit }],
    queryFn: ({ signal }) => getEventLog({ correlationId, limit }, signal),
    refetchInterval: poll ? 2_000 : false,
    placeholderData: (prev) => prev,
  });

  const filtered: EventLogEntryDto[] = (() => {
    const rows = query.data ?? [];
    if (dirFilter === 'All') return rows;
    return rows.filter((r) => r.direction === dirFilter);
  })();

  return (
    <div className="card p-4 space-y-3">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h3 className="font-semibold text-slate-900">{title}</h3>
          <p className="text-xs text-slate-500">
            Tabla <span className="font-mono">[orders].[DemoEventLog]</span>.
            Persiste los publish/consume de MassTransit aunque el Outbox ya se haya limpiado.
            {correlationId && ' Filtrado por el pedido actual.'}
          </p>
        </div>
        {query.isFetching && !query.isLoading && (
          <span className="text-xs text-brand-600 flex items-center gap-1 shrink-0">
            <span className="inline-block w-3 h-3 border-2 border-brand-300 border-t-brand-600 rounded-full animate-spin" />
            live
          </span>
        )}
      </div>

      <div className="flex gap-1 text-xs">
        {(['All', 'Published', 'Consumed'] as const).map((v) => (
          <button
            key={v}
            type="button"
            className={
              'px-2 py-1 rounded border ' +
              (dirFilter === v
                ? 'bg-brand-600 text-white border-brand-600'
                : 'bg-white text-slate-700 border-slate-300 hover:bg-slate-50')
            }
            onClick={() => setDirFilter(v)}
          >
            {v === 'All' ? 'Todos' : v === 'Published' ? 'Publicados' : 'Consumidos'}
          </button>
        ))}
        <span className="ml-auto text-slate-400 py-1">
          {filtered.length} / {query.data?.length ?? 0}
        </span>
      </div>

      {query.isLoading && <div className="text-xs text-slate-500">Cargando…</div>}

      {query.error && (
        <div className="text-xs text-red-700 bg-red-50 border border-red-100 rounded p-2">
          No se pudo leer el log: {(query.error as Error).message}
        </div>
      )}

      {!query.isLoading && filtered.length === 0 && (
        <div className="text-xs text-slate-500 py-4 text-center">
          {correlationId
            ? 'Ningún evento registrado para este pedido aún.'
            : 'No hay eventos. Crea un pedido.'}
        </div>
      )}

      {filtered.length > 0 && (
        <ul className="divide-y divide-slate-100 max-h-[28rem] overflow-y-auto -mx-4">
          {filtered.map((m) => {
            const badge = directionBadge(m.direction);
            return (
              <li key={m.id} className="px-4 py-2 text-xs">
                <div className="flex items-baseline gap-2 flex-wrap">
                  <span
                    className={`inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-bold ring-1 ring-inset ${badge.className}`}
                  >
                    {badge.label}
                  </span>
                  <span className="font-medium text-slate-900">
                    {shortenType(m.messageType)}
                  </span>
                  <span className="text-slate-400 font-mono ml-auto">
                    {formatTime(m.occurredAt)}
                  </span>
                </div>
                <div className="flex items-center gap-2 mt-0.5 text-slate-500">
                  {m.destinationAddress && (
                    <span className="font-mono truncate" title={m.destinationAddress}>
                      → {shortenAddress(m.destinationAddress)}
                    </span>
                  )}
                  {m.correlationId && (
                    <span
                      className="ml-auto font-mono text-[10px] text-slate-400 truncate max-w-[10rem]"
                      title={m.correlationId}
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
