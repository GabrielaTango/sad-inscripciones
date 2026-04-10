import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { Calendar, MapPin, Laptop, PenSquare, Lock, CalendarX } from 'lucide-react'
import { eventosService } from '../services/eventosService'
import { tiposEventoService } from '../services/tiposEventoService'
import { inscripcionesService } from '../services/inscripcionesService'
import { authService } from '../services/authService'
import { useAuth } from '../context/AuthContext'
import type { Evento, TipoEvento } from '../types/models'

const badgeColor = (tipo: string) => {
  const map: Record<string, string> = {
    Congreso: 'bg-red-100 text-red-700',
    Curso: 'bg-blue-100 text-blue-700',
    Taller: 'bg-green-100 text-green-700',
    Jornada: 'bg-amber-100 text-amber-700',
    Webinar: 'bg-cyan-100 text-cyan-700',
  }
  return map[tipo] || 'bg-gray-100 text-gray-700'
}

const EventosPage = () => {
  const { isAuthenticated } = useAuth()
  const [eventos, setEventos] = useState<Evento[]>([])
  const [tiposEvento, setTiposEvento] = useState<TipoEvento[]>([])
  const [eventosInscripto, setEventosInscripto] = useState<Set<number>>(new Set())
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const load = async () => {
      try {
        const [evs, tipos] = await Promise.all([
          eventosService.getActivos(),
          tiposEventoService.getAll().catch(() => [] as TipoEvento[]),
        ])
        setEventos(evs)
        setTiposEvento(tipos)

        if (isAuthenticated) {
          try {
            const socioData = await authService.getSocioData()
            if (socioData.documento) {
              const pendientes = await inscripcionesService.getPendientes(socioData.documento)
              setEventosInscripto(new Set(pendientes.map(p => p.eventoId)))
            }
          } catch { /* ignore */ }
        }
      } catch {
        setEventos([])
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [isAuthenticated])

  const tipoNombre = (tipoId: number) => tiposEvento.find(t => t.id === tipoId)?.nombre || 'Evento'

  return (
    <>
      <section className="page-header">
        <div className="max-w-7xl mx-auto px-4">
          <h1 className="font-bold text-3xl">Eventos</h1>
          <p className="text-lg text-white/90 mb-0">
            Congresos, cursos, talleres y jornadas de actualizacion profesional
          </p>
        </div>
      </section>

      <section className="py-16 md:py-24">
        <div className="max-w-7xl mx-auto px-4">
          {loading ? (
            <div className="text-center py-16">
              <div className="animate-spin w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full mx-auto"></div>
            </div>
          ) : eventos.length === 0 ? (
            <div className="text-center py-16 text-slate-500">
              <CalendarX className="mx-auto" size={48} />
              <p className="mt-3">No hay eventos disponibles en este momento.</p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {eventos.filter(e => !eventosInscripto.has(e.id)).map((evento) => {
                const tipo = tipoNombre(evento.tipoEventoId)
                const fechaCierre = new Date(evento.fechaCierreInscripcion)
                const inscripcionAbierta = fechaCierre > new Date()
                return (
                  <div key={evento.id}>
                    <div className="bg-white rounded-2xl shadow-sm border border-slate-200 hover:shadow-xl transition-all duration-300 h-full">
                      <div className="p-6 flex flex-col h-full">
                        <div className="flex justify-between items-start mb-2">
                          <span className={`inline-block px-2 py-1 rounded text-xs font-medium ${badgeColor(tipo)}`}>{tipo}</span>
                          <span className="inline-flex items-center px-2 py-1 rounded text-xs font-medium bg-slate-100 text-slate-700">
                            <Laptop className="mr-1" size={14} />{evento.modalidad}
                          </span>
                        </div>
                        <h5 className="font-bold text-lg text-slate-800 mb-1">{evento.titulo}</h5>
                        <p className="text-slate-500 text-sm mb-1 flex items-center">
                          <Calendar className="mr-1" size={14} />
                          {new Date(evento.fechaInicio).toLocaleDateString('es-AR', { day: 'numeric', month: 'long', year: 'numeric' })}
                        </p>
                        {evento.lugar && (
                          <p className="text-slate-500 text-sm mb-3 flex items-center">
                            <MapPin className="mr-1" size={14} />{evento.lugar}
                          </p>
                        )}
                        <p className="flex-grow text-slate-600">{evento.descripcion}</p>
                        {inscripcionAbierta ? (
                          evento.soloSocios && !isAuthenticated ? (
                            <span className="inline-flex items-center px-2 py-1 rounded text-xs font-medium bg-amber-100 text-amber-700 mt-2 self-start">
                              <Lock className="mr-1" size={14} />Solo para socios
                            </span>
                          ) : (
                            <Link to={`/inscripcion/${evento.id}`} className="btn-primary btn-sm mt-2 self-start inline-flex items-center">
                              <PenSquare className="mr-1" size={14} />Inscribirse
                            </Link>
                          )
                        ) : (
                          <span className="inline-block px-2 py-1 rounded text-xs font-medium bg-slate-100 text-slate-700 mt-2 self-start">Inscripcion cerrada</span>
                        )}
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </div>
      </section>
    </>
  )
}

export default EventosPage
