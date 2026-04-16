import { useState } from 'react'
import { Link, NavLink, useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import { UserCheck, Settings, LogOut, UserPlus, ChevronDown } from 'lucide-react'
import logo from '../../assets/logo70.png'

const Navbar = () => {
  const { isAuthenticated, isAdmin, cuit, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [mobileOpen, setMobileOpen] = useState(false)
  const [dropdownOpen, setDropdownOpen] = useState(false)

  const isAdminRoute = location.pathname.startsWith('/admin')

  const handleLogout = () => {
    logout()
    navigate('/')
  }

  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    `text-slate-600 hover:text-blue-600 transition-colors font-medium ${isActive ? 'text-blue-600' : ''}`

  return (
    <header className="bg-white shadow-sm sticky top-0 z-50">
      <nav className={isAdminRoute ? 'px-4 sm:px-6' : 'max-w-7xl mx-auto px-4 sm:px-6 lg:px-8'}>
        <div className="flex justify-between items-center h-16 md:h-20">
          {/* Logo */}
          <Link to="/" className="flex items-center space-x-3">
            <div className="flex items-center justify-center">
              <img src={logo} alt="SAD Logo" className="h-12 md:h-16 w-auto object-contain" />
            </div>
            {/*
            <div className="hidden sm:block">
              <h1 className="text-lg md:text-xl font-bold text-slate-800">Sociedad de Diabetes</h1>
              <p className="text-xs text-slate-500">Educación médica continua</p>
            </div>
            */}
          </Link>

          {/* Desktop Navigation */}
          <div className="hidden lg:flex items-center space-x-8">
            <NavLink className={navLinkClass} to="/">Inicio</NavLink>
            <NavLink className={navLinkClass} to="/nosotros">Nosotros</NavLink>
            <NavLink className={navLinkClass} to="/eventos">Eventos</NavLink>
            <NavLink className={navLinkClass} to="/mis-inscripciones">Inscripciones</NavLink>
            {isAuthenticated && (
              <NavLink className={navLinkClass} to="/resumen-cuenta">Cuenta Corriente</NavLink>
            )}
            <NavLink className={navLinkClass} to="/contacto">Contacto</NavLink>

            {isAuthenticated ? (
              <div className="relative">
                <button
                  onClick={() => setDropdownOpen(!dropdownOpen)}
                  className="flex items-center gap-2 px-4 py-2 text-sm border border-slate-200 text-slate-700 rounded-lg hover:bg-slate-50 transition-colors font-medium"
                >
                  <UserCheck className="w-4 h-4" />
                  {cuit}
                  <ChevronDown className={`w-3 h-3 transition-transform ${dropdownOpen ? 'rotate-180' : ''}`} />
                </button>
                {dropdownOpen && (
                  <div className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-50">
                    {isAdmin && (
                      <Link
                        to="/admin/eventos"
                        className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                        onClick={() => setDropdownOpen(false)}
                      >
                        <Settings className="w-4 h-4" />Admin
                      </Link>
                    )}
                    <button
                      className="flex items-center gap-2 w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                      onClick={() => { setDropdownOpen(false); handleLogout() }}
                    >
                      <LogOut className="w-4 h-4" />Salir
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <Link to="/login" className="bg-blue-600 text-white px-5 py-2 rounded-lg hover:bg-blue-700 transition-colors font-medium">
                <UserPlus className="w-4 h-4 inline mr-1" /> Socios
              </Link>
            )}
          </div>

          {/* Mobile menu button */}
          <button
            onClick={() => setMobileOpen(!mobileOpen)}
            className="lg:hidden p-2 rounded-lg hover:bg-slate-100"
          >
            <ChevronDown className={`w-6 h-6 text-slate-600 transition-transform ${mobileOpen ? 'rotate-180' : ''}`} />
          </button>
        </div>

        {/* Mobile Navigation */}
        {mobileOpen && (
          <div className="lg:hidden py-4 border-t border-slate-100">
            <div className="flex flex-col space-y-3">
              <NavLink className={navLinkClass} to="/" onClick={() => setMobileOpen(false)}>Inicio</NavLink>
              <NavLink className={navLinkClass} to="/nosotros" onClick={() => setMobileOpen(false)}>Nosotros</NavLink>
              <NavLink className={navLinkClass} to="/eventos" onClick={() => setMobileOpen(false)}>Eventos</NavLink>
              <NavLink className={navLinkClass} to="/mis-inscripciones" onClick={() => setMobileOpen(false)}>Inscripciones</NavLink>
              {isAuthenticated && (
                <NavLink className={navLinkClass} to="/resumen-cuenta" onClick={() => setMobileOpen(false)}>Cuenta Corriente</NavLink>
              )}
              <NavLink className={navLinkClass} to="/contacto" onClick={() => setMobileOpen(false)}>Contacto</NavLink>

              {isAuthenticated ? (
                <div className="pt-2 space-y-2 border-t border-slate-100 mt-2">
                  {isAdmin && (
                    <Link to="/admin/eventos" className="block text-slate-600 hover:text-blue-600 transition-colors font-medium py-2" onClick={() => setMobileOpen(false)}>
                      <Settings className="w-4 h-4 inline mr-2" />Admin
                    </Link>
                  )}
                  <button className="block text-slate-600 hover:text-blue-600 transition-colors font-medium py-2" onClick={() => { setMobileOpen(false); handleLogout() }}>
                    <LogOut className="w-4 h-4 inline mr-2" />Salir
                  </button>
                </div>
              ) : (
                <Link to="/login" className="bg-blue-600 text-white px-5 py-2 rounded-lg hover:bg-blue-700 transition-colors font-medium text-center" onClick={() => setMobileOpen(false)}>
                  <UserPlus className="w-4 h-4 inline mr-1" /> Socios
                </Link>
              )}
            </div>
          </div>
        )}
      </nav>
    </header>
  )
}

export default Navbar
