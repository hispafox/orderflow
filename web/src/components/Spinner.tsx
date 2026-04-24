interface SpinnerProps {
  label?: string;
}

export default function Spinner({ label = 'Cargando…' }: SpinnerProps) {
  return (
    <div className="flex items-center justify-center gap-3 py-12 text-slate-500">
      <span className="inline-block w-5 h-5 border-2 border-slate-300 border-t-brand-600 rounded-full animate-spin" />
      <span className="text-sm">{label}</span>
    </div>
  );
}
