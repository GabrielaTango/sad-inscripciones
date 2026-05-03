import { NavLink, Outlet } from 'react-router-dom'
import { CreditCard, Receipt } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

const menuItems: { path: string; label: string; icon: LucideIcon; end?: boolean }[] = [
  { path: '/capitulo', label: 'Cobros', icon: CreditCard, end: true },
  { path: '/capitulo/historial', label: 'Mis cobros', icon: Receipt },
]

const CapituloLayout = () => {
  return (
    <div className="flex min-h-[calc(100vh-64px)]">
      <nav className="hidden md:block w-56 lg:w-64 bg-slate-900 shrink-0 py-4">
        <div className="px-4 mb-3">
          <h6 className="text-slate-400 uppercase text-xs font-bold tracking-wider">Capítulo</h6>
        </div>
        <ul className="space-y-1">
          {menuItems.map((item) => (
            <li key={item.path}>
              <NavLink
                to={item.path}
                end={item.end}
                className={({ isActive }) =>
                  `flex items-center gap-2 px-4 py-2 text-sm transition-colors ${isActive ? 'bg-blue-600 text-white font-bold' : 'text-slate-400 hover:text-white hover:bg-white/10'}`
                }
              >
                <item.icon className="w-4 h-4" />
                {item.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      <main className="flex-1 p-4 lg:p-6 overflow-auto">
        <Outlet />
      </main>
    </div>
  )
}

export default CapituloLayout
