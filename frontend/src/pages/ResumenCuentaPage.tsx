import { useState, useEffect, useMemo, useCallback } from 'react'
import { useAuth } from '../context/AuthContext'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { resumenCuentaService, type ResumenCuentaMeta } from '../services/resumenCuentaService'
import type { ResumenCuenta } from '../types/models'
import pagoMisCuentasLogo from '../assets/pago-mis-cuentas.png'
import DebitoAutomaticoForm, {
  type DebitoAutomaticoInfo,
  type DebitoAutomaticoFormData,
} from '../components/DebitoAutomaticoForm'
import {
  Receipt,
  CheckCircle,
  XCircle,
  Clock,
  CheckSquare,
  CreditCard,
  Info,
  X,
  Barcode,
  ShieldCheck,
  RefreshCw,
} from 'lucide-react'

const ResumenCuentaPage = () => {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const [movimientos, setMovimientos] = useState<ResumenCuenta[]>([])
  const [meta, setMeta] = useState<ResumenCuentaMeta | null>(null)
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set())
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [paying, setPaying] = useState<null | 'mp' | 'pf'>(null)
  const [pagoResultado, setPagoResultado] = useState<{ status: string; message: string } | null>(null)
  const [debito, setDebito] = useState<DebitoAutomaticoInfo | null>(null)
  const [debitoModal, setDebitoModal] = useState(false)
  const [debitoSubmitting, setDebitoSubmitting] = useState(false)
  const [debitoOk, setDebitoOk] = useState('')

  const loadData = useCallback(async () => {
    try {
      const [mov, m, da] = await Promise.all([
        resumenCuentaService.getByCuit(),
        resumenCuentaService.getMeta().catch(() => null),
        resumenCuentaService.getDebitoAutomatico().catch(() => null),
      ])
      setMovimientos(mov)
      setMeta(m)
      setDebito(da)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al cargar resumen')
    } finally {
      setLoading(false)
    }
  }, [])

  const handleDebitoSubmit = async (data: DebitoAutomaticoFormData) => {
    setDebitoSubmitting(true)
    try {
      const info = await resumenCuentaService.guardarDebitoAutomatico(data)
      setDebito(info)
      setDebitoModal(false)
      setDebitoOk('Solicitud enviada. Se va a sincronizar con Tango en los próximos minutos.')
      setTimeout(() => setDebitoOk(''), 6000)
    } finally {
      setDebitoSubmitting(false)
    }
  }

  const handleDebitoBaja = async () => {
    if (!window.confirm('¿Querés dar de baja el débito automático? Se sincronizará con Tango.')) return
    setDebitoSubmitting(true)
    try {
      const info = await resumenCuentaService.darDeBajaDebitoAutomatico()
      setDebito(info)
      setDebitoOk('Baja solicitada. Se va a sincronizar con Tango en los próximos minutos.')
      setTimeout(() => setDebitoOk(''), 6000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al dar de baja el débito.')
    } finally {
      setDebitoSubmitting(false)
    }
  }

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login')
      return
    }
    resumenCuentaService.validarPendientes()
      .catch(() => { /* no bloquear si falla la validación */ })
      .finally(loadData)
  }, [isAuthenticated, navigate, loadData])

  // Handle MP return — MP sends collection_status/collection_id on auto_return, or status on back_urls
  useEffect(() => {
    const status = searchParams.get('collection_status') || searchParams.get('status')
    const extRef = searchParams.get('external_reference')
    const paymentId = searchParams.get('collection_id') || searchParams.get('payment_id')

    if (status && extRef) {
      // Clear params from URL
      setSearchParams({}, { replace: true })

      if (status === 'approved') {
        resumenCuentaService.confirmarPago(extRef, paymentId ? Number(paymentId) : undefined)
          .then(result => {
            setPagoResultado({
              status: 'success',
              message: result.estadoPago === 'Aprobado'
                ? 'El pago fue registrado exitosamente.'
                : 'El pago esta siendo procesado. La imputacion se vera reflejada en su resumen de cuenta una vez confirmado.'
            })
            loadData()
          })
          .catch(() => {
            setPagoResultado({
              status: 'success',
              message: 'El pago fue registrado.'
            })
            loadData()
          })
      } else if (status === 'rejected') {
        setPagoResultado({ status: 'danger', message: 'El pago fue rechazado. Puede intentar nuevamente.' })
        loadData()
      } else {
        loadData()
      }
    }
  }, [searchParams, setSearchParams, loadData])

  const sorted = useMemo(
    () => [...movimientos].sort((a, b) => new Date(a.fechaVto).getTime() - new Date(b.fechaVto).getTime()),
    [movimientos]
  )

  const totalSeleccionado = useMemo(
    () => sorted.filter(m => selectedIds.has(m.id)).reduce((sum, m) => sum + m.saldo, 0),
    [sorted, selectedIds]
  )


  const handleToggle = (index: number) => {
    const item = sorted[index]
    const newSelected = new Set(selectedIds)

    if (newSelected.has(item.id)) {
      for (let i = index; i < sorted.length; i++) {
        newSelected.delete(sorted[i].id)
      }
    } else {
      for (let i = 0; i <= index; i++) {
        newSelected.add(sorted[i].id)
      }
    }

    setSelectedIds(newSelected)
  }

  const handlePagar = async () => {
    if (selectedIds.size === 0) return
    setPaying('mp')
    setError('')
    try {
      const result = await resumenCuentaService.generarPago(Array.from(selectedIds))
      if (result.initPoint) {
        window.location.href = result.initPoint
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al generar el pago')
      setPaying(null)
    }
  }

  const handlePagarPagoFacil = async () => {
    if (selectedIds.size === 0) return
    setPaying('pf')
    setError('')
    try {
      const result = await resumenCuentaService.generarCuponPagoFacil(Array.from(selectedIds))
      navigate(`/resumen-cuenta/cupon-pagofacil/${result.id}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al generar el cupón')
      setPaying(null)
    }
  }

  const formatDate = (d: string) => d ? new Date(d).toLocaleDateString('es-AR') : '-'
  const formatCurrency = (n: number) => `$${n.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`

  if (loading) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center">
        <div className="animate-spin w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full inline-block" role="status"></div>
      </div>
    )
  }

  return (
    <div className="max-w-7xl mx-auto px-4 py-16">
      <h2 className="font-bold text-slate-800 mb-4 flex items-center gap-2">
        <Receipt className="inline-block" />Resumen de Cuenta
      </h2>

      {/* Payment result alert */}
      {pagoResultado && (
        <div className={`alert-${pagoResultado.status === 'warning' ? 'info' : pagoResultado.status} flex items-center justify-between`} role="alert">
          <span className="flex items-center gap-2">
            {pagoResultado.status === 'success' ? <CheckCircle className="w-5 h-5" /> : pagoResultado.status === 'danger' ? <XCircle className="w-5 h-5" /> : <Clock className="w-5 h-5" />}
            {pagoResultado.message}
          </span>
          <button type="button" onClick={() => setPagoResultado(null)} className="ml-4 p-1 hover:opacity-70">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {error && <div className="alert-danger">{error}</div>}

      {debitoOk && (
        <div className="alert-success flex items-center gap-2 mb-3">
          <CheckCircle className="w-5 h-5" />{debitoOk}
        </div>
      )}

      {/* Débito automático */}
      <div className="card rounded-2xl border-slate-200 mb-3 p-4 flex flex-wrap items-center gap-3">
        {debito?.bloqueado ? (
          <>
            <RefreshCw className="w-5 h-5 text-amber-600 animate-spin" />
            <div className="flex-1">
              <div className="font-semibold text-slate-800">Sincronizando con Tango...</div>
              <div className="text-sm text-slate-600">
                Tu solicitud está siendo procesada. Volvé a entrar en unos minutos.
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
              Modificar
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
              <div className="font-semibold text-slate-800">¿Querés cobrar tus cuotas por débito automático?</div>
              <div className="text-sm text-slate-600">
                Cargá tu tarjeta y nos encargamos del resto.
              </div>
            </div>
            <button
              type="button"
              className="btn-primary btn-sm"
              onClick={() => setDebitoModal(true)}
            >
              Adherir
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
      />

      {/* Comprobantes pendientes */}
      {movimientos.length === 0 ? (
        <div className="alert-light flex items-center gap-2">
          <Info className="w-5 h-5" />
          No se encontraron comprobantes pendientes en su cuenta corriente.
        </div>
      ) : (
        <>
          <div className="card rounded-2xl border-slate-200">
            <div className="overflow-x-auto rounded-2xl border border-slate-200">
              <table className="w-full text-sm">
                <thead className="bg-slate-800 text-white">
                  <tr>
                    <th style={{ width: '50px' }} className="text-center p-3">
                      <CheckSquare className="w-4 h-4 inline-block" />
                    </th>
                    <th className="p-3">Tipo</th>
                    <th className="p-3">Comprobante</th>
                    <th className="p-3">Vencimiento</th>
                    <th className="p-3 text-right">Saldo</th>
                  </tr>
                </thead>
                <tbody>
                  {sorted.map((mov, index) => {
                    const isSelected = selectedIds.has(mov.id)
                    return (
                      <tr
                        key={mov.id}
                        className={`${isSelected ? 'bg-blue-50' : ''} cursor-pointer hover:bg-slate-50 border-b border-slate-100`}
                        onClick={() => handleToggle(index)}
                      >
                        <td className="text-center p-3">
                          <input
                            type="checkbox"
                            className="w-4 h-4 rounded border-slate-300"
                            checked={isSelected}
                            onChange={() => handleToggle(index)}
                            onClick={e => e.stopPropagation()}
                          />
                        </td>
                        <td className="p-3"><span className="badge bg-slate-100 text-slate-700">{mov.tComp}</span></td>
                        <td className="p-3 font-semibold">{mov.nComp}</td>
                        <td className="p-3">{formatDate(mov.fechaVto)}</td>
                        <td className="p-3 text-right font-bold">{formatCurrency(mov.saldo)}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </div>

          {/* Footer: Total + Pay button */}
          <div className="card rounded-2xl border-slate-200 shadow-md mt-3">
            <div className="p-4">
              <div className="grid grid-cols-1 md:grid-cols-12 gap-4 items-center">
                <div className="md:col-span-5">
                  <div className="text-slate-600 text-sm">
                    {selectedIds.size} comprobante{selectedIds.size !== 1 ? 's' : ''} seleccionado{selectedIds.size !== 1 ? 's' : ''}
                  </div>
                  {meta?.codClient && (
                    <div className="mt-2 flex items-center gap-3 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
                      <img
                        src={pagoMisCuentasLogo}
                        alt="Pago Mis Cuentas"
                        className="h-7 w-auto"
                      />
                      <div className="leading-tight">
                        <div className="text-[11px] uppercase tracking-wider text-slate-500">Identificador</div>
                        <div className="font-mono font-semibold text-slate-800">
                          {meta.codClient.padStart(8, '0')}
                        </div>
                      </div>
                    </div>
                  )}
                </div>
                <div className="md:col-span-3 md:text-right">
                  <div className="text-slate-600 text-sm">Total a pagar</div>
                  <div className="text-2xl font-bold text-blue-600">{formatCurrency(totalSeleccionado)}</div>
                </div>
                <div className="md:col-span-4 md:text-right mt-2 md:mt-0 flex flex-col gap-2">
                  <button
                    className="btn-accent btn-lg w-full"
                    disabled={selectedIds.size === 0 || paying !== null}
                    onClick={handlePagar}
                  >
                    {paying === 'mp' ? (
                      <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-2"></span>Generando...</>
                    ) : (
                      <span className="flex items-center justify-center gap-2"><CreditCard className="w-5 h-5" />Pagar con Mercado Pago</span>
                    )}
                  </button>
                  <button
                    className="btn-outline-primary btn-lg w-full"
                    disabled={selectedIds.size === 0 || paying !== null}
                    onClick={handlePagarPagoFacil}
                  >
                    {paying === 'pf' ? (
                      <><span className="animate-spin w-4 h-4 border-2 border-current border-t-transparent rounded-full inline-block mr-2"></span>Generando cupón...</>
                    ) : (
                      <span className="flex items-center justify-center gap-2"><Barcode className="w-5 h-5" />Pagar con Pago Fácil</span>
                    )}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  )
}

export default ResumenCuentaPage
