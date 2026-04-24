import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { listProducts } from '../api/products';
import { listOrders } from '../api/orders';
import Spinner from '../components/Spinner';
import ErrorBanner from '../components/ErrorBanner';

export default function HomePage() {
  const productsQ = useQuery({
    queryKey: ['products', { page: 1, pageSize: 1 }],
    queryFn: ({ signal }) => listProducts({ page: 1, pageSize: 1 }, signal),
  });

  const ordersQ = useQuery({
    queryKey: ['orders', { page: 1, pageSize: 1 }],
    queryFn: ({ signal }) => listOrders({ page: 1, pageSize: 1 }, signal),
  });

  const isLoading = productsQ.isLoading || ordersQ.isLoading;
  const error = productsQ.error ?? ordersQ.error;

  return (
    <div className="space-y-6">
      <section>
        <h1 className="text-2xl font-bold text-slate-900">OrderFlow · panel de demo</h1>
        <p className="mt-1 text-slate-600 text-sm">
          SPA de muestra para visualizar el flujo distribuido del sistema: catálogo de productos,
          creación de pedidos y orquestación del Saga entre <span className="font-mono">Orders.API</span>,{' '}
          <span className="font-mono">Products.API</span> y <span className="font-mono">Payments.API</span>.
        </p>
      </section>

      {isLoading && <Spinner label="Consultando servicios…" />}
      {error && !isLoading && (
        <ErrorBanner
          error={error}
          onRetry={() => {
            productsQ.refetch();
            ordersQ.refetch();
          }}
        />
      )}

      {!isLoading && !error && (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <KpiCard
            to="/products"
            label="Productos en catálogo"
            value={productsQ.data?.totalCount ?? 0}
            cta="Ver catálogo"
          />
          <KpiCard
            to="/orders"
            label="Pedidos registrados"
            value={ordersQ.data?.totalCount ?? 0}
            cta="Ver pedidos"
          />
        </div>
      )}

      <section className="card p-5">
        <h2 className="font-semibold text-slate-900">Flujo sugerido para la demo</h2>
        <ol className="mt-3 text-sm text-slate-600 list-decimal list-inside space-y-1">
          <li>
            Abre <Link className="text-brand-600 underline" to="/products">Productos</Link> y comprueba el stock disponible.
          </li>
          <li>
            Crea un pedido desde <Link className="text-brand-600 underline" to="/orders/new">Nuevo pedido</Link>.
          </li>
          <li>Observa cómo avanza el Saga en la vista de detalle del pedido.</li>
          <li>Confirma o cancela el pedido y comprueba la actualización en tiempo (casi) real.</li>
        </ol>
      </section>
    </div>
  );
}

interface KpiCardProps {
  to: string;
  label: string;
  value: number;
  cta: string;
}

function KpiCard({ to, label, value, cta }: KpiCardProps) {
  return (
    <Link to={to} className="card p-5 hover:shadow-md transition-shadow group">
      <div className="text-sm text-slate-500">{label}</div>
      <div className="mt-2 text-3xl font-bold text-slate-900">{value}</div>
      <div className="mt-3 text-brand-600 text-sm font-medium group-hover:underline">{cta} →</div>
    </Link>
  );
}
