import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { listProducts } from '../api/products';
import { createOrder } from '../api/orders';
import type {
  CreateOrderItemRequest,
  CreateOrderRequest,
  ProductSummaryResponse,
} from '../api/types';
import Money from '../components/Money';
import Spinner from '../components/Spinner';
import ErrorBanner from '../components/ErrorBanner';

interface Line {
  productId: string;
  quantity: number;
}

const DEMO_CUSTOMER_ID = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

export default function CreateOrderPage() {
  const navigate = useNavigate();
  const qc = useQueryClient();

  const productsQ = useQuery({
    queryKey: ['products', { page: 1, pageSize: 50 }],
    queryFn: ({ signal }) => listProducts({ page: 1, pageSize: 50 }, signal),
  });

  const [customerEmail, setCustomerEmail] = useState('demo@orderflow.local');
  const [street, setStreet] = useState('Calle Mayor 1');
  const [city, setCity] = useState('Madrid');
  const [zipCode, setZipCode] = useState('28001');
  const [country, setCountry] = useState('ES');
  const [lines, setLines] = useState<Line[]>([]);

  const productMap = useMemo(() => {
    const map = new Map<string, ProductSummaryResponse>();
    productsQ.data?.items.forEach((p) => map.set(p.id, p));
    return map;
  }, [productsQ.data]);

  const addAvailableProduct = () => {
    const available = productsQ.data?.items.find(
      (p) => p.isActive && p.stock > 0 && !lines.some((l) => l.productId === p.id),
    );
    if (available) {
      setLines((ls) => [...ls, { productId: available.id, quantity: 1 }]);
    }
  };

  const removeLine = (idx: number) => setLines((ls) => ls.filter((_, i) => i !== idx));

  const updateLine = (idx: number, patch: Partial<Line>) =>
    setLines((ls) => ls.map((l, i) => (i === idx ? { ...l, ...patch } : l)));

  const total = lines.reduce((acc, l) => {
    const p = productMap.get(l.productId);
    return p ? acc + p.price * l.quantity : acc;
  }, 0);

  const createMut = useMutation({
    mutationFn: (body: CreateOrderRequest) => createOrder(body),
    onSuccess: (order) => {
      qc.invalidateQueries({ queryKey: ['orders'] });
      navigate(`/orders/${order.id}`);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (lines.length === 0) return;

    const items: CreateOrderItemRequest[] = lines
      .map((l) => {
        const p = productMap.get(l.productId);
        if (!p) return null;
        return {
          productId: p.id,
          productName: p.name,
          quantity: l.quantity,
          unitPrice: p.price,
          currency: p.currency || 'EUR',
        };
      })
      .filter((x): x is CreateOrderItemRequest => x !== null);

    const body: CreateOrderRequest = {
      customerId: DEMO_CUSTOMER_ID,
      customerEmail,
      items,
      shippingAddress: { street, city, zipCode, country },
    };

    createMut.mutate(body);
  };

  if (productsQ.isLoading) return <Spinner label="Cargando catálogo…" />;
  if (productsQ.error) return <ErrorBanner error={productsQ.error} onRetry={() => productsQ.refetch()} />;

  const currency = lines[0] ? productMap.get(lines[0].productId)?.currency ?? 'EUR' : 'EUR';
  const canSubmit = lines.length > 0 && !createMut.isPending;

  return (
    <form className="space-y-6" onSubmit={handleSubmit}>
      <div>
        <h1 className="text-2xl font-bold">Nuevo pedido</h1>
        <p className="text-sm text-slate-500">
          Se envía a <span className="font-mono">POST /api/orders</span>. Dispara el Saga distribuido: reserva de stock → pago → confirmación.
        </p>
      </div>

      <section className="card p-5 space-y-3">
        <h2 className="font-semibold text-slate-900">Cliente</h2>
        <div>
          <label className="label" htmlFor="email">Email</label>
          <input
            id="email"
            type="email"
            className="input"
            required
            value={customerEmail}
            onChange={(e) => setCustomerEmail(e.target.value)}
          />
        </div>
        <p className="text-xs text-slate-500">
          El backend está en modo demo (AllowAnonymous) y asigna un <span className="font-mono">customerId</span> por defecto.
        </p>
      </section>

      <section className="card p-5 space-y-3">
        <div className="flex items-center justify-between">
          <h2 className="font-semibold text-slate-900">Líneas del pedido</h2>
          <button
            type="button"
            className="btn-secondary"
            onClick={addAvailableProduct}
            disabled={!productsQ.data || lines.length >= (productsQ.data?.items.length ?? 0)}
          >
            + Añadir línea
          </button>
        </div>

        {lines.length === 0 && (
          <p className="text-sm text-slate-500 py-4 text-center">
            Añade al menos un producto para poder crear el pedido.
          </p>
        )}

        {lines.map((line, idx) => {
          const product = productMap.get(line.productId);
          const maxQty = product?.stock ?? 1;
          return (
            <div key={idx} className="grid grid-cols-12 gap-2 items-end">
              <div className="col-span-12 sm:col-span-7">
                <label className="label">Producto</label>
                <select
                  className="input"
                  value={line.productId}
                  onChange={(e) => updateLine(idx, { productId: e.target.value, quantity: 1 })}
                >
                  {productsQ.data?.items.map((p) => {
                    const takenByOther = lines.some((l, j) => j !== idx && l.productId === p.id);
                    return (
                      <option key={p.id} value={p.id} disabled={takenByOther || !p.isActive || p.stock === 0}>
                        {p.name} — {p.price} {p.currency} (stock {p.stock})
                      </option>
                    );
                  })}
                </select>
              </div>
              <div className="col-span-5 sm:col-span-2">
                <label className="label">Cantidad</label>
                <input
                  type="number"
                  className="input"
                  min={1}
                  max={Math.max(1, maxQty)}
                  value={line.quantity}
                  onChange={(e) =>
                    updateLine(idx, {
                      quantity: Math.max(1, Math.min(maxQty, Number(e.target.value) || 1)),
                    })
                  }
                />
              </div>
              <div className="col-span-5 sm:col-span-2 text-right">
                <div className="label">Subtotal</div>
                <div className="py-2 font-medium">
                  {product ? <Money amount={product.price * line.quantity} currency={product.currency} /> : '—'}
                </div>
              </div>
              <div className="col-span-2 sm:col-span-1">
                <button
                  type="button"
                  className="btn-secondary w-full"
                  onClick={() => removeLine(idx)}
                  aria-label="Quitar línea"
                >
                  ×
                </button>
              </div>
            </div>
          );
        })}

        {lines.length > 0 && (
          <div className="pt-3 border-t border-slate-100 flex justify-end items-baseline gap-2">
            <span className="text-slate-500">Total estimado:</span>
            <span className="text-xl font-bold">
              <Money amount={total} currency={currency} />
            </span>
          </div>
        )}
      </section>

      <section className="card p-5 space-y-3">
        <h2 className="font-semibold text-slate-900">Dirección de envío</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="sm:col-span-2">
            <label className="label" htmlFor="street">Calle</label>
            <input id="street" className="input" required maxLength={200} value={street} onChange={(e) => setStreet(e.target.value)} />
          </div>
          <div>
            <label className="label" htmlFor="zip">Código postal</label>
            <input id="zip" className="input" required maxLength={20} value={zipCode} onChange={(e) => setZipCode(e.target.value)} />
          </div>
          <div>
            <label className="label" htmlFor="city">Ciudad</label>
            <input id="city" className="input" required maxLength={100} value={city} onChange={(e) => setCity(e.target.value)} />
          </div>
          <div>
            <label className="label" htmlFor="country">País (ISO-2)</label>
            <input
              id="country"
              className="input uppercase"
              required
              minLength={2}
              maxLength={2}
              value={country}
              onChange={(e) => setCountry(e.target.value.toUpperCase())}
            />
          </div>
        </div>
      </section>

      {createMut.error && <ErrorBanner error={createMut.error} />}

      <div className="flex justify-end gap-2">
        <button type="button" className="btn-secondary" onClick={() => navigate(-1)}>Cancelar</button>
        <button type="submit" className="btn-primary" disabled={!canSubmit}>
          {createMut.isPending ? 'Creando…' : 'Crear pedido'}
        </button>
      </div>
    </form>
  );
}
