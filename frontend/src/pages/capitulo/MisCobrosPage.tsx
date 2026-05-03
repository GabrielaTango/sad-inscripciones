import { useEffect, useState } from 'react'
import { Receipt, RefreshCw, AlertTriangle, CheckCircle, Clock } from 'lucide-react'
import { capituloService } from '../../services/capituloService'
import type { CobroCapitulo } from '../../services/capituloService'

const formatDate = (d?: string) => (d ? new Date(d).toLocaleString('es-AR') : '-')
const formatCurrency = (n: number) => `$${n.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`

const parseComprobantes = (json?: string | null): { tComp: string; nComp: string }[] => {
  if (!json) return []
  try {
    const arr = JSON.parse(json) as { tComp?: string; nComp?: string; TComp?: string; NComp?: string }[]
    return arr.map(x => ({ tComp: (x.tComp ?? x.TComp ?? '').trim(), nComp: (x.nComp ?? x.NComp ?? '').trim() }))
  } catch {
    return []
  }
}

const MisCobrosPage = () => {
  const [cobros, setCobros] = useState<CobroCapitulo[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const cargar = async () => {
    setLoading(true)
    setError('')
    try {
      setCobros(await capituloService.listarCobros())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al cargar cobros')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { cargar() }, [])

  return (
    <div>
      <div className="flex items-center gap-3 mb-4">
        <Receipt className="w-6 h-6 text-blue-600" />
        <h2 className="font-bold text-slate-800 text-2xl">Mis cobros</h2>
        <button className="btn-secondary btn-sm ml-auto" onClick={cargar} disabled={loading}>
          <RefreshCw className={`w-4 h-4 inline mr-1 ${loading ? 'animate-spin' : ''}`} />Refrescar
        </button>
      </div>

      {error && (
        <div className="alert-danger flex items-center gap-2 mb-4">
          <AlertTriangle className="w-5 h-5" />{error}
        </div>
      )}

      <div className="card rounded-2xl border-slate-200">
        <div className="overflow-x-auto rounded-2xl border border-slate-200">
          <table className="w-full text-sm">
            <thead className="bg-slate-800 text-white">
              <tr>
                <th className="p-3 text-left">Fecha</th>
                <th className="p-3 text-left">CUIT</th>
                <th className="p-3 text-left">Medio</th>
                <th className="p-3 text-right">Monto</th>
                <th className="p-3 text-left">Comprobantes</th>
                <th className="p-3 text-center">Estado</th>
                <th className="p-3 text-center">Tango</th>
              </tr>
            </thead>
            <tbody>
              {!loading && cobros.length === 0 && (
                <tr><td colSpan={7} className="p-6 text-center text-slate-500">Sin cobros registrados.</td></tr>
              )}
              {cobros.map(c => {
                const comps = parseComprobantes(c.comprobantes)
                return (
                  <tr key={c.id} className="border-b border-slate-100 hover:bg-slate-50">
                    <td className="p-3 whitespace-nowrap">{formatDate(c.createdAt)}</td>
                    <td className="p-3 font-mono">{c.cuit}</td>
                    <td className="p-3">{c.medioPago ?? '-'}</td>
                    <td className="p-3 text-right font-bold">{formatCurrency(c.monto)}</td>
                    <td className="p-3">
                      {comps.length === 0
                        ? <span className="badge bg-amber-100 text-amber-800">A cuenta</span>
                        : comps.map((cp, i) => (
                            <span key={i} className="badge bg-slate-100 text-slate-700 mr-1">{cp.tComp} {cp.nComp}</span>
                          ))}
                    </td>
                    <td className="p-3 text-center">
                      {c.estadoPago === 'Aprobado'
                        ? <span className="text-emerald-600 inline-flex items-center gap-1"><CheckCircle className="w-4 h-4" />{c.estadoPago}</span>
                        : <span className="text-slate-500 inline-flex items-center gap-1"><Clock className="w-4 h-4" />{c.estadoPago}</span>}
                    </td>
                    <td className="p-3 text-center">
                      {c.sincronizadoTango
                        ? <span className="text-emerald-600">OK</span>
                        : <span className="text-slate-400">Pendiente</span>}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

export default MisCobrosPage
