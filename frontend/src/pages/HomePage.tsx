import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { GraduationCap, BookOpen, Users, Calendar, MapPin, Laptop, ArrowRight, UserPlus, CalendarX } from 'lucide-react'
import Hero from '../components/Hero/Hero'
import { eventosService } from '../services/eventosService'
import { tiposEventoService } from '../services/tiposEventoService'
import type { Evento, TipoEvento } from '../types/models'

const badgeColor = (tipo: string) => {
  const map: Record<string, string> = {
    Congreso: 'bg-red-100 text-red-700',
    Curso: 'bg-blue-100 text-blue-700',
    Taller: 'bg-green-100 text-green-700',
    Jornada: 'bg-amber-100 text-amber-700',
    Webinar: 'bg-cyan-100 text-cyan-700',
  }
  return map[tipo] || 'bg-blue-600 text-white'
}

const HomePage = () => {
  const [proximos, setProximos] = useState<Evento[]>([])
  const [tiposEvento, setTiposEvento] = useState<TipoEvento[]>([])
  const [loadingEventos, setLoadingEventos] = useState(true)

  useEffect(() => {
    const load = async () => {
      try {
        const [evs, tipos] = await Promise.all([
          eventosService.getActivos(),
          tiposEventoService.getAll().catch(() => [] as TipoEvento[]),
        ])
        const hoy = new Date()
        const futuros = evs
          .filter(e => new Date(e.fechaInicio) >= hoy)
          .sort((a, b) => new Date(a.fechaInicio).getTime() - new Date(b.fechaInicio).getTime())
          .slice(0, 3)
        setProximos(futuros)
        setTiposEvento(tipos)
      } catch {
        setProximos([])
      } finally {
        setLoadingEventos(false)
      }
    }
    load()
  }, [])

  const tipoNombre = (tipoId: number) => tiposEvento.find(t => t.id === tipoId)?.nombre || 'Evento'

  return (
    <>
      <Hero />

      {/* Servicios destacados */}
      <section className="py-16 md:py-24 bg-white">
        <div className="max-w-7xl mx-auto px-4">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <span className="section-label">NUESTROS SERVICIOS</span>
            <h2 className="section-title">¿Qué hacemos?</h2>
            <p className="section-subtitle">
              Trabajamos para mejorar la calidad de vida de las personas con diabetes
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <div className="bg-gradient-to-b from-blue-50 to-white rounded-2xl border border-blue-100 hover:shadow-xl transition-all duration-300 h-full text-center p-4">
              <div className="p-6">
                <div
                  className="w-16 h-16 bg-blue-100 rounded-2xl inline-flex items-center justify-center mb-3"
                >
                  <GraduationCap className="w-8 h-8 text-blue-600" />
                </div>
                <h5 className="text-lg font-bold text-slate-800">Formación Continua</h5>
                <p className="text-slate-600">
                  Cursos, talleres y programas de actualización para profesionales
                  de la salud especializados en diabetes.
                </p>
              </div>
            </div>

            <div className="bg-gradient-to-b from-blue-50 to-white rounded-2xl border border-blue-100 hover:shadow-xl transition-all duration-300 h-full text-center p-4">
              <div className="p-6">
                <div
                  className="w-16 h-16 bg-blue-100 rounded-2xl inline-flex items-center justify-center mb-3"
                >
                  <BookOpen className="w-8 h-8 text-blue-600" />
                </div>
                <h5 className="text-lg font-bold text-slate-800">Investigación</h5>
                <p className="text-slate-600">
                  Promovemos y difundimos investigaciones científicas sobre
                  prevención, diagnóstico y tratamiento de la diabetes.
                </p>
              </div>
            </div>

            <div className="bg-gradient-to-b from-blue-50 to-white rounded-2xl border border-blue-100 hover:shadow-xl transition-all duration-300 h-full text-center p-4">
              <div className="p-6">
                <div
                  className="w-16 h-16 bg-blue-100 rounded-2xl inline-flex items-center justify-center mb-3"
                >
                  <Users className="w-8 h-8 text-blue-600" />
                </div>
                <h5 className="text-lg font-bold text-slate-800">Comunidad</h5>
                <p className="text-slate-600">
                  Red de profesionales comprometidos con la excelencia en el
                  cuidado de pacientes con diabetes.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Próximos eventos */}
      <section className="py-16 md:py-24 bg-slate-50">
        <div className="max-w-7xl mx-auto px-4">
          <div className="text-center max-w-3xl mx-auto mb-16">
            <span className="section-label">CALENDARIO</span>
            <h2 className="section-title">Próximos Eventos</h2>
          </div>

          {loadingEventos ? (
            <div className="text-center py-8">
              <div className="animate-spin w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full mx-auto"></div>
            </div>
          ) : proximos.length === 0 ? (
            <div className="text-center py-8 text-slate-500">
              <CalendarX className="mx-auto" size={48} />
              <p className="mt-3">No hay próximos eventos por el momento.</p>
            </div>
          ) : (
            <div className="flex flex-wrap justify-center gap-6">
              {proximos.map((evento) => {
                const tipo = tipoNombre(evento.tipoEventoId)
                const esVirtual = evento.modalidad?.toLowerCase() === 'virtual'
                return (
                  <div key={evento.id} className="w-full md:w-[calc(50%-0.75rem)] lg:w-[calc(33.333%-1rem)] bg-white rounded-2xl shadow-sm border border-slate-200 hover:shadow-xl transition-all duration-300">
                    <div className="p-6">
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium mb-2 ${badgeColor(tipo)}`}>{tipo}</span>
                      <h5 className="text-lg font-bold text-slate-800">{evento.titulo}</h5>
                      <p className="text-slate-600 text-sm">
                        <Calendar className="w-4 h-4 inline-block mr-1" />
                        {new Date(evento.fechaInicio).toLocaleDateString('es-AR', { day: 'numeric', month: 'long', year: 'numeric' })}
                      </p>
                      {(evento.lugar || esVirtual) && (
                        <p className="text-slate-600 text-sm">
                          {esVirtual
                            ? <><Laptop className="w-4 h-4 inline-block mr-1" /> Virtual</>
                            : <><MapPin className="w-4 h-4 inline-block mr-1" /> {evento.lugar}</>}
                        </p>
                      )}
                      {evento.descripcion && (
                        <p className="text-slate-600">{evento.descripcion}</p>
                      )}
                    </div>
                  </div>
                )
              })}
            </div>
          )}

          <div className="text-center mt-4">
            <Link to="/eventos" className="btn-primary px-4">
              Ver todos los eventos <ArrowRight className="w-4 h-4 inline-block ml-1" />
            </Link>
          </div>
        </div>
      </section>

      {/* CTA Inscripción */}
      <section className="py-16 md:py-24 text-white text-center bg-gradient-to-br from-slate-800 to-slate-900">
        <div className="max-w-7xl mx-auto px-4">
          <span className="text-blue-400 text-sm font-semibold tracking-wider uppercase mb-4 block">SUMATE</span>
          <h2 className="font-bold mb-3">Unite a la Sociedad Argentina de Diabetes</h2>
          <p className="text-lg text-slate-400 mb-4">
            Formá parte de la comunidad de profesionales que trabaja por mejorar
            la calidad de vida de las personas con diabetes.
          </p>
          <Link to="/eventos" className="btn-accent btn-lg px-6">
            <UserPlus className="w-5 h-5 inline-block mr-2" />Inscribirse ahora
          </Link>
        </div>
      </section>
    </>
  )
}

export default HomePage
