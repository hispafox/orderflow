import type { OrderDto, OrderStatus, SagaState } from '../api/types';

type StepStatus = 'done' | 'active' | 'pending' | 'failed' | 'cancelled';

interface Step {
  key: string;
  label: string;
  description: string;
  status: StepStatus;
  timestamp?: string | null;
}

const TERMINAL_ORDER_STATES: OrderStatus[] = ['Confirmed', 'Cancelled', 'Delivered'];

function buildStepsFromSaga(saga: SagaState): Step[] {
  const state = saga.state;
  const isTerminalConfirmed = state === 'Confirmed';
  const isCancelled = state === 'Cancelled';
  const isFailed = state === 'Failed';

  const mark = (thresholds: string[], currentActiveOn: string): StepStatus => {
    if (isCancelled) return thresholds.includes(state) ? 'done' : 'cancelled';
    if (isFailed && !thresholds.includes(state)) return 'failed';
    if (thresholds.includes(state)) return 'done';
    if (state === currentActiveOn) return 'active';
    return 'pending';
  };

  return [
    {
      key: 'created',
      label: 'Pedido creado',
      description: 'La orden fue aceptada por Orders.API',
      status: 'done',
      timestamp: saga.createdAt,
    },
    {
      key: 'stock',
      label: 'Reserva de stock',
      description: 'Products.API reserva unidades del inventario',
      status: mark(['AwaitingPayment', 'Confirmed'], 'AwaitingStock'),
    },
    {
      key: 'payment',
      label: 'Procesamiento de pago',
      description: saga.paymentId
        ? `Payments.API · id ${saga.paymentId.slice(0, 8)}…`
        : 'Payments.API valida el cobro',
      status: mark(['Confirmed'], 'AwaitingPayment'),
    },
    {
      key: 'confirmed',
      label: 'Pedido confirmado',
      description: isCancelled
        ? 'Saga terminó en cancelación'
        : isFailed
        ? saga.failureReason ?? 'Saga terminó en fallo'
        : 'Evento OrderConfirmed publicado',
      status: isTerminalConfirmed ? 'done' : isCancelled ? 'cancelled' : isFailed ? 'failed' : 'pending',
      timestamp: saga.completedAt,
    },
  ];
}

function buildStepsFromOrder(order: OrderDto): Step[] {
  const isConfirmed = order.status === 'Confirmed' || order.status === 'Shipped' || order.status === 'Delivered';
  const isCancelled = order.status === 'Cancelled';
  const terminalStatus: StepStatus = isConfirmed ? 'done' : isCancelled ? 'cancelled' : 'pending';
  const terminalTs = order.confirmedAt ?? order.cancelledAt ?? null;

  return [
    {
      key: 'created',
      label: 'Pedido creado',
      description: 'La orden fue aceptada por Orders.API',
      status: 'done',
      timestamp: order.createdAt,
    },
    {
      key: 'stock',
      label: 'Reserva de stock',
      description: isCancelled
        ? 'Compensado por el Saga al cancelar'
        : 'Products.API reservó las unidades del inventario',
      status: terminalStatus,
    },
    {
      key: 'payment',
      label: 'Procesamiento de pago',
      description: isConfirmed
        ? 'Payments.API confirmó el cobro'
        : isCancelled
        ? 'Sin cargo (pedido cancelado)'
        : 'Payments.API valida el cobro',
      status: terminalStatus,
    },
    {
      key: 'confirmed',
      label: isCancelled ? 'Pedido cancelado' : 'Pedido confirmado',
      description: isCancelled
        ? order.cancellationReason ?? 'Cancelado desde la UI'
        : 'Evento OrderConfirmed publicado · Saga finalizada',
      status: terminalStatus,
      timestamp: terminalTs,
    },
  ];
}

function dotClasses(status: StepStatus): string {
  switch (status) {
    case 'done':
      return 'bg-emerald-500 ring-emerald-200';
    case 'active':
      return 'bg-brand-500 ring-brand-200 animate-pulse';
    case 'failed':
      return 'bg-red-500 ring-red-200';
    case 'cancelled':
      return 'bg-slate-400 ring-slate-200';
    case 'pending':
    default:
      return 'bg-slate-200 ring-slate-100';
  }
}

function formatTs(iso: string | null | undefined): string | null {
  if (!iso) return null;
  try {
    return new Date(iso).toLocaleTimeString('es-ES', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  } catch {
    return null;
  }
}

function formatDurationMs(ms: number): string {
  if (ms < 1000) return `${Math.round(ms)} ms`;
  const s = ms / 1000;
  if (s < 60) return `${s.toFixed(1)} s`;
  const m = Math.floor(s / 60);
  const rs = Math.round(s - m * 60);
  return `${m} min ${rs} s`;
}

interface SagaTimelineProps {
  saga: SagaState | null | undefined;
  order: OrderDto;
  isRefreshing?: boolean;
  isSagaMissing?: boolean;
}

export default function SagaTimeline({ saga, order, isRefreshing, isSagaMissing }: SagaTimelineProps) {
  const orderIsTerminal = TERMINAL_ORDER_STATES.includes(order.status);

  // Caso 1 · Tenemos saga activa → timeline en vivo
  // Caso 2 · 404 + pedido en estado terminal → saga completó y su fila fue purgada por MassTransit
  //          (SetCompletedWhenFinalized). Reconstruimos post-mortem desde el Order.
  // Caso 3 · 404 + pedido aún Pending → saga no ha empezado (primeros segundos tras POST).
  const useSaga = Boolean(saga);
  const usePostMortem = !useSaga && isSagaMissing && orderIsTerminal;

  if (!useSaga && !usePostMortem) {
    return (
      <div className="card p-4 text-sm text-slate-500 space-y-2">
        <div className="font-medium text-slate-700">Saga aún no iniciado</div>
        <p className="text-xs">
          El evento <span className="font-mono">OrderCreated</span> está en el Outbox y será publicado
          en breve. La vista refrescará sola.
        </p>
        <p className="text-xs text-slate-400">
          Creado hace {formatDurationMs(Date.now() - new Date(order.createdAt).getTime())}.
        </p>
      </div>
    );
  }

  const steps = useSaga ? buildStepsFromSaga(saga!) : buildStepsFromOrder(order);
  const currentState = useSaga ? saga!.state : order.status;
  const createdAt = useSaga ? saga!.createdAt : order.createdAt;
  const completedAt = useSaga
    ? saga!.completedAt
    : order.confirmedAt ?? order.cancelledAt ?? null;
  const paymentId = useSaga ? saga!.paymentId : null;
  const failureReason = useSaga
    ? saga!.failureReason
    : order.status === 'Cancelled'
    ? order.cancellationReason
    : null;

  const durationMs =
    completedAt
      ? new Date(completedAt).getTime() - new Date(createdAt).getTime()
      : Date.now() - new Date(createdAt).getTime();

  return (
    <div className="card p-4 space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="font-semibold text-slate-900">Estado del Saga</h3>
          <p className="text-xs text-slate-500">
            {useSaga ? (
              <>Estado actual: <span className="font-mono">{currentState}</span></>
            ) : (
              <>Saga finalizado · reconstruido desde el pedido</>
            )}
          </p>
        </div>
        {isRefreshing && (
          <span className="text-xs text-brand-600 flex items-center gap-1">
            <span className="inline-block w-3 h-3 border-2 border-brand-300 border-t-brand-600 rounded-full animate-spin" />
            actualizando
          </span>
        )}
      </div>

      <ol className="relative border-l-2 border-slate-200 ml-2 space-y-5">
        {steps.map((step) => {
          const ts = formatTs(step.timestamp);
          return (
            <li key={step.key} className="ml-5">
              <span
                className={`absolute -left-[9px] flex items-center justify-center w-4 h-4 rounded-full ring-4 ${dotClasses(step.status)}`}
              />
              <div className="flex items-baseline gap-2 flex-wrap">
                <span className="font-medium text-slate-900">{step.label}</span>
                {step.status === 'active' && (
                  <span className="text-xs text-brand-600">en curso</span>
                )}
                {step.status === 'failed' && (
                  <span className="text-xs text-red-600">fallo</span>
                )}
                {step.status === 'cancelled' && (
                  <span className="text-xs text-slate-500">cancelado</span>
                )}
                {ts && <span className="text-xs text-slate-400 font-mono ml-auto">{ts}</span>}
              </div>
              <p className="text-xs text-slate-500 mt-0.5">{step.description}</p>
            </li>
          );
        })}
      </ol>

      <dl className="grid grid-cols-2 gap-x-3 gap-y-2 pt-3 border-t border-slate-100 text-xs">
        <dt className="text-slate-500">Duración</dt>
        <dd className="font-mono text-slate-900 text-right">
          {formatDurationMs(durationMs)}
          {!completedAt && <span className="text-slate-400"> (en curso)</span>}
        </dd>

        <dt className="text-slate-500">Inicio</dt>
        <dd className="font-mono text-slate-900 text-right">
          {new Date(createdAt).toLocaleString('es-ES')}
        </dd>

        {completedAt && (
          <>
            <dt className="text-slate-500">Fin</dt>
            <dd className="font-mono text-slate-900 text-right">
              {new Date(completedAt).toLocaleString('es-ES')}
            </dd>
          </>
        )}

        {paymentId && (
          <>
            <dt className="text-slate-500">Payment Id</dt>
            <dd className="font-mono text-slate-900 text-right break-all">
              {paymentId}
            </dd>
          </>
        )}

        <dt className="text-slate-500">Correlación</dt>
        <dd className="font-mono text-slate-900 text-right break-all">{order.id}</dd>
      </dl>

      {failureReason && (
        <div className="p-2 text-xs rounded bg-red-50 text-red-700 border border-red-100">
          <span className="font-semibold">Motivo: </span>
          {failureReason}
        </div>
      )}
    </div>
  );
}
