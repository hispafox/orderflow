import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { listOrders } from '../api/orders';
import type { OrderStatus } from '../api/types';
import Money from '../components/Money';
import Spinner from '../components/Spinner';
import ErrorBanner from '../components/ErrorBanner';
import StatusBadge from '../components/StatusBadge';

const PAGE_SIZE = 20;
const STATUSES: OrderStatus[] = ['Pending', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled'];

export default function OrdersPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<OrderStatus | ''>('');

  const query = useQuery({
    queryKey: ['orders', { page, pageSize: PAGE_SIZE, status: status || undefined }],
    queryFn: ({ signal }) =>
      listOrders({ page, pageSize: PAGE_SIZE, status: status || undefined }, signal),
    placeholderData: (prev) => prev,
  });

  return (
    <div className="space-y-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Pedidos</h1>
          <p className="text-sm text-slate-500">
            Lectura del read model de <span className="font-mono">Orders.API</span>.
          </p>
        </div>
        <Link to="/orders/new" className="btn-primary">+ Nuevo pedido</Link>
      </div>

      <div className="card p-4 flex flex-wrap gap-3 items-end">
        <div className="flex-1 min-w-[180px]">
          <label className="label" htmlFor="status-filter">Filtrar por estado</label>
          <select
            id="status-filter"
            className="input"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as OrderStatus | '');
              setPage(1);
            }}
          >
            <option value="">Todos</option>
            {STATUSES.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>
        {query.isFetching && !query.isLoading && (
          <span className="text-xs text-slate-500 pb-2">actualizando…</span>
        )}
      </div>

      {query.isLoading && <Spinner label="Cargando pedidos…" />}
      {query.error && !query.isLoading && (
        <ErrorBanner error={query.error} onRetry={() => query.refetch()} />
      )}

      {query.data && (
        <>
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-slate-600 text-left text-xs uppercase">
                <tr>
                  <th className="px-4 py-3">Pedido</th>
                  <th className="px-4 py-3">Cliente</th>
                  <th className="px-4 py-3">Estado</th>
                  <th className="px-4 py-3 text-right">Líneas</th>
                  <th className="px-4 py-3 text-right">Total</th>
                  <th className="px-4 py-3">Creado</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {query.data.items.map((o) => (
                  <tr key={o.id} className="hover:bg-slate-50">
                    <td className="px-4 py-3">
                      <Link to={`/orders/${o.id}`} className="text-brand-600 hover:underline font-mono text-xs">
                        {o.id.slice(0, 8)}…
                      </Link>
                      {o.firstItemName && (
                        <div className="text-xs text-slate-500 mt-0.5">
                          {o.firstItemName}{o.lineCount > 1 ? ` +${o.lineCount - 1}` : ''}
                        </div>
                      )}
                    </td>
                    <td className="px-4 py-3 text-slate-700">{o.customerEmail || '—'}</td>
                    <td className="px-4 py-3"><StatusBadge status={o.status} /></td>
                    <td className="px-4 py-3 text-right">{o.lineCount}</td>
                    <td className="px-4 py-3 text-right font-medium">
                      <Money amount={o.total} currency={o.currency} />
                    </td>
                    <td className="px-4 py-3 text-xs text-slate-500">
                      {new Date(o.createdAt).toLocaleString('es-ES')}
                    </td>
                  </tr>
                ))}
                {query.data.items.length === 0 && (
                  <tr>
                    <td colSpan={6} className="px-4 py-10 text-center text-slate-500 text-sm">
                      No hay pedidos con ese filtro.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between text-sm">
            <span className="text-slate-500">
              Página {query.data.page} de {query.data.totalPages || 1} · {query.data.totalCount} total
            </span>
            <div className="flex gap-2">
              <button
                type="button"
                className="btn-secondary"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={!query.data.hasPreviousPage}
              >
                ← Anterior
              </button>
              <button
                type="button"
                className="btn-secondary"
                onClick={() => setPage((p) => p + 1)}
                disabled={!query.data.hasNextPage}
              >
                Siguiente →
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
