import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { listProducts } from '../api/products';
import Money from '../components/Money';
import Spinner from '../components/Spinner';
import ErrorBanner from '../components/ErrorBanner';

const PAGE_SIZE = 20;

export default function ProductsPage() {
  const [page, setPage] = useState(1);

  const query = useQuery({
    queryKey: ['products', { page, pageSize: PAGE_SIZE }],
    queryFn: ({ signal }) => listProducts({ page, pageSize: PAGE_SIZE }, signal),
    placeholderData: (prev) => prev,
  });

  return (
    <div className="space-y-4">
      <div className="flex items-baseline justify-between">
        <div>
          <h1 className="text-2xl font-bold">Catálogo</h1>
          <p className="text-sm text-slate-500">
            Datos obtenidos desde <span className="font-mono">Products.API</span> (proxy Vite → https://localhost:7200).
          </p>
        </div>
        {query.data && (
          <span className="text-sm text-slate-500">
            {query.data.totalCount} producto{query.data.totalCount === 1 ? '' : 's'}
          </span>
        )}
      </div>

      {query.isLoading && <Spinner label="Cargando productos…" />}
      {query.error && !query.isLoading && (
        <ErrorBanner error={query.error} onRetry={() => query.refetch()} />
      )}

      {query.data && (
        <>
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-slate-600 text-left text-xs uppercase">
                <tr>
                  <th className="px-4 py-3">Producto</th>
                  <th className="px-4 py-3 text-right">Precio</th>
                  <th className="px-4 py-3 text-right">Stock</th>
                  <th className="px-4 py-3">Estado</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {query.data.items.map((p) => (
                  <tr key={p.id} className="hover:bg-slate-50">
                    <td className="px-4 py-3">
                      <div className="font-medium text-slate-900">{p.name}</div>
                      <div className="text-xs text-slate-400 font-mono">{p.id}</div>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <Money amount={p.price} currency={p.currency} />
                    </td>
                    <td className="px-4 py-3 text-right">
                      <span className={p.stock === 0 ? 'text-red-600 font-medium' : ''}>
                        {p.stock}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      {p.isActive ? (
                        <span className="text-emerald-700">Activo</span>
                      ) : (
                        <span className="text-slate-500">Inactivo</span>
                      )}
                    </td>
                  </tr>
                ))}
                {query.data.items.length === 0 && (
                  <tr>
                    <td colSpan={4} className="px-4 py-10 text-center text-slate-500 text-sm">
                      No hay productos en el catálogo.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between text-sm">
            <span className="text-slate-500">
              Página {query.data.page} de {query.data.totalPages || 1}
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
