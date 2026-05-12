import { NavLink, Outlet } from 'react-router-dom'
import { LayoutDashboard, Bookmark, Users, CalendarDays, UserCheck, CreditCard, Award, Gift, UserCog, Receipt, Mail, FileText, Wallet } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

const menuItems: { path: string; label: string; icon: LucideIcon; end?: boolean }[] = [
  { path: '/admin', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { path: '/admin/tipos-evento', label: 'Tipos de Evento', icon: Bookmark },
  { path: '/admin/tipos-alumno', label: 'Tipos de Alumno', icon: Users },
  { path: '/admin/eventos', label: 'Eventos', icon: CalendarDays },
  { path: '/admin/inscripciones', label: 'Inscripciones', icon: UserCheck },
  { path: '/admin/pagos', label: 'Pagos', icon: CreditCard },
  { path: '/admin/becas', label: 'Becas', icon: Award },
  { path: '/admin/promociones', label: 'Promociones', icon: Gift },
  { path: '/admin/usuarios', label: 'Usuarios', icon: UserCog },
  { path: '/admin/pagos-cuenta-corriente', label: 'Pagos Cta. Cte.', icon: Receipt },
  { path: '/admin/configuracion-email', label: 'Config. Email', icon: Mail },
  { path: '/admin/configuracion-mercadopago', label: 'Config. MercadoPago', icon: Wallet },
  { path: '/admin/email-templates', label: 'Templates Email', icon: FileText },
]

const AdminLayout = () => {
  return (
    <div className="flex min-h-[calc(100vh-64px)]">
      <nav className="hidden md:block w-56 lg:w-64 bg-slate-900 shrink-0 py-4">
        <div className="px-4 mb-3">
          <h6 className="text-slate-400 uppercase text-xs font-bold tracking-wider">Administración</h6>
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

export default AdminLayout
