import { useState, useEffect, useMemo, useCallback } from 'react'
import { useAuth } from '../context/AuthContext'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { resumenCuentaService } from '../services/resumenCuentaService'
import type { PagoCuentaCorriente } from '../services/resumenCuentaService'
import type { ResumenCuenta } from '../types/models'
import {
  Receipt,
  CheckCircle,
  XCircle,
  Clock,
  Hourglass,
  Check,
  CheckSquare,
  CreditCard,
  Info,
  X,
} from 'lucide-react'

const ResumenCuentaPage = () => {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const [movimientos, setMovimientos] = useState<ResumenCuenta[]>([])
  const [pagos, setPagos] = useState<PagoCuentaCorriente[]>([])
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set())
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [paying, setPaying] = useState(false)
  const [pagoResultado, setPagoResultado] = useState<{ status: string; message: string } | null>(null)

  const loadData = useCallback(async () => {
    try {
      const [mov, pag] = await Promise.all([
        resumenCuentaService.getByCuit(),
        resumenCuentaService.getPagos(),
      ])
      setMovimientos(mov)
      setPagos(pag)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al cargar resumen')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login')
      return
    }
    loadData()
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
                ? 'El pago fue registrado exitosamente. La imputacion se vera reflejada en su resumen de cuenta en las proximas horas.'
                : 'El pago esta siendo procesado. La imputacion se vera reflejada en su resumen de cuenta una vez confirmado.'
            })
            loadData()
          })
          .catch(() => {
            setPagoResultado({
              status: 'success',
              message: 'El pago fue registrado. La imputacion se vera reflejada en su resumen de cuenta en las proximas horas.'
            })
            loadData()
          })
      } else if (status === 'rejected') {
        setPagoResultado({ status: 'danger', message: 'El pago fue rechazado. Puede intentar nuevamente.' })
        loadData()
      } else {
        setPagoResultado({
          status: 'warning',
          message: 'El pago esta pendiente de confirmacion. La imputacion se vera reflejada una vez que se acredite.'
        })
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

  const pagosPendientes = useMemo(
    () => pagos.filter(p => p.estadoPago === 'Pendiente'),
    [pagos]
  )

  const pagosAprobados = useMemo(
    () => pagos.filter(p => p.estadoPago === 'Aprobado'),
    [pagos]
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
    setPaying(true)
    setError('')
    try {
      const result = await resumenCuentaService.generarPago(Array.from(selectedIds))
      if (result.initPoint) {
        window.location.href = result.initPoint
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al generar el pago')
      setPaying(false)
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

      {/* Pagos pendientes de imputacion */}
      {pagosPendientes.length > 0 && (
        <div className="alert-info mb-4">
          <h6 className="text-base font-bold flex items-center gap-2">
            <Hourglass className="w-5 h-5" />Pagos pendientes de imputacion
          </h6>
          <p className="mb-2 text-sm">Los siguientes pagos fueron registrados y se veran reflejados en su resumen de cuenta una vez procesada la imputacion.</p>
          {pagosPendientes.map(p => (
            <div key={p.id} className="flex justify-between items-center bg-white rounded-2xl px-3 py-2 mb-1">
              <span className="text-sm flex items-center gap-1">
                <Clock className="w-4 h-4" />
                {formatDate(p.createdAt)} - Ref: {p.externalReference}
              </span>
              <span className="font-bold text-blue-600">{formatCurrency(p.monto)}</span>
            </div>
          ))}
        </div>
      )}

      {/* Pagos aprobados recientes */}
      {pagosAprobados.length > 0 && (
        <div className="alert-success mb-4">
          <h6 className="text-base font-bold flex items-center gap-2">
            <CheckCircle className="w-5 h-5" />Pagos registrados
          </h6>
          <p className="mb-2 text-sm">Estos pagos fueron aprobados. La imputacion se vera reflejada en su resumen de cuenta en las proximas horas.</p>
          {pagosAprobados.slice(0, 5).map(p => (
            <div key={p.id} className="flex justify-between items-center bg-white rounded-2xl px-3 py-2 mb-1">
              <span className="text-sm flex items-center gap-1">
                <Check className="w-4 h-4 text-green-600" />
                {formatDate(p.fechaPago || p.createdAt)}
              </span>
              <span className="font-bold text-green-600">{formatCurrency(p.monto)}</span>
            </div>
          ))}
        </div>
      )}

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
                <div className="md:col-span-6">
                  <div className="text-slate-600 text-sm">
                    {selectedIds.size} comprobante{selectedIds.size !== 1 ? 's' : ''} seleccionado{selectedIds.size !== 1 ? 's' : ''}
                  </div>
                </div>
                <div className="md:col-span-3 md:text-right">
                  <div className="text-slate-600 text-sm">Total a pagar</div>
                  <div className="text-2xl font-bold text-blue-600">{formatCurrency(totalSeleccionado)}</div>
                </div>
                <div className="md:col-span-3 md:text-right mt-2 md:mt-0">
                  <button
                    className="btn-accent btn-lg w-full"
                    disabled={selectedIds.size === 0 || paying}
                    onClick={handlePagar}
                  >
                    {paying ? (
                      <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-2"></span>Generando...</>
                    ) : (
                      <span className="flex items-center justify-center gap-2"><CreditCard className="w-5 h-5" />Pagar</span>
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
