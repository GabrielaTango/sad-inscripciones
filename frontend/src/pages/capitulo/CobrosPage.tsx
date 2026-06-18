import { useEffect, useMemo, useRef, useState } from 'react'
import { Search, CheckSquare, CreditCard, AlertTriangle, CheckCircle, Wallet, ArrowLeft, Clock, ShieldCheck, RefreshCw, FileSpreadsheet } from 'lucide-react'
import { capituloService } from '../../services/capituloService'
import type { Vendedor, SocioCapitulo, ResumenItem, MedioPagoCapitulo } from '../../services/capituloService'
import DebitoAutomaticoForm, {
  type DebitoAutomaticoInfo,
  type DebitoAutomaticoFormData,
} from '../../components/DebitoAutomaticoForm'

type Etapa = 'buscar' | 'cobrar'

const formatDate = (d: string) => (d ? new Date(d).toLocaleDateString('es-AR') : '-')
const formatCurrency = (n: number) => `$${n.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`

const CobrosPage = () => {
  const [vendedor, setVendedor] = useState<Vendedor | null>(null)
  const [errorVendedor, setErrorVendedor] = useState('')

  const [busqueda, setBusqueda] = useState('')
  const [socios, setSocios] = useState<SocioCapitulo[]>([])
  const [buscando, setBuscando] = useState(false)
  const [exportando, setExportando] = useState(false)

  const [etapa, setEtapa] = useState<Etapa>('buscar')
  const [socioActivo, setSocioActivo] = useState<SocioCapitulo | null>(null)
  const [resumen, setResumen] = useState<ResumenItem[]>([])
  const [cargandoResumen, setCargandoResumen] = useState(false)
  const [seleccionados, setSeleccionados] = useState<Set<number>>(new Set())
  const [pagoACuenta, setPagoACuenta] = useState(false)
  const [montoCuenta, setMontoCuenta] = useState('')
  const [medioPago, setMedioPago] = useState<MedioPagoCapitulo>('Contado')

  const [registrando, setRegistrando] = useState(false)
  const registrandoRef = useRef(false)
  const [error, setError] = useState('')
  const [exito, setExito] = useState('')

  const [debito, setDebito] = useState<DebitoAutomaticoInfo | null>(null)
  const [debitoModal, setDebitoModal] = useState(false)
  const [debitoSubmitting, setDebitoSubmitting] = useState(false)

  const buscar = async (term: string) => {
    setBuscando(true)
    setError('')
    try {
      const data = await capituloService.buscarSocios(term)
      setSocios(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al buscar socios')
    } finally {
      setBuscando(false)
    }
  }

  const exportar = async () => {
    setExportando(true)
    setError('')
    try {
      await capituloService.exportSociosExcel(busqueda.trim())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al exportar')
    } finally {
      setExportando(false)
    }
  }

  useEffect(() => {
    capituloService.me()
      .then(setVendedor)
      .catch(err => setErrorVendedor(err.message ?? 'Error cargando vendedor'))
    buscar('')
  }, [])

  const seleccionarSocio = async (socio: SocioCapitulo) => {
    setSocioActivo(socio)
    setSeleccionados(new Set())
    setPagoACuenta(false)
    setMontoCuenta('')
    setMedioPago('Contado')
    setExito('')
    setError('')
    setDebito(null)
    setEtapa('cobrar')
    setCargandoResumen(true)
    try {
      const [data, da] = await Promise.all([
        capituloService.getResumen(socio.cuit),
        capituloService.getDebitoAutomatico(socio.cuit).catch(() => null),
      ])
      setResumen(data)
      setDebito(da)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al cargar resumen')
      setResumen([])
    } finally {
      setCargandoResumen(false)
    }
  }

  const handleDebitoSubmit = async (data: DebitoAutomaticoFormData) => {
    if (!socioActivo) return
    setDebitoSubmitting(true)
    try {
      const info = await capituloService.guardarDebitoAutomatico(socioActivo.cuit, data)
      setDebito(info)
      setDebitoModal(false)
      setExito('Solicitud enviada. Se va a sincronizar con Tango en los próximos minutos.')
    } finally {
      setDebitoSubmitting(false)
    }
  }

  const handleDebitoBaja = async () => {
    if (!socioActivo) return
    if (!window.confirm('¿Dar de baja el débito automático de este socio?')) return
    setDebitoSubmitting(true)
    try {
      const info = await capituloService.darDeBajaDebitoAutomatico(socioActivo.cuit)
      setDebito(info)
      setExito('Baja solicitada. Se va a sincronizar con Tango en los próximos minutos.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al dar de baja el débito.')
    } finally {
      setDebitoSubmitting(false)
    }
  }

  const volverABuscar = () => {
    setEtapa('buscar')
    setSocioActivo(null)
    setResumen([])
    setSeleccionados(new Set())
    setPagoACuenta(false)
    setMontoCuenta('')
    setError('')
  }

  const sortedResumen = useMemo(
    () => [...resumen].sort((a, b) => new Date(a.fechaVto).getTime() - new Date(b.fechaVto).getTime()),
    [resumen]
  )

  const totalSeleccionado = useMemo(
    () => sortedResumen.filter(r => seleccionados.has(r.id)).reduce((s, r) => s + r.saldo, 0),
    [sortedResumen, seleccionados]
  )

  const toggleComprobante = (index: number) => {
    if (pagoACuenta) return
    const item = sortedResumen[index]
    if (item.pendienteSync) return
    const next = new Set(seleccionados)
    if (next.has(item.id)) {
      // al deseleccionar, caen también los posteriores (saltando los bloqueados)
      for (let i = index; i < sortedResumen.length; i++) {
        if (!sortedResumen[i].pendienteSync) next.delete(sortedResumen[i].id)
      }
    } else {
      // al seleccionar, se marcan todos los anteriores elegibles para forzar
      // el orden de cuotas (saltando los bloqueados)
      for (let i = 0; i <= index; i++) {
        if (!sortedResumen[i].pendienteSync) next.add(sortedResumen[i].id)
      }
    }
    setSeleccionados(next)
  }

  const togglePagoACuenta = () => {
    setPagoACuenta(prev => {
      const next = !prev
      if (next) setSeleccionados(new Set())
      else setMontoCuenta('')
      return next
    })
  }

  const montoFinal = pagoACuenta ? Number(montoCuenta || 0) : totalSeleccionado
  const puedeRegistrar = !registrando &&
    !!socioActivo &&
    montoFinal > 0 &&
    (pagoACuenta || seleccionados.size > 0)

  const registrar = async () => {
    if (!socioActivo) return
    if (registrandoRef.current) return // guard síncrono contra doble click
    registrandoRef.current = true
    setRegistrando(true)
    setError('')
    setExito('')
    try {
      const result = await capituloService.registrarCobro({
        cuit: socioActivo.cuit,
        medioPago,
        monto: montoFinal,
        ids: pagoACuenta ? undefined : Array.from(seleccionados),
      })
      setExito(`Cobro registrado (#${result.id}) por ${formatCurrency(result.monto)}`)
      setSeleccionados(new Set())
      setPagoACuenta(false)
      setMontoCuenta('')
      // refrescar resumen — los comprobantes recién cobrados quedan bloqueados
      const fresh = await capituloService.getResumen(socioActivo.cuit)
      setResumen(fresh)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al registrar el cobro')
    } finally {
      setRegistrando(false)
      registrandoRef.current = false
    }
  }

  return (
    <div>
      <div className="flex items-center gap-3 mb-4">
        <CreditCard className="w-6 h-6 text-blue-600" />
        <h2 className="font-bold text-slate-800 text-2xl">Registrar cobro</h2>
        {vendedor && (
          <span className="ml-auto text-sm text-slate-500">
            Capítulo: <span className="font-semibold text-slate-700">{vendedor.codVended}</span>
          </span>
        )}
      </div>

      {errorVendedor && (
        <div className="alert-danger flex items-center gap-2 mb-4">
          <AlertTriangle className="w-5 h-5" />{errorVendedor}
        </div>
      )}

      {etapa === 'buscar' && (
        <div className="card rounded-2xl border-slate-200 p-4">
          <label className="form-label">Buscar socio (CUIT o razón social)</label>
          <div className="flex gap-2">
            <input
              type="text"
              className="form-input flex-1"
              value={busqueda}
              onChange={e => setBusqueda(e.target.value)}
              onKeyDown={e => { if (e.key === 'Enter') buscar(busqueda.trim()) }}
              placeholder="Ej: 20-12345678-9 o García"
            />
            <button className="btn-primary" onClick={() => buscar(busqueda.trim())} disabled={buscando}>
              {buscando ? '...' : (<><Search className="w-4 h-4 inline mr-1" />Buscar</>)}
            </button>
          </div>

          {error && <div className="alert-danger mt-3">{error}</div>}

          {socios.length > 0 && (
            <div className="mt-4">
              <div className="flex justify-end mb-2">
                <button
                  className="btn-outline-success btn-sm"
                  onClick={exportar}
                  disabled={exportando}
                  title="Exportar todos los socios del capítulo a Excel"
                >
                  <FileSpreadsheet className="w-4 h-4 inline mr-1" />
                  {exportando ? 'Exportando...' : 'Exportar a Excel'}
                </button>
              </div>
              <div className="overflow-x-auto rounded-xl border border-slate-200">
              <table className="w-full text-sm">
                <thead className="bg-slate-800 text-white">
                  <tr>
                    <th className="p-3 text-left">CUIT</th>
                    <th className="p-3 text-left">Razón social</th>
                    <th className="p-3 text-left">Categoría</th>
                    <th className="p-3 text-left">Email</th>
                    <th className="p-3 text-left">Domicilio</th>
                    <th className="p-3 text-right">Saldo</th>
                    <th className="p-3"></th>
                  </tr>
                </thead>
                <tbody>
                  {socios.map(s => (
                    <tr key={s.cuit} className="border-b border-slate-100 hover:bg-slate-50">
                      <td className="p-3 font-mono">{s.cuit}</td>
                      <td className="p-3 font-semibold">{s.razonSoci}</td>
                      <td className="p-3 text-slate-600">{s.categoria || '-'}</td>
                      <td className="p-3 text-slate-600">{s.email || '-'}</td>
                      <td className="p-3 text-slate-600">{s.domicilio || '-'}</td>
                      <td className={`p-3 text-right font-bold ${s.saldo > 0 ? 'text-rose-600' : 'text-slate-500'}`}>
                        {formatCurrency(s.saldo)}
                      </td>
                      <td className="p-3 text-right">
                        <button className="btn-primary btn-sm" onClick={() => seleccionarSocio(s)}>
                          Cobrar
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              </div>
            </div>
          )}

          {!buscando && socios.length === 0 && !error && (
            <div className="mt-4 text-slate-500 text-sm">Sin socios para mostrar.</div>
          )}
        </div>
      )}

      {etapa === 'cobrar' && socioActivo && (
        <div className="space-y-4">
          <div className="card rounded-2xl border-slate-200 p-4 flex flex-wrap items-center gap-3">
            <button className="btn-secondary btn-sm" onClick={volverABuscar}>
              <ArrowLeft className="w-4 h-4 inline mr-1" />Cambiar socio
            </button>
            <div>
              <div className="text-xs uppercase text-slate-500">Socio</div>
              <div className="font-bold text-slate-800">{socioActivo.razonSoci}</div>
              <div className="text-sm text-slate-600 font-mono">{socioActivo.cuit}</div>
            </div>
          </div>

          <div className="card rounded-2xl border-slate-200 p-4 flex flex-wrap items-center gap-3">
            {debito?.bloqueado ? (
              <>
                <RefreshCw className="w-5 h-5 text-amber-600 animate-spin" />
                <div className="flex-1">
                  <div className="font-semibold text-slate-800">Sincronizando con Tango...</div>
                  <div className="text-sm text-slate-600">
                    Hay un cambio pendiente. Volvé a intentar en unos minutos.
                  </div>
                </div>
              </>
            ) : debito?.activo ? (
              <>
                <ShieldCheck className="w-5 h-5 text-emerald-600" />
                <div className="flex-1">
                  <div className="font-semibold text-slate-800">Débito automático activo</div>
                  <div className="text-sm text-slate-600">
                    {debito.marcaTarjeta} ****{debito.tarjetaUltimos4}
                    {debito.vencimiento ? ` · vence ${debito.vencimiento}` : ''}
                  </div>
                </div>
                <button
                  type="button"
                  className="btn-secondary btn-sm"
                  disabled={debitoSubmitting}
                  onClick={() => setDebitoModal(true)}
                >
                  Modificar tarjeta
                </button>
                <button
                  type="button"
                  className="btn-outline-danger btn-sm"
                  disabled={debitoSubmitting}
                  onClick={handleDebitoBaja}
                >
                  Dar de baja
                </button>
              </>
            ) : (
              <>
                <CreditCard className="w-5 h-5 text-blue-600" />
                <div className="flex-1">
                  <div className="font-semibold text-slate-800">Sin débito automático</div>
                  <div className="text-sm text-slate-600">
                    Adherir al socio para cobros automáticos de sus cuotas.
                  </div>
                </div>
                <button
                  type="button"
                  className="btn-primary btn-sm"
                  onClick={() => setDebitoModal(true)}
                >
                  Adherir débito automático
                </button>
              </>
            )}
          </div>

          <DebitoAutomaticoForm
            open={debitoModal}
            onClose={() => setDebitoModal(false)}
            onSubmit={handleDebitoSubmit}
            submitting={debitoSubmitting}
            currentInfo={debito}
            titulo={`Débito automático · ${socioActivo.razonSoci}`}
          />

          {exito && (
            <div className="alert-success flex items-center gap-2">
              <CheckCircle className="w-5 h-5" />{exito}
            </div>
          )}
          {error && (
            <div className="alert-danger flex items-center gap-2">
              <AlertTriangle className="w-5 h-5" />{error}
            </div>
          )}

          <div className="card rounded-2xl border-slate-200 p-4">
            <div className="flex flex-wrap items-center gap-3 mb-3">
              <h3 className="font-bold text-slate-800 flex items-center gap-2">
                <CheckSquare className="w-5 h-5" />Cuotas pendientes
              </h3>
              <label className="ml-auto flex items-center gap-2 text-sm">
                <input type="checkbox" checked={pagoACuenta} onChange={togglePagoACuenta} />
                <Wallet className="w-4 h-4" />Pago a cuenta (sin imputar comprobantes)
              </label>
            </div>

            {cargandoResumen ? (
              <div className="text-center py-6 text-slate-500">Cargando...</div>
            ) : pagoACuenta ? (
              <div>
                <label className="form-label">Monto a cuenta</label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  className="form-input"
                  value={montoCuenta}
                  onChange={e => setMontoCuenta(e.target.value)}
                  placeholder="0.00"
                />
                <p className="text-xs text-slate-500 mt-1">
                  Queda como saldo a favor del socio en Tango.
                </p>
              </div>
            ) : sortedResumen.length === 0 ? (
              <div className="text-slate-500 text-sm">Sin comprobantes pendientes.</div>
            ) : (
              <div className="overflow-x-auto rounded-xl border border-slate-200">
                <table className="w-full text-sm">
                  <thead className="bg-slate-800 text-white">
                    <tr>
                      <th style={{ width: 50 }} className="text-center p-3"></th>
                      <th className="p-3 text-left">Tipo</th>
                      <th className="p-3 text-left">Comprobante</th>
                      <th className="p-3 text-left">Vencimiento</th>
                      <th className="p-3 text-right">Saldo</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sortedResumen.map((mov, index) => {
                      const sel = seleccionados.has(mov.id)
                      const pendiente = mov.pendienteSync
                      const rowClass = pendiente
                        ? 'bg-amber-50 cursor-not-allowed border-b border-slate-100'
                        : `${sel ? 'bg-blue-50' : ''} cursor-pointer hover:bg-slate-50 border-b border-slate-100`
                      return (
                        <tr
                          key={mov.id}
                          className={rowClass}
                          onClick={() => toggleComprobante(index)}
                        >
                          <td className="text-center p-3">
                            <input
                              type="checkbox"
                              className="w-4 h-4 rounded border-slate-300"
                              checked={sel}
                              disabled={pendiente}
                              onChange={() => toggleComprobante(index)}
                              onClick={e => e.stopPropagation()}
                            />
                          </td>
                          <td className="p-3"><span className="badge bg-slate-100 text-slate-700">{mov.tComp}</span></td>
                          <td className="p-3 font-semibold">
                            {mov.nComp}
                            {pendiente && (
                              <span
                                className="ml-2 inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-amber-800"
                                title="Ya hay un cobro registrado para este comprobante esperando sincronizar a Tango"
                              >
                                <Clock className="w-3 h-3" />Pendiente sync
                              </span>
                            )}
                          </td>
                          <td className="p-3">{formatDate(mov.fechaVto)}</td>
                          <td className={`p-3 text-right font-bold ${pendiente ? 'text-slate-400 line-through' : ''}`}>
                            {formatCurrency(mov.saldo)}
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <div className="card rounded-2xl border-slate-200 shadow-md p-4">
            <div className="grid grid-cols-1 md:grid-cols-12 gap-4 items-end">
              <div className="md:col-span-4">
                <label className="form-label">Medio de pago</label>
                <select
                  className="form-input"
                  value={medioPago}
                  onChange={e => setMedioPago(e.target.value as MedioPagoCapitulo)}
                >
                  <option value="Contado">Contado</option>
                  <option value="Transferencia">Transferencia</option>
                </select>
              </div>
              <div className="md:col-span-4 md:text-right">
                <div className="text-slate-600 text-sm">Total a cobrar</div>
                <div className="text-2xl font-bold text-blue-600">{formatCurrency(montoFinal)}</div>
              </div>
              <div className="md:col-span-4">
                <button
                  className="btn-accent w-full"
                  disabled={!puedeRegistrar}
                  onClick={registrar}
                >
                  {registrando
                    ? <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-2"></span>Registrando...</>
                    : <span className="flex items-center justify-center gap-2"><CreditCard className="w-5 h-5" />Registrar cobro</span>}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default CobrosPage
