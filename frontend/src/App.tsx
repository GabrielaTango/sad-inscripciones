import { Routes, Route } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import Navbar from './components/Navbar/Navbar'
import Footer from './components/Footer/Footer'
import ProtectedRoute from './components/Admin/ProtectedRoute'
import AdminLayout from './components/Admin/AdminLayout'
import CapituloRoute from './components/Capitulo/CapituloRoute'
import CapituloLayout from './components/Capitulo/CapituloLayout'
import CobrosPage from './pages/capitulo/CobrosPage'
import MisCobrosPage from './pages/capitulo/MisCobrosPage'
import HomePage from './pages/HomePage'
import NosotrosPage from './pages/NosotrosPage'
import EventosPage from './pages/EventosPage'
import InscripcionPage from './pages/InscripcionPage'
import ContactoPage from './pages/ContactoPage'
import LoginPage from './pages/LoginPage'
import PagoResultadoPage from './pages/PagoResultadoPage'
import MisInscripcionesPage from './pages/MisInscripcionesPage'
import ResumenCuentaPage from './pages/ResumenCuentaPage'
import CuponPagoFacilPage from './pages/CuponPagoFacilPage'
import TiposEventoPage from './pages/admin/TiposEventoPage'
import TiposAlumnoPage from './pages/admin/TiposAlumnoPage'
import EventosAdminPage from './pages/admin/EventosAdminPage'
import EventoDetallePage from './pages/admin/EventoDetallePage'
import InscripcionesAdminPage from './pages/admin/InscripcionesAdminPage'
import PagosAdminPage from './pages/admin/PagosAdminPage'
import BecaEventosAdminPage from './pages/admin/BecaEventosAdminPage'
import BecaCodigosPage from './pages/admin/BecaCodigosPage'
import UsuariosAdminPage from './pages/admin/UsuariosAdminPage'
import PromocionesAdminPage from './pages/admin/PromocionesAdminPage'
import PromocionCuponesPage from './pages/admin/PromocionCuponesPage'
import DashboardPage from './pages/admin/DashboardPage'
import PagosCuentaCorrienteAdminPage from './pages/admin/PagosCuentaCorrienteAdminPage'
import ConfiguracionEmailPage from './pages/admin/ConfiguracionEmailPage'
import ConfiguracionMercadoPagoPage from './pages/admin/ConfiguracionMercadoPagoPage'
import EmailTemplatesListPage from './pages/admin/EmailTemplatesListPage'
import EmailTemplateEditorPage from './pages/admin/EmailTemplateEditorPage'

function App() {
  return (
    <AuthProvider>
      <div className="flex flex-col min-h-screen">
        <Navbar />
        <main className="flex-grow">
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/nosotros" element={<NosotrosPage />} />
            <Route path="/eventos" element={<EventosPage />} />
            <Route path="/inscripcion/:eventoId" element={<InscripcionPage />} />
            <Route path="/contacto" element={<ContactoPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/mis-inscripciones" element={<MisInscripcionesPage />} />
            <Route path="/resumen-cuenta" element={<ResumenCuentaPage />} />
            <Route path="/resumen-cuenta/cupon-pagofacil/:id" element={<CuponPagoFacilPage />} />
            <Route path="/pago/resultado" element={<PagoResultadoPage />} />

            <Route path="/capitulo" element={<CapituloRoute><CapituloLayout /></CapituloRoute>}>
              <Route index element={<CobrosPage />} />
              <Route path="historial" element={<MisCobrosPage />} />
            </Route>

            <Route path="/admin" element={<ProtectedRoute><AdminLayout /></ProtectedRoute>}>
              <Route index element={<DashboardPage />} />
              <Route path="tipos-evento" element={<TiposEventoPage />} />
              <Route path="tipos-alumno" element={<TiposAlumnoPage />} />
              <Route path="eventos" element={<EventosAdminPage />} />
              <Route path="eventos/nuevo" element={<EventoDetallePage />} />
              <Route path="eventos/:id" element={<EventoDetallePage />} />
              <Route path="inscripciones" element={<InscripcionesAdminPage />} />
              <Route path="pagos" element={<PagosAdminPage />} />
              <Route path="becas" element={<BecaEventosAdminPage />} />
              <Route path="becas/:becaEventoId/codigos" element={<BecaCodigosPage />} />
              <Route path="promociones" element={<PromocionesAdminPage />} />
              <Route path="promociones/:promocionId/cupones" element={<PromocionCuponesPage />} />
              <Route path="usuarios" element={<UsuariosAdminPage />} />
              <Route path="pagos-cuenta-corriente" element={<PagosCuentaCorrienteAdminPage />} />
              <Route path="configuracion-email" element={<ConfiguracionEmailPage />} />
              <Route path="configuracion-mercadopago" element={<ConfiguracionMercadoPagoPage />} />
              <Route path="email-templates" element={<EmailTemplatesListPage />} />
              <Route path="email-templates/:codigo" element={<EmailTemplateEditorPage />} />
            </Route>
          </Routes>
        </main>
        <Footer />
      </div>
    </AuthProvider>
  )
}

export default App
