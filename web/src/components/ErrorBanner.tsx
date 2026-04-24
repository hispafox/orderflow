import { ApiError } from '../api/client';

interface ErrorBannerProps {
  error: unknown;
  onRetry?: () => void;
}

function describe(error: unknown): { title: string; detail?: string; correlationId?: string | null } {
  if (error instanceof ApiError) {
    return {
      title: `${error.status} · ${error.message}`,
      detail: typeof error.body === 'string' ? error.body : undefined,
      correlationId: error.correlationId,
    };
  }
  if (error instanceof Error) {
    return { title: 'Error de red', detail: error.message };
  }
  return { title: 'Error desconocido' };
}

export default function ErrorBanner({ error, onRetry }: ErrorBannerProps) {
  const { title, detail, correlationId } = describe(error);
  return (
    <div className="card border-red-200 bg-red-50 p-4 flex items-start gap-3">
      <span className="text-red-600 text-lg leading-none">!</span>
      <div className="flex-1 text-sm">
        <div className="font-semibold text-red-800">{title}</div>
        {detail && <div className="text-red-700 mt-1">{detail}</div>}
        {correlationId && (
          <div className="text-red-600 mt-1 text-xs font-mono">correlation: {correlationId}</div>
        )}
      </div>
      {onRetry && (
        <button type="button" onClick={onRetry} className="btn-secondary">
          Reintentar
        </button>
      )}
    </div>
  );
}
