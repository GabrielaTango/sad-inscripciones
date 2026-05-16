import { useState, useEffect, useCallback } from 'react'
import { Search, Check, X, Barcode } from 'lucide-react'
import DataTable from '../../components/Admin/DataTable'
import { pagosCuentaCorrienteService } from '../../services/pagosCuentaCorrienteService'
import type { PagoCuentaCorriente } from '../../services/resumenCuentaService'
const PagosCuentaCorrienteAdminPage = () => {
  const [data, setData] = useState<PagoCuentaCorriente[]>([])
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [actionId, setActionId] = useState<number | null>(null)
  const [, setVerifyResult] = useState<{ id: number; result: unknown } | null>(null)

  const load = useCallback(async () => {
    try {
      setData(await pagosCuentaCorrienteService.getAll())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al cargar pagos')
    }
  }, [])

  useEffect(() => { load() }, [load])

  const handleAprobar = async (id: number) => {
    if (!confirm('Marcar este pago como Aprobado?')) return
    setActionId(id); setError(''); setSuccess('')
    try {
      await pagosCuentaCorrienteService.aprobar(id)
      setSuccess('Pago marcado como aprobado')
      await load()
    } catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setActionId(null) }
  }

  const handleRechazar = async (id: number) => {
    if (!confirm('Marcar este pago como Rechazado?')) return
    setActionId(id); setError(''); setSuccess('')
    try {
      await pagosCuentaCorrienteService.rechazar(id)
      setSuccess('Pago marcado como rechazado')
      await load()
    } catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setActionId(null) }
  }

  const handleVerificar = async (id: number) => {
    setActionId(id); setError(''); setSuccess(''); setVerifyResult(null)
    try {
      const result = await pagosCuentaCorrienteService.verificarMP(id)
      setVerifyResult({ id, result })
      if (result.encontrado) {
        setSuccess(`MP verificado: estado=${result.estadoPago}, paymentId=${result.mpPaymentId}`)
        await load()
      } else {
        setError(result.message || 'No se encontro pago en Mercado Pago')
      }
    } catch (err) { setError(err instanceof Error ? err.message : 'Error al verificar') }
    finally { setActionId(null) }
  }

  const formatDate = (d?: string) => d ? new Date(d).toLocaleDateString('es-AR') : '-'
  const formatCurrency = (n: number) => `$${n.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`

  const estadoBadge = (estado: string) => {
    const map: Record<string, string> = { Pendiente: 'bg-amber-100 text-amber-700', Aprobado: 'bg-green-100 text-green-700', Rechazado: 'bg-red-100 text-red-700' }
    return map[estado] || 'bg-blue-100 text-blue-700'
  }

  // Mostramos "MercadoPago" cuando el campo es null por compatibilidad histórica
  // (todos los pagos previos a la migración de medios eran MP).
  const medioLabel = (m?: string | null) => m && m.length > 0 ? m : 'MercadoPago'
  const medioBadge = (m?: string | null) => {
    if (m === 'PagoFacil') return 'bg-violet-100 text-violet-700'
    if (m === 'Contado' || m === 'Transferencia') return 'bg-slate-100 text-slate-700'
    return 'bg-blue-100 text-blue-700'
  }

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'cuit', label: 'CUIT' },
    { key: 'monto', label: 'Monto', render: (i: PagoCuentaCorriente) => formatCurrency(i.monto) },
    { key: 'medioPago', label: 'Medio', render: (i: PagoCuentaCorriente) => (
      <span className={`badge ${medioBadge(i.medioPago)}`}>{medioLabel(i.medioPago)}</span>
    )},
    { key: 'externalReference', label: 'Referencia', render: (i: PagoCuentaCorriente) => (
      <span className="text-sm">{i.externalReference}</span>
    )},
    { key: 'estadoPago', label: 'Estado', render: (i: PagoCuentaCorriente) => (
      <span className={`badge ${estadoBadge(i.estadoPago)}`}>{i.estadoPago}</span>
    )},
    { key: 'mpPaymentId', label: 'MP Payment ID', render: (i: PagoCuentaCorriente) => i.mpPaymentId || '-' },
    { key: 'createdAt', label: 'Fecha', render: (i: PagoCuentaCorriente) => formatDate(i.createdAt) },
    { key: 'fechaPago', label: 'Fecha Pago', render: (i: PagoCuentaCorriente) => formatDate(i.fechaPago) },
  ]

  return (
    <div>
      <h2 className="font-bold text-slate-800 mb-4">Pagos Cuenta Corriente</h2>

      {error && <div className="alert-danger flex justify-between items-center">
        {error}<button type="button" className="text-red-500 hover:text-red-700" onClick={() => setError('')}>&times;</button>
      </div>}
      {success && <div className="alert-success flex justify-between items-center">
        {success}<button type="button" className="text-green-500 hover:text-green-700" onClick={() => setSuccess('')}>&times;</button>
      </div>}

      <DataTable
        data={data as unknown as Record<string, unknown>[]}
        columns={columns as never}
        searchPlaceholder="Buscar por CUIT, referencia..."
        actions={(item: unknown) => {
          const pago = item as PagoCuentaCorriente
          const isLoading = actionId === pago.id
          const esPagoFacil = pago.medioPago === 'PagoFacil'
          return (
            <>
              {esPagoFacil ? (
                <a
                  className="btn-outline-primary btn-sm p-1.5"
                  href={`/resumen-cuenta/cupon-pagofacil/${pago.id}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  title="Ver cupón Pago Fácil"
                >
                  <Barcode size={16} />
                </a>
              ) : (
                <button
                  className="btn-outline-primary btn-sm p-1.5"
                  onClick={() => handleVerificar(pago.id)}
                  disabled={isLoading}
                  title="Verificar en Mercado Pago"
                >
                  {isLoading ? <span className="animate-spin w-4 h-4 border-2 border-current border-t-transparent rounded-full inline-block"></span> : <Search size={16} />}
                </button>
              )}
              {pago.estadoPago !== 'Aprobado' && (
                <button
                  className="btn-outline-success btn-sm p-1.5"
                  onClick={() => handleAprobar(pago.id)}
                  disabled={isLoading}
                  title="Aprobar manualmente"
                >
                  <Check size={16} />
                </button>
              )}
              {pago.estadoPago !== 'Rechazado' && (
                <button
                  className="btn-outline-danger btn-sm p-1.5"
                  onClick={() => handleRechazar(pago.id)}
                  disabled={isLoading}
                  title="Rechazar"
                >
                  <X size={16} />
                </button>
              )}
            </>
          )
        }}
      />
    </div>
  )
}

export default PagosCuentaCorrienteAdminPage
