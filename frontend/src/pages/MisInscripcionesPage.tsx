import { useState, useEffect, useCallback } from 'react'
import { useAuth } from '../context/AuthContext'
import { inscripcionesService } from '../services/inscripcionesService'
import type { InscripcionPendiente } from '../services/inscripcionesService'
import { eventosService } from '../services/eventosService'
import type { Evento } from '../types/models'
import { Search, Info, Calendar, MapPin, User, CreditCard } from 'lucide-react'

const MisInscripcionesPage = () => {
  const { isAuthenticated, cuit } = useAuth()
  const [documento, setDocumento] = useState('')
  const [eventoId, setEventoId] = useState<number | undefined>(undefined)
  const [eventosActivos, setEventosActivos] = useState<Evento[]>([])
  const [inscripciones, setInscripciones] = useState<InscripcionPendiente[]>([])
  const [loading, setLoading] = useState(false)
  const [searched, setSearched] = useState(false)
  const [error, setError] = useState('')
  const [payingId, setPayingId] = useState<number | null>(null)

  // Load active events for the select (only when not logged in)
  useEffect(() => {
    if (!isAuthenticated) {
      eventosService.getActivos().then(setEventosActivos).catch(() => {})
    }
  }, [isAuthenticated])

  // Auto-search when logged in
  const searchByDoc = useCallback(async (doc: string, evId?: number) => {
    setLoading(true); setError(''); setSearched(true)
    try {
      const result = await inscripcionesService.getPendientes(doc, evId)
      setInscripciones(result)
    } catch (err) { setError(err instanceof Error ? err.message : 'Error al buscar inscripciones') }
    finally { setLoading(false) }
  }, [])

  useEffect(() => {
    if (isAuthenticated && cuit) {
      inscripcionesService.validarPendientes()
        .catch(() => { /* no bloquear si falla la validación */ })
        .finally(() => searchByDoc(cuit))
    }
  }, [isAuthenticated, cuit, searchByDoc])

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    if (!documento.trim()) return
    searchByDoc(documento.trim(), eventoId)
  }

  const formatDate = (d: string) => d ? new Date(d).toLocaleDateString('es-AR') : '-'
  const formatCurrency = (n: number) => `$${n.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`

  return (
    <div className="max-w-7xl mx-auto px-4 py-16 md:py-24">
      <h2 className="font-bold text-slate-800 mb-4">Mis Inscripciones</h2>

      {/* Search form for non-authenticated users */}
      {!isAuthenticated && (
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm mb-4">
          <div className="p-4">
            <form onSubmit={handleSearch}>
              <div className="grid grid-cols-1 md:grid-cols-12 gap-4 items-end">
                <div className="md:col-span-4">
                  <label className="form-label">Evento</label>
                  <select className="form-select" value={eventoId ?? ''} onChange={e => setEventoId(e.target.value ? Number(e.target.value) : undefined)}>
                    <option value="">Todos los eventos</option>
                    {eventosActivos.map(ev => <option key={ev.id} value={ev.id}>{ev.titulo}</option>)}
                  </select>
                </div>
                <div className="md:col-span-4">
                  <label className="form-label">Nro de Documento *</label>
                  <input
                    type="text"
                    className="form-input"
                    placeholder="Ingrese su DNI"
                    value={documento}
                    onChange={e => setDocumento(e.target.value)}
                    required
                  />
                </div>
                <div className="md:col-span-4">
                  <button type="submit" className="btn-primary w-full" disabled={loading}>
                    {loading ? <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-1"></span>Buscando...</> : <><Search className="inline w-4 h-4 mr-1" />Buscar</>}
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Loading for authenticated */}
      {isAuthenticated && loading && (
        <div className="text-center py-4"><div className="animate-spin w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full"></div></div>
      )}

      {error && <div className="alert-danger">{error}</div>}

      {/* Results */}
      {searched && !loading && (
        <>
          {inscripciones.length === 0 ? (
            <div className="alert-info">
              <Info className="inline w-4 h-4 mr-2" />
              No se encontraron inscripciones.
            </div>
          ) : (
            <div className="space-y-4">
              {[...inscripciones]
                .sort((a, b) => {
                  const orden: Record<string, number> = { Pendiente: 0, Reservada: 1, Confirmada: 2 }
                  const diff = (orden[a.estado] ?? 2) - (orden[b.estado] ?? 2)
                  if (diff !== 0) return diff
                  return new Date(b.fechaInscripcion).getTime() - new Date(a.fechaInscripcion).getTime()
                })
                .map(insc => (
                <div key={insc.id}>
                  <div className="bg-white rounded-2xl border border-slate-200 shadow-sm hover:shadow-xl transition-all duration-300">
                    <div className="p-4">
                      <div className="grid grid-cols-1 md:grid-cols-12 gap-4 items-center">
                        <div className="md:col-span-6">
                          <h5 className="font-bold text-slate-800 mb-1">{insc.eventoTitulo}</h5>
                          <div className="text-slate-500 text-sm">
                            <Calendar className="inline w-4 h-4 mr-1" />{formatDate(insc.eventoFechaInicio)}
                            <span className="mx-2">|</span>
                            <MapPin className="inline w-4 h-4 mr-1" />{insc.eventoModalidad}
                          </div>
                          <div className="text-slate-500 text-sm mt-1">
                            <User className="inline w-4 h-4 mr-1" />{insc.nombre} {insc.apellido}
                            <span className="mx-2">|</span>
                            Inscripto el {formatDate(insc.fechaInscripcion)}
                          </div>
                        </div>
                        <div className="md:col-span-3 md:text-center my-2 md:my-0">
                          {insc.descuentoAplicado > 0 && (
                            <div className="text-sm text-slate-500 line-through">{formatCurrency(insc.precioBase)}</div>
                          )}
                          <div className="text-2xl font-bold text-blue-600">{formatCurrency(insc.precioFinal)}</div>
                          <span className={`badge ${insc.estado === 'Confirmada' ? 'bg-green-100 text-green-700' : insc.estado === 'Reservada' ? 'bg-blue-100 text-blue-700' : 'bg-amber-100 text-amber-700'}`}>{insc.estado}</span>
                        </div>
                        <div className="md:col-span-3 md:text-end">
                          {insc.estado === 'Pendiente' && insc.precioFinal > 0 && (
                            <div className="flex flex-col gap-2">
                              <button
                                className="btn-accent btn-sm"
                                disabled={payingId !== null}
                                onClick={async () => {
                                  setPayingId(insc.id)
                                  try {
                                    const result = await inscripcionesService.generarPago(insc.id, 1)
                                    if (result.initPoint) window.location.href = result.initPoint
                                  } catch (err) { setError(err instanceof Error ? err.message : 'Error al generar el pago'); setPayingId(null) }
                                }}
                              >
                                {payingId === insc.id ? <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-1"></span>Generando...</> : <><CreditCard className="inline w-4 h-4 mr-1" />1 pago de {formatCurrency(insc.precioFinal)}</>}
                              </button>
                              {insc.precioFinalCuotas && insc.cantidadCuotas && (
                                <button
                                  className="btn-outline-primary btn-sm"
                                  disabled={payingId !== null}
                                  onClick={async () => {
                                    setPayingId(insc.id)
                                    try {
                                      const result = await inscripcionesService.generarPago(insc.id, insc.cantidadCuotas!)
                                      if (result.initPoint) window.location.href = result.initPoint
                                    } catch (err) { setError(err instanceof Error ? err.message : 'Error al generar el pago'); setPayingId(null) }
                                  }}
                                >
                                  {payingId === insc.id ? <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-1"></span>Generando...</> : <><CreditCard className="inline w-4 h-4 mr-1" />{insc.cantidadCuotas} pagos de {formatCurrency(insc.precioFinalCuotas / insc.cantidadCuotas)}</>}
                                </button>
                              )}
                            </div>
                          )}
                          {insc.estado === 'Reservada' && insc.montoReserva && (() => {
                            const resto = insc.precioFinal - insc.montoReserva
                            const restoCuota = insc.precioFinalCuotas && insc.cantidadCuotas
                              ? (insc.precioFinalCuotas - insc.montoReserva) / insc.cantidadCuotas
                              : null
                            return (
                              <div className="flex flex-col gap-2">
                                <small className="text-slate-500 mb-1">Reserva pagada: {formatCurrency(insc.montoReserva)}</small>
                                <button
                                  className="btn-primary btn-sm"
                                  disabled={payingId !== null}
                                  onClick={async () => {
                                    setPayingId(insc.id)
                                    try {
                                      const result = await inscripcionesService.generarPago(insc.id, 1)
                                      if (result.initPoint) window.location.href = result.initPoint
                                    } catch (err) { setError(err instanceof Error ? err.message : 'Error al generar el pago'); setPayingId(null) }
                                  }}
                                >
                                  {payingId === insc.id ? <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-1"></span>Generando...</> : <><CreditCard className="inline w-4 h-4 mr-1" />Pagar resto {formatCurrency(resto)}</>}
                                </button>
                                {restoCuota && restoCuota > 0 && insc.cantidadCuotas && (
                                  <button
                                    className="btn-outline-primary btn-sm"
                                    disabled={payingId !== null}
                                    onClick={async () => {
                                      setPayingId(insc.id)
                                      try {
                                        const result = await inscripcionesService.generarPago(insc.id, insc.cantidadCuotas!)
                                        if (result.initPoint) window.location.href = result.initPoint
                                      } catch (err) { setError(err instanceof Error ? err.message : 'Error al generar el pago'); setPayingId(null) }
                                    }}
                                  >
                                    {payingId === insc.id ? <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-1"></span>Generando...</> : <><CreditCard className="inline w-4 h-4 mr-1" />{insc.cantidadCuotas} cuotas de {formatCurrency(restoCuota)}</>}
                                  </button>
                                )}
                              </div>
                            )
                          })()}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </>
      )}

      {!searched && !isAuthenticated && (
        <div className="text-center text-slate-500 py-4">
          <Search className="mx-auto w-8 h-8" />
          <p className="mt-2">Ingrese su documento para consultar sus inscripciones pendientes de pago.</p>
        </div>
      )}
    </div>
  )
}

export default MisInscripcionesPage
