import type { OrderStatus } from '../api/types';

const styles: Record<OrderStatus, string> = {
  Pending: 'bg-amber-100 text-amber-800 ring-amber-200',
  Confirmed: 'bg-emerald-100 text-emerald-800 ring-emerald-200',
  Shipped: 'bg-sky-100 text-sky-800 ring-sky-200',
  Delivered: 'bg-violet-100 text-violet-800 ring-violet-200',
  Cancelled: 'bg-red-100 text-red-800 ring-red-200',
  Failed: 'bg-rose-100 text-rose-800 ring-rose-200',
};

const labels: Record<OrderStatus, string> = {
  Pending: 'Pendiente',
  Confirmed: 'Confirmado',
  Shipped: 'Enviado',
  Delivered: 'Entregado',
  Cancelled: 'Cancelado',
  Failed: 'Fallido',
};

interface StatusBadgeProps {
  status: OrderStatus | string;
}

export default function StatusBadge({ status }: StatusBadgeProps) {
  const known = (status in styles) as boolean;
  const s = (known ? styles[status as OrderStatus] : 'bg-slate-100 text-slate-700 ring-slate-200');
  const l = (known ? labels[status as OrderStatus] : status);
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ring-1 ring-inset ${s}`}>
      {l}
    </span>
  );
}
