import { NavLink, Outlet } from 'react-router-dom';

function navClass({ isActive }: { isActive: boolean }) {
  const base = 'px-3 py-2 rounded-md text-sm font-medium transition-colors';
  return isActive
    ? `${base} bg-brand-600 text-white`
    : `${base} text-slate-600 hover:bg-slate-100 hover:text-slate-900`;
}

export default function App() {
  return (
    <div className="min-h-screen flex flex-col">
      <header className="bg-white border-b border-slate-200 sticky top-0 z-10">
        <div className="max-w-6xl mx-auto px-4 h-14 flex items-center justify-between">
          <NavLink to="/" className="flex items-center gap-2 font-bold text-slate-900">
            <span className="inline-block w-6 h-6 rounded bg-brand-600 text-white text-xs flex items-center justify-center">OF</span>
            OrderFlow <span className="text-slate-400 font-normal hidden sm:inline">· Demo</span>
          </NavLink>
          <nav className="flex items-center gap-1">
            <NavLink to="/" end className={navClass}>Inicio</NavLink>
            <NavLink to="/products" className={navClass}>Productos</NavLink>
            <NavLink to="/orders" className={navClass}>Pedidos</NavLink>
            <NavLink to="/orders/new" className={navClass}>Nuevo pedido</NavLink>
          </nav>
        </div>
      </header>

      <main className="flex-1 max-w-6xl w-full mx-auto px-4 py-8">
        <Outlet />
      </main>

      <footer className="border-t border-slate-200 py-4 text-center text-xs text-slate-500">
        OrderFlow · demo SPA · React + Vite + TS
      </footer>
    </div>
  );
}
