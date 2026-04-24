import type { SagaState } from '../api/types';

type StepStatus = 'done' | 'active' | 'pending' | 'failed' | 'cancelled';

interface Step {
  key: string;
  label: string;
  description: string;
  status: StepStatus;
}

function buildSteps(saga: SagaState): Step[] {
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

interface SagaTimelineProps {
  saga: SagaState | null | undefined;
  isRefreshing?: boolean;
}

export default function SagaTimeline({ saga, isRefreshing }: SagaTimelineProps) {
  if (!saga) {
    return (
      <div className="card p-4 text-sm text-slate-500">
        Aún no hay saga registrada para este pedido.
      </div>
    );
  }

  const steps = buildSteps(saga);

  return (
    <div className="card p-4">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="font-semibold text-slate-900">Estado del Saga</h3>
          <p className="text-xs text-slate-500">
            Estado actual: <span className="font-mono">{saga.state}</span>
          </p>
        </div>
        {isRefreshing && (
          <span className="text-xs text-brand-600 flex items-center gap-1">
            <span className="inline-block w-3 h-3 border-2 border-brand-300 border-t-brand-600 rounded-full animate-spin" />
            actualizando
          </span>
        )}
      </div>
      <ol className="relative border-l-2 border-slate-200 ml-2 space-y-6">
        {steps.map((step) => (
          <li key={step.key} className="ml-5">
            <span
              className={`absolute -left-[9px] flex items-center justify-center w-4 h-4 rounded-full ring-4 ${dotClasses(step.status)}`}
            />
            <div className="flex items-baseline gap-2">
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
            </div>
            <p className="text-xs text-slate-500 mt-0.5">{step.description}</p>
          </li>
        ))}
      </ol>
      {saga.failureReason && (
        <div className="mt-4 p-2 text-xs rounded bg-red-50 text-red-700 border border-red-100">
          <span className="font-semibold">Motivo: </span>
          {saga.failureReason}
        </div>
      )}
    </div>
  );
}
