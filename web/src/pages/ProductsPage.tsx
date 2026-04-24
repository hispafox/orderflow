import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { listProducts, updateProductStock } from '../api/products';
import Money from '../components/Money';
import Spinner from '../components/Spinner';
import ErrorBanner from '../components/ErrorBanner';

const PAGE_SIZE = 20;

export default function ProductsPage() {
  const [page, setPage] = useState(1);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingValue, setEditingValue] = useState<number>(0);
  const qc = useQueryClient();

  const query = useQuery({
    queryKey: ['products', { page, pageSize: PAGE_SIZE }],
    queryFn: ({ signal }) => listProducts({ page, pageSize: PAGE_SIZE }, signal),
    placeholderData: (prev) => prev,
  });

  const stockMutation = useMutation({
    mutationFn: ({ id, newStock }: { id: string; newStock: number }) =>
      updateProductStock(id, newStock),
    onSuccess: () => {
      setEditingId(null);
      qc.invalidateQueries({ queryKey: ['products'] });
    },
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

      {stockMutation.error && <ErrorBanner error={stockMutation.error} />}

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
                  <th className="px-4 py-3 text-right">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {query.data.items.map((p) => {
                  const isEditing = editingId === p.id;
                  return (
                    <tr key={p.id} className="hover:bg-slate-50">
                      <td className="px-4 py-3">
                        <div className="font-medium text-slate-900">{p.name}</div>
                        <div className="text-xs text-slate-400 font-mono">{p.id}</div>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <Money amount={p.price} currency={p.currency} />
                      </td>
                      <td className="px-4 py-3 text-right">
                        {isEditing ? (
                          <input
                            type="number"
                            className="input w-24 text-right"
                            min={0}
                            max={100_000}
                            value={editingValue}
                            onChange={(e) => setEditingValue(Number(e.target.value) || 0)}
                            autoFocus
                          />
                        ) : (
                          <span className={p.stock === 0 ? 'text-red-600 font-medium' : ''}>
                            {p.stock}
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-3">
                        {p.isActive ? (
                          <span className="text-emerald-700">Activo</span>
                        ) : (
                          <span className="text-slate-500">Inactivo</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-right">
                        {isEditing ? (
                          <div className="flex gap-1 justify-end">
                            <button
                              type="button"
                              className="btn-primary text-xs py-1 px-2"
                              onClick={() =>
                                stockMutation.mutate({ id: p.id, newStock: editingValue })
                              }
                              disabled={stockMutation.isPending}
                            >
                              {stockMutation.isPending ? '…' : 'Guardar'}
                            </button>
                            <button
                              type="button"
                              className="btn-secondary text-xs py-1 px-2"
                              onClick={() => setEditingId(null)}
                            >
                              ×
                            </button>
                          </div>
                        ) : (
                          <div className="flex gap-1 justify-end">
                            <button
                              type="button"
                              className="btn-secondary text-xs py-1 px-2"
                              onClick={() => {
                                setEditingId(p.id);
                                setEditingValue(p.stock + 100);
                              }}
                              title="Fijar un nuevo stock absoluto (+100 por defecto)"
                            >
                              Reponer stock
                            </button>
                          </div>
                        )}
                      </td>
                    </tr>
                  );
                })}
                {query.data.items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-10 text-center text-slate-500 text-sm">
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
