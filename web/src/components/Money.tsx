interface MoneyProps {
  amount: number;
  currency: string;
  className?: string;
}

export default function Money({ amount, currency, className }: MoneyProps) {
  const formatted = new Intl.NumberFormat('es-ES', {
    style: 'currency',
    currency: currency || 'EUR',
    minimumFractionDigits: 2,
  }).format(amount);

  return <span className={className}>{formatted}</span>;
}
