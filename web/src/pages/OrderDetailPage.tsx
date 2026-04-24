import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  cancelOrder,
  confirmOrder,
  getOrder,
  getSagaState,
} from '../api/orders';
import type { OrderStatus, SagaState } from '../api/types';
import Money from '../components/Money';
import Spinner from '../components/Spinner';
import ErrorBanner from '../components/ErrorBanner';
import StatusBadge from '../components/StatusBadge';
import SagaTimeline from '../components/SagaTimeline';
import { ApiError } from '../api/client';

const TERMINAL_SAGA_STATES = new Set(['Confirmed', 'Cancelled', 'Failed']);
const TERMINAL_ORDER_STATES: OrderStatus[] = ['Confirmed', 'Cancelled', 'Delivered'];

export default function OrderDetailPage() {
  const { id = '' } = useParams();
  const qc = useQueryClient();

  const orderQ = useQuery({
    queryKey: ['orders', id],
    queryFn: ({ signal }) => getOrder(id, signal),
    enabled: Boolean(id),
    refetchInterval: (q) => {
      const data = q.state.data;
      if (data && TERMINAL_ORDER_STATES.includes(data.status)) return false;
      return 2_000;
    },
  });

  const sagaQ = useQuery({
    queryKey: ['orders', id, 'saga'],
    queryFn: ({ signal }) => getSagaState(id, signal),
    enabled: Boolean(id),
    retry: (failureCount, err) => {
      if (err instanceof ApiError && err.status === 404) return false;
      return failureCount < 2;
    },
    refetchInterval: (q) => {
      const data = q.state.data as SagaState | undefined;
      if (data && TERMINAL_SAGA_STATES.has(data.state)) return false;
      const order = qc.getQueryData<typeof orderQ.data>(['orders', id]);
      if (order && TERMINAL_ORDER_STATES.includes(order.status)) return false;
      return 2_000;
    },
  });

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ['orders', id] });
    qc.invalidateQueries({ queryKey: ['orders', id, 'saga'] });
    qc.invalidateQueries({ queryKey: ['orders'] });
  };

  const confirmMut = useMutation({
    mutationFn: () => confirmOrder(id),
    onSuccess: invalidate,
  });

  const [cancelReason, setCancelReason] = useState('');
  const [showCancel, setShowCancel] = useState(false);

  const cancelMut = useMutation({
    mutationFn: (reason: string) => cancelOrder(id, { reason }),
    onSuccess: () => {
      setShowCancel(false);
      setCancelReason('');
      invalidate();
    },
  });

  if (!id) return <ErrorBanner error={new Error('Falta el id del pedido en la URL')} />;
  if (orderQ.isLoading) return <Spinner label="Cargando pedido…" />;
  if (orderQ.error) return <ErrorBanner error={orderQ.error} onRetry={() => orderQ.refetch()} />;
  if (!orderQ.data) return null;

  const order = orderQ.data;
  const canAct = !TERMINAL_ORDER_STATES.includes(order.status);
  const isSagaMissing = sagaQ.error instanceof ApiError && sagaQ.error.status === 404;

  return (
    <div className="space-y-6">
      <nav className="text-sm text-slate-500">
        <Link to="/orders" className="hover:underline">← Volver a pedidos</Link>
      </nav>

      <header className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">
            Pedido <span className="font-mono text-slate-600 text-lg">{order.id.slice(0, 8)}…</span>
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Creado el {new Date(order.createdAt).toLocaleString('es-ES')}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <StatusBadge status={order.status} />
          <Money amount={order.total} currency={order.currency} className="text-xl font-semibold" />
        </div>
      </header>

      {canAct && (
        <div className="card p-4 flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm text-slate-600">Acciones disponibles sobre este pedido:</p>
          <div className="flex gap-2">
            <button
              type="button"
              className="btn-primary"
              onClick={() => confirmMut.mutate()}
              disabled={confirmMut.isPending || order.status !== 'Pending'}
              title={order.status !== 'Pending' ? 'Solo se puede confirmar un pedido Pending' : undefined}
            >
              {confirmMut.isPending ? 'Confirmando…' : 'Confirmar pedido'}
            </button>
            <button
              type="button"
              className="btn-danger"
              onClick={() => setShowCancel((v) => !v)}
              disabled={cancelMut.isPending}
            >
              Cancelar
            </button>
          </div>
        </div>
      )}

      {confirmMut.error && <ErrorBanner error={confirmMut.error} />}
      {cancelMut.error && <ErrorBanner error={cancelMut.error} />}

      {showCancel && canAct && (
        <div className="card p-4 space-y-3 border-red-200">
          <label className="label" htmlFor="cancel-reason">Motivo de la cancelación</label>
          <input
            id="cancel-reason"
            className="input"
            value={cancelReason}
            onChange={(e) => setCancelReason(e.target.value)}
            maxLength={500}
            placeholder="Ej. cliente solicita cambio"
          />
          <div className="flex gap-2 justify-end">
            <button
              type="button"
              className="btn-secondary"
              onClick={() => setShowCancel(false)}
            >
              Volver
            </button>
            <button
              type="button"
              className="btn-danger"
              disabled={!cancelReason.trim() || cancelMut.isPending}
              onClick={() => cancelMut.mutate(cancelReason.trim())}
            >
              {cancelMut.isPending ? 'Cancelando…' : 'Confirmar cancelación'}
            </button>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <section className="lg:col-span-2 card p-5">
          <h2 className="font-semibold text-slate-900 mb-4">Líneas</h2>
          <table className="w-full text-sm">
            <thead className="text-xs uppercase text-slate-500">
              <tr>
                <th className="text-left pb-2">Producto</th>
                <th className="text-right pb-2">Cant.</th>
                <th className="text-right pb-2">PVP</th>
                <th className="text-right pb-2">Subtotal</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {order.lines.map((l) => (
                <tr key={l.id}>
                  <td className="py-2">
                    <div className="font-medium">{l.productName}</div>
                    <div className="text-xs text-slate-400 font-mono">{l.productId}</div>
                  </td>
                  <td className="py-2 text-right">{l.quantity}</td>
                  <td className="py-2 text-right">
                    <Money amount={l.unitPrice} currency={order.currency} />
                  </td>
                  <td className="py-2 text-right font-medium">
                    <Money amount={l.lineTotal} currency={order.currency} />
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <td colSpan={3} className="pt-3 text-right font-semibold">Total</td>
                <td className="pt-3 text-right font-bold">
                  <Money amount={order.total} currency={order.currency} />
                </td>
              </tr>
            </tfoot>
          </table>

          <div className="mt-6 pt-4 border-t border-slate-100">
            <h3 className="font-semibold text-slate-900 mb-2">Dirección de envío</h3>
            <address className="not-italic text-sm text-slate-600 leading-relaxed">
              {order.shippingAddress.street}<br />
              {order.shippingAddress.zipCode} {order.shippingAddress.city}<br />
              {order.shippingAddress.country}
            </address>
          </div>

          {order.cancellationReason && (
            <div className="mt-4 p-3 text-sm bg-red-50 text-red-800 rounded border border-red-100">
              <span className="font-semibold">Cancelado:</span> {order.cancellationReason}
            </div>
          )}
        </section>

        <aside className="space-y-4">
          {sagaQ.isLoading && !isSagaMissing && <Spinner label="Leyendo saga…" />}
          {!isSagaMissing && sagaQ.error && <ErrorBanner error={sagaQ.error} />}
          {(sagaQ.data || isSagaMissing) && (
            <SagaTimeline
              saga={sagaQ.data}
              order={order}
              isRefreshing={(sagaQ.isFetching && !sagaQ.isLoading) || (orderQ.isFetching && !orderQ.isLoading)}
              isSagaMissing={isSagaMissing}
            />
          )}
        </aside>
      </div>
    </div>
  );
}
