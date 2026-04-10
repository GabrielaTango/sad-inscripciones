import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { CheckCircle, CreditCard, Send, ShieldCheck, BookmarkCheck } from 'lucide-react'
import { eventosService } from '../services/eventosService'
import { eventoPreciosService } from '../services/eventoPreciosService'
import { tiposAlumnoService } from '../services/tiposAlumnoService'
import { inscripcionesService } from '../services/inscripcionesService'
import { becaCodigosService } from '../services/becaCodigosService'
import { promocionCuponesService, type PromocionCuponDisponible } from '../services/promocionCuponesService'
import { provinciasService } from '../services/provinciasService'
import { authService } from '../services/authService'
import { useAuth } from '../context/AuthContext'
import type { Evento, EventoPrecio, TipoAlumno, InscripcionForm, Provincia } from '../types/models'

const InscripcionPage = () => {
  const { eventoId } = useParams<{ eventoId: string }>()
  const id = Number(eventoId)
  const { isAuthenticated } = useAuth()

  const [evento, setEvento] = useState<Evento | null>(null)
  const [precios, setPrecios] = useState<EventoPrecio[]>([])
  const [tiposAlumno, setTiposAlumno] = useState<TipoAlumno[]>([])
  const [provincias, setProvincias] = useState<Provincia[]>([])
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [becaValida, setBecaValida] = useState<boolean | null>(null)
  const [cuponesDisponibles, setCuponesDisponibles] = useState<PromocionCuponDisponible[]>([])
  const [aceptaTerminos, setAceptaTerminos] = useState(false)
  const [modalidadPago, setModalidadPago] = useState<'unico' | 'cuotas' | 'reserva'>('unico')

  // Socio detectado
  const [esSocio, setEsSocio] = useState(false)
  const [socioDataCargada, setSocioDataCargada] = useState(false)
  const [documentoValidado, setDocumentoValidado] = useState(false)

  // Estado post-inscripcion
  const [sinCosto, setSinCosto] = useState(false)

  const [form, setForm] = useState<InscripcionForm>({
    eventoId: id, tipoAlumnoId: 0, nombre: '', apellido: '',
    email: '', telefono: '', documento: '', provincia: '', codigoBeca: '', codigoCupon: '',
    fechaNacimiento: '', domicilio: '', codigoPostal: '', localidad: '',
    pais: '', celular: '', profesion: '', especialidad: '', institucion: '', sector: '',
  })

  useEffect(() => {
    const load = async () => {
      try {
        const [ev, prec, tipos, provs] = await Promise.all([
          eventosService.getById(id),
          eventoPreciosService.getByEventoId(id),
          tiposAlumnoService.getAll().catch(() => [] as TipoAlumno[]),
          provinciasService.getAll().catch(() => [] as Provincia[]),
        ])
        setEvento(ev); setPrecios(prec); setTiposAlumno(tipos); setProvincias(provs)

        if (isAuthenticated) {
          try {
            const socioData = await authService.getSocioData()
            const socioTipo = tipos.find(t => t.nombre.toLowerCase() === 'socio')
            const socioFiltroPrice = socioTipo ? prec.find(p => p.tipoAlumnoId === socioTipo.id && p.activo) : null

            setEsSocio(true)
            setSocioDataCargada(true)
            setForm(prev => ({
              ...prev,
              documento: socioData.documento || prev.documento,
              apellido: socioData.apellido || prev.apellido,
              nombre: socioData.nombre || prev.nombre,
              domicilio: socioData.domicilio || prev.domicilio,
              codigoPostal: socioData.codigoPostal || prev.codigoPostal,
              provincia: socioData.provincia || prev.provincia,
              ...(socioFiltroPrice ? { tipoAlumnoId: socioFiltroPrice.tipoAlumnoId } : {}),
            }))
            if (socioData.documento) {
              promocionCuponesService.getDisponibles(socioData.documento)
                .then(setCuponesDisponibles)
                .catch(() => setCuponesDisponibles([]))
            }
          } catch {
            // Socio not found — user can fill the form manually
          }
        }
      } catch { setError('No se pudo cargar el evento.') }
      finally { setLoading(false) }
    }
    load()
  }, [id, isAuthenticated])

  const selectedPrecio = precios.find(p => p.tipoAlumnoId === form.tipoAlumnoId && p.activo)
  const tipoAlumnoNombre = (taId: number) => tiposAlumno.find(t => t.id === taId)?.nombre || ''

  const socioTipoAlumno = tiposAlumno.find(t => t.nombre.toLowerCase() === 'socio')
  const socioPrice = socioTipoAlumno ? precios.find(p => p.tipoAlumnoId === socioTipoAlumno.id && p.activo) : null
  const preciosFiltrados = esSocio && socioPrice
    ? precios.filter(p => p.tipoAlumnoId === socioTipoAlumno!.id && p.activo)
    : documentoValidado && !esSocio && socioTipoAlumno
      ? precios.filter(p => p.activo && p.tipoAlumnoId !== socioTipoAlumno.id)
      : precios.filter(p => p.activo)

  const loadCuponesDisponibles = async (documento: string) => {
    try {
      const cupones = await promocionCuponesService.getDisponibles(documento)
      setCuponesDisponibles(cupones)
    } catch {
      setCuponesDisponibles([])
    }
  }

  const handleDocumentoBlur = async () => {
    if (isAuthenticated || !form.documento.trim()) return
    const doc = form.documento.trim()
    try {
      const socioData = await authService.getSocioDataByCuit(doc)
      setEsSocio(true)
      setDocumentoValidado(true)
      const socioTipo = tiposAlumno.find(t => t.nombre.toLowerCase() === 'socio')
      const socioP = socioTipo ? precios.find(p => p.tipoAlumnoId === socioTipo.id && p.activo) : null

      setForm(prev => ({
        ...prev,
        apellido: socioData.apellido || prev.apellido,
        nombre: socioData.nombre || prev.nombre,
        domicilio: socioData.domicilio || prev.domicilio,
        codigoPostal: socioData.codigoPostal || prev.codigoPostal,
        provincia: socioData.provincia || prev.provincia,
        ...(socioP ? { tipoAlumnoId: socioP.tipoAlumnoId } : {}),
      }))
    } catch {
      setEsSocio(false)
      setDocumentoValidado(true)
      setForm(prev => ({
        eventoId: id, tipoAlumnoId: 0, nombre: '', apellido: '',
        email: '', telefono: '', documento: prev.documento, provincia: '', codigoBeca: '', codigoCupon: '',
        fechaNacimiento: '', domicilio: '', codigoPostal: '', localidad: '',
        pais: '', celular: '', profesion: '', especialidad: '', institucion: '', sector: '',
      }))
    }
    await loadCuponesDisponibles(doc)
  }

  const handleValidarBeca = async () => {
    if (!form.codigoBeca) return
    try {
      await becaCodigosService.validarCodigo(form.codigoBeca)
      setBecaValida(true)
    } catch {
      setBecaValida(false)
    }
  }

  const handleSubmit = async (cuotas: number) => {
    setSubmitting(true); setError('')
    try {
      const result = await inscripcionesService.create({ ...form, cuotas, modalidadPago })

      if (result.initPoint) {
        window.location.href = result.initPoint
      } else {
        setSinCosto(true)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al procesar la inscripcion.')
    } finally { setSubmitting(false) }
  }

  if (loading) return <div className="text-center py-16"><div className="animate-spin w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full mx-auto"></div></div>
  if (!evento) return <div className="text-center py-16"><p className="text-slate-600">Evento no encontrado.</p><Link to="/eventos" className="btn-primary">Ver Eventos</Link></div>

  // Vista: inscripcion sin costo
  if (sinCosto) {
    return (
      <>
        <section className="page-header">
          <div className="max-w-7xl mx-auto px-4">
            <h1 className="font-bold">Inscripcion</h1>
            <p className="text-lg text-white/90 mb-0">{evento.titulo}</p>
          </div>
        </section>
        <section className="py-16">
          <div className="max-w-7xl mx-auto px-4">
            <div className="flex justify-center">
              <div className="max-w-2xl text-center py-16">
                <CheckCircle className="text-green-600 mx-auto" style={{ width: '5rem', height: '5rem' }} />
                <h3 className="font-bold mt-3">Inscripcion exitosa!</h3>
                <p className="text-slate-600">Tu inscripcion ha sido registrada sin costo.</p>
                <Link to="/eventos" className="btn-primary mt-3">Ver mas eventos</Link>
              </div>
            </div>
          </div>
        </section>
      </>
    )
  }

  // Vista: formulario de inscripcion
  return (
    <>
      <section className="page-header">
        <div className="max-w-7xl mx-auto px-4">
          <h1 className="font-bold">Inscripcion</h1>
          <p className="text-lg text-white/90 mb-0">{evento.titulo}</p>
        </div>
      </section>

      <section className="py-16">
        <div className="max-w-7xl mx-auto px-4">
          <div className="flex justify-center">
            <div className="w-full max-w-4xl">
              <div className="card rounded-2xl border-slate-200">
                <div className="p-6 md:p-8">
                  <h4 className="font-bold text-slate-800 mb-4">Formulario de Inscripcion</h4>
                  {error && <div className="alert-danger">{error}</div>}

                  <form onSubmit={e => e.preventDefault()}>

                    {/* Seccion 1: Datos del Solicitante */}
                    <h5 className="font-bold mt-2 mb-3">Datos del Solicitante</h5>
                    <div className="border-t border-slate-200 mb-3"></div>
                    <div className="grid grid-cols-1 md:grid-cols-12 gap-4 mb-6">
                      <div className="md:col-span-4">
                        <label className="form-label">Documento *</label>
                        <input type="text" className="form-input" value={form.documento} onChange={(e) => setForm({ ...form, documento: e.target.value })} onBlur={handleDocumentoBlur} required readOnly={socioDataCargada} />
                      </div>
                      <div className="md:col-span-4">
                        <label className="form-label">Apellido *</label>
                        <input type="text" className="form-input" value={form.apellido} onChange={(e) => setForm({ ...form, apellido: e.target.value })} required readOnly={socioDataCargada} />
                      </div>
                      <div className="md:col-span-4">
                        <label className="form-label">Nombres *</label>
                        <input type="text" className="form-input" value={form.nombre} onChange={(e) => setForm({ ...form, nombre: e.target.value })} required readOnly={socioDataCargada} />
                      </div>
                      <div className="md:col-span-6">
                        <label className="form-label">Categoria *</label>
                        <select className="form-select" value={form.tipoAlumnoId} onChange={(e) => setForm({ ...form, tipoAlumnoId: Number(e.target.value) })} required>
                          <option value={0}>Seleccionar...</option>
                          {preciosFiltrados.map(p => (
                            <option key={p.tipoAlumnoId} value={p.tipoAlumnoId}>
                              {tipoAlumnoNombre(p.tipoAlumnoId)} - ${p.precioBase.toFixed(2)}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="md:col-span-6">
                        <label className="form-label">Fecha de Nacimiento</label>
                        <input type="date" className="form-input" value={form.fechaNacimiento || ''} onChange={(e) => setForm({ ...form, fechaNacimiento: e.target.value })} />
                      </div>

                      {selectedPrecio && (
                        <div className="md:col-span-12">
                          <div className="alert-info mb-0">
                            <strong>Precio base:</strong> ${selectedPrecio.precioBase.toFixed(2)}
                            {selectedPrecio.permiteDescuento && <span className="text-slate-600 ml-2">(acepta descuentos)</span>}
                          </div>
                        </div>
                      )}
                    </div>

                    {/* Seccion 2: Domicilio Particular */}
                    <h5 className="font-bold mt-2 mb-3">Domicilio Particular</h5>
                    <div className="border-t border-slate-200 mb-3"></div>
                    <div className="grid grid-cols-1 md:grid-cols-12 gap-4 mb-6">
                      <div className="md:col-span-12">
                        <label className="form-label">Domicilio</label>
                        <input type="text" className="form-input" value={form.domicilio || ''} onChange={(e) => setForm({ ...form, domicilio: e.target.value })} />
                      </div>
                      <div className="md:col-span-4">
                        <label className="form-label">Codigo Postal</label>
                        <input type="text" className="form-input" value={form.codigoPostal || ''} onChange={(e) => setForm({ ...form, codigoPostal: e.target.value })} />
                      </div>
                      <div className="md:col-span-4">
                        <label className="form-label">Localidad</label>
                        <input type="text" className="form-input" value={form.localidad || ''} onChange={(e) => setForm({ ...form, localidad: e.target.value })} />
                      </div>
                      <div className="md:col-span-4">
                        <label className="form-label">Provincia</label>
                        <select className="form-select" value={form.provincia || ''} onChange={(e) => setForm({ ...form, provincia: e.target.value })}>
                          <option value="">Seleccionar...</option>
                          {provincias.map(p => (
                            <option key={p.codProvin} value={p.codProvin}>
                              {p.nombrePro}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="md:col-span-4">
                        <label className="form-label">Pais</label>
                        <input type="text" className="form-input" value={form.pais || ''} onChange={(e) => setForm({ ...form, pais: e.target.value })} />
                      </div>
                      <div className="md:col-span-4">
                        <label className="form-label">Telefono</label>
                        <input type="tel" className="form-input" value={form.telefono || ''} onChange={(e) => setForm({ ...form, telefono: e.target.value })} />
                      </div>
                      <div className="md:col-span-4">
                        <label className="form-label">Celular</label>
                        <input type="tel" className="form-input" value={form.celular || ''} onChange={(e) => setForm({ ...form, celular: e.target.value })} />
                      </div>
                      <div className="md:col-span-12">
                        <label className="form-label">Email *</label>
                        <input type="email" className="form-input" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required />
                      </div>
                    </div>

                    {/* Seccion 3: Datos Profesionales */}
                    <h5 className="font-bold mt-2 mb-3">Datos Profesionales</h5>
                    <div className="border-t border-slate-200 mb-3"></div>
                    <div className="grid grid-cols-1 md:grid-cols-12 gap-4 mb-6">
                      <div className="md:col-span-6">
                        <label className="form-label">Profesion</label>
                        <input type="text" className="form-input" value={form.profesion || ''} onChange={(e) => setForm({ ...form, profesion: e.target.value })} />
                      </div>
                      <div className="md:col-span-6">
                        <label className="form-label">Especialidad</label>
                        <input type="text" className="form-input" value={form.especialidad || ''} onChange={(e) => setForm({ ...form, especialidad: e.target.value })} />
                      </div>
                    </div>

                    {/* Seccion 4: Lugar de Trabajo */}
                    <h5 className="font-bold mt-2 mb-3">Lugar de Trabajo</h5>
                    <div className="border-t border-slate-200 mb-3"></div>
                    <div className="grid grid-cols-1 md:grid-cols-12 gap-4 mb-6">
                      <div className="md:col-span-6">
                        <label className="form-label">Institucion</label>
                        <input type="text" className="form-input" value={form.institucion || ''} onChange={(e) => setForm({ ...form, institucion: e.target.value })} />
                      </div>
                      <div className="md:col-span-6">
                        <label className="form-label">Sector / Servicio</label>
                        <input type="text" className="form-input" value={form.sector || ''} onChange={(e) => setForm({ ...form, sector: e.target.value })} />
                      </div>
                    </div>

                    {/* Codigo de Beca y Submit */}
                    <div className="grid grid-cols-1 md:grid-cols-12 gap-4">
                      <div className="md:col-span-12">
                        <label className="form-label">Codigo de Beca</label>
                        <div className="flex">
                          <input type="text" className="form-input" placeholder="Ingrese codigo si tiene uno" value={form.codigoBeca || ''} onChange={(e) => { setForm({ ...form, codigoBeca: e.target.value }); setBecaValida(null) }} />
                          <button type="button" className="btn-outline" onClick={handleValidarBeca} disabled={!form.codigoBeca}>Validar</button>
                        </div>
                        {becaValida === true && <span className="text-sm text-green-600">Codigo valido</span>}
                        {becaValida === false && <span className="text-sm text-red-600">Codigo invalido o ya utilizado</span>}
                      </div>

                      {cuponesDisponibles.length > 0 && (
                        <div className="md:col-span-12">
                          <label className="form-label">Cupon Promocional</label>
                          <select className="form-select" value={form.codigoCupon || ''} onChange={(e) => setForm({ ...form, codigoCupon: e.target.value })}>
                            <option value="">Sin cupon</option>
                            {cuponesDisponibles.map(c => (
                              <option key={c.id} value={c.codigo}>
                                {c.promocionNombre} - {c.tipoDescuento === 'Porcentaje' ? c.valor + '%' : '$' + c.valor} desc.
                                {c.fechaVencimiento ? ` (vence ${new Date(c.fechaVencimiento).toLocaleDateString('es-AR')})` : ''}
                              </option>
                            ))}
                          </select>
                        </div>
                      )}

                      {evento.terminosArchivo && (
                        <div className="md:col-span-12 mt-3">
                          <div className="form-check">
                            <input
                              type="checkbox"
                              className="form-check-input"
                              id="aceptaTerminos"
                              checked={aceptaTerminos}
                              onChange={(e) => setAceptaTerminos(e.target.checked)}
                              required
                            />
                            <label className="text-sm" htmlFor="aceptaTerminos">
                              Acepto los{' '}
                              <a href={`/api/eventos/${id}/terminos/archivo`} target="_blank" rel="noopener noreferrer">
                                terminos y condiciones
                              </a>
                            </label>
                          </div>
                        </div>
                      )}

                      <div className="md:col-span-12 mt-4">
                        {selectedPrecio && selectedPrecio.precioBase > 0 ? (
                          <>
                            <div className="mb-3">
                              <div className="inline-flex items-center gap-2">
                                <input className="form-check-input" type="radio" name="modalidadPago" id="pagoUnico" value="unico" checked={modalidadPago === 'unico'} onChange={() => setModalidadPago('unico')} />
                                <label className="text-sm" htmlFor="pagoUnico">En un Pago</label>
                              </div>
                              {selectedPrecio.precioCuotas && selectedPrecio.precioCuotas > 0 && (
                                <div className="inline-flex items-center gap-2">
                                  <input className="form-check-input" type="radio" name="modalidadPago" id="pagoCuotas" value="cuotas" checked={modalidadPago === 'cuotas'} onChange={() => setModalidadPago('cuotas')} />
                                  <label className="text-sm" htmlFor="pagoCuotas">Cuotas Sin Interes</label>
                                </div>
                              )}
                              <div className="inline-flex items-center gap-2">
                                <input className="form-check-input" type="radio" name="modalidadPago" id="pagoReserva" value="reserva" checked={modalidadPago === 'reserva'} onChange={() => setModalidadPago('reserva')} />
                                <label className="text-sm" htmlFor="pagoReserva">Reservar Vacante</label>
                              </div>
                            </div>

                            {modalidadPago === 'unico' && (
                              <button type="button" className="btn-primary btn-lg w-full" disabled={submitting || !form.tipoAlumnoId || (!!evento.terminosArchivo && !aceptaTerminos)} onClick={() => handleSubmit(1)}>
                                {submitting ? <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-2"></span>Procesando...</> : <><CreditCard className="inline mr-2" size={18} />Pagar ${selectedPrecio.precioBase.toLocaleString('es-AR', { minimumFractionDigits: 2 })}</>}
                              </button>
                            )}

                            {modalidadPago === 'cuotas' && selectedPrecio.precioCuotas && selectedPrecio.precioCuotas > 0 && (
                              <button type="button" className="btn-outline-primary btn-lg w-full" disabled={submitting || !form.tipoAlumnoId || (!!evento.terminosArchivo && !aceptaTerminos)} onClick={() => handleSubmit(selectedPrecio.cantidadCuotas || 6)}>
                                {submitting ? <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-2"></span>Procesando...</> : <><CreditCard className="inline mr-2" size={18} />{selectedPrecio.cantidadCuotas || 6} cuotas sin interes de ${(selectedPrecio.precioCuotas / (selectedPrecio.cantidadCuotas || 6)).toLocaleString('es-AR', { minimumFractionDigits: 2 })}</>}
                              </button>
                            )}

                            {modalidadPago === 'reserva' && (
                              <button type="button" className="btn-accent btn-lg w-full" disabled={submitting || !form.tipoAlumnoId || (!!evento.terminosArchivo && !aceptaTerminos)} onClick={() => handleSubmit(1)}>
                                {submitting ? <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-2"></span>Procesando...</> : <><BookmarkCheck className="inline mr-2" size={18} />Reservar vacante por ${Math.round(selectedPrecio.precioBase * 0.3).toLocaleString('es-AR')}</>}
                              </button>
                            )}

                            <div className="text-center mt-2">
                              <span className="text-sm text-slate-600">
                                <ShieldCheck className="inline mr-1" size={14} />
                                Pago seguro procesado por Mercado Pago
                              </span>
                            </div>
                          </>
                        ) : (
                          <button type="button" className="btn-primary btn-lg w-full" disabled={submitting || !form.tipoAlumnoId || (!!evento.terminosArchivo && !aceptaTerminos)} onClick={() => handleSubmit(1)}>
                            {submitting ? <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-2"></span>Procesando...</> : <><Send className="inline mr-2" size={18} />Inscribirse</>}
                          </button>
                        )}
                      </div>
                    </div>
                  </form>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>
    </>
  )
}

export default InscripcionPage
