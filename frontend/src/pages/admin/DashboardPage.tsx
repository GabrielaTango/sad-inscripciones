import { useState, useEffect, useCallback } from 'react'
import { dashboardService } from '../../services/dashboardService'
import type { DashboardStats } from '../../services/dashboardService'
import { syncService } from '../../services/syncService'
import type { SyncTriggerStatus } from '../../services/syncService'
import { UserCheck, CalendarCheck, TrendingUp, CalendarDays, BarChart3, CreditCard, Award, RefreshCw } from 'lucide-react'

const DashboardPage = () => {
  const [stats, setStats] = useState<DashboardStats | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [syncing, setSyncing] = useState(false)
  const [syncStatus, setSyncStatus] = useState<SyncTriggerStatus | null>(null)
  const [syncMessage, setSyncMessage] = useState<{ kind: 'success' | 'danger'; text: string } | null>(null)

  const loadSyncStatus = useCallback(async () => {
    try {
      setSyncStatus(await syncService.getTriggerStatus())
    } catch {
      // silencioso — no debe romper el dashboard
    }
  }, [])

  const handleTriggerSync = async () => {
    if (syncing) return
    if (!window.confirm('Esto va a forzar una sincronización completa con Tango. ¿Continuar?')) return
    setSyncing(true)
    setSyncMessage(null)
    try {
      await syncService.triggerFullSync()
      setSyncMessage({ kind: 'success', text: 'Sincronización solicitada. El SyncService la ejecutará en los próximos segundos.' })
      await loadSyncStatus()
    } catch (err) {
      setSyncMessage({ kind: 'danger', text: err instanceof Error ? err.message : 'Error solicitando sincronización' })
    } finally {
      setSyncing(false)
    }
  }

  useEffect(() => { loadSyncStatus() }, [loadSyncStatus])

  const formatDate = (d: string | null) => d ? new Date(d).toLocaleString('es-AR') : '-'

  const load = useCallback(async () => {
    setLoading(true)
    try {
      setStats(await dashboardService.getStats())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al cargar estadisticas')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  if (loading) {
    return (
      <div className="text-center py-10">
        <div className="inline-block animate-spin w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full" role="status">
          <span className="sr-only">Cargando...</span>
        </div>
      </div>
    )
  }

  if (error) {
    return <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">{error}</div>
  }

  if (!stats) return null

  const estadoColor = (estado: string): { badge: string; border: string } => {
    const map: Record<string, { badge: string; border: string }> = {
      Pendiente: { badge: 'bg-amber-100 text-amber-700', border: 'border-amber-500' },
      Confirmada: { badge: 'bg-green-100 text-green-700', border: 'border-green-500' },
      Cancelada: { badge: 'bg-red-100 text-red-700', border: 'border-red-500' },
    }
    return map[estado] || { badge: 'bg-gray-100 text-gray-700', border: 'border-gray-500' }
  }

  const estadoPagoColor = (estado: string): { badge: string; border: string } => {
    const map: Record<string, { badge: string; border: string }> = {
      Pendiente: { badge: 'bg-amber-100 text-amber-700', border: 'border-amber-500' },
      Confirmado: { badge: 'bg-green-100 text-green-700', border: 'border-green-500' },
      Rechazado: { badge: 'bg-red-100 text-red-700', border: 'border-red-500' },
    }
    return map[estado] || { badge: 'bg-gray-100 text-gray-700', border: 'border-gray-500' }
  }

  const totalBecas = stats.becasUsadas + stats.becasDisponibles

  return (
    <div>
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3 mb-4">
        <h2 className="font-bold text-slate-800">Dashboard</h2>
        <div className="flex flex-col items-start md:items-end gap-1">
          <button
            type="button"
            onClick={handleTriggerSync}
            disabled={syncing}
            className="btn-accent flex items-center gap-2 disabled:opacity-60"
          >
            <RefreshCw className={`w-4 h-4 ${syncing ? 'animate-spin' : ''}`} />
            {syncing ? 'Solicitando...' : 'Sincronizar Tango'}
          </button>
          {syncStatus?.requestedAt && !syncStatus?.consumedAt && (
            <span className="text-xs text-amber-700">
              Pendiente desde {formatDate(syncStatus.requestedAt)}
            </span>
          )}
          {syncStatus?.consumedAt && !syncStatus?.requestedAt && (
            <span className="text-xs text-slate-500">
              Última: {formatDate(syncStatus.consumedAt)}
            </span>
          )}
        </div>
      </div>

      {syncMessage && (
        <div
          className={`mb-4 px-4 py-3 rounded border ${syncMessage.kind === 'success'
            ? 'bg-green-50 border-green-300 text-green-800'
            : 'bg-red-50 border-red-300 text-red-800'}`}
        >
          {syncMessage.text}
        </div>
      )}

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-4">
        <div className="card h-full">
          <div className="p-4">
            <div className="flex items-center">
              <div className="w-12 h-12 rounded-full bg-blue-100 flex items-center justify-center mr-3">
                <UserCheck className="w-6 h-6 text-slate-800" />
              </div>
              <div>
                <div className="text-slate-500 text-sm">Total Inscripciones</div>
                <div className="text-3xl font-bold">{stats.totalInscripciones}</div>
              </div>
            </div>
          </div>
        </div>
        <div className="card h-full">
          <div className="p-4">
            <div className="flex items-center">
              <div className="w-12 h-12 rounded-full bg-green-500/10 flex items-center justify-center mr-3">
                <CalendarCheck className="w-6 h-6 text-green-500" />
              </div>
              <div>
                <div className="text-slate-500 text-sm">Hoy</div>
                <div className="text-3xl font-bold">{stats.inscripcionesHoy}</div>
              </div>
            </div>
          </div>
        </div>
        <div className="card h-full">
          <div className="p-4">
            <div className="flex items-center">
              <div className="w-12 h-12 rounded-full bg-blue-500/10 flex items-center justify-center mr-3">
                <TrendingUp className="w-6 h-6 text-blue-500" />
              </div>
              <div>
                <div className="text-slate-500 text-sm">Esta Semana</div>
                <div className="text-3xl font-bold">{stats.inscripcionesSemana}</div>
              </div>
            </div>
          </div>
        </div>
        <div className="card h-full">
          <div className="p-4">
            <div className="flex items-center">
              <div className="w-12 h-12 rounded-full bg-blue-100 flex items-center justify-center mr-3">
                <CalendarDays className="w-6 h-6 text-slate-800" />
              </div>
              <div>
                <div className="text-slate-500 text-sm">Eventos Activos</div>
                <div className="text-3xl font-bold">{stats.totalEventosActivos}</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Inscripciones por Estado */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
        {stats.inscripcionesPorEstado.map((e) => {
          const colors = estadoColor(e.estado)
          return (
            <div key={e.estado} className={`card border-l-4 ${colors.border}`}>
              <div className="p-4">
                <div className="flex justify-between items-center">
                  <div>
                    <div className="text-slate-500 text-sm">{e.estado}</div>
                    <div className="text-3xl font-bold">{e.cantidad}</div>
                  </div>
                  <span className={`badge ${colors.badge} text-base px-3 py-1 rounded-full`}>{e.estado}</span>
                </div>
              </div>
            </div>
          )
        })}
      </div>

      {/* Inscripciones por Evento */}
      <div className="card mb-4">
        <div className="px-4 pt-4">
          <h5 className="font-bold mb-0 text-slate-800 flex items-center">
            <BarChart3 className="w-5 h-5 mr-2" />Inscripciones por Evento
          </h5>
        </div>
        <div className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left">Evento</th>
                  <th className="px-4 py-3 text-center">Total</th>
                  <th className="px-4 py-3 text-center">Confirmadas</th>
                  <th className="px-4 py-3 text-center">Pendientes</th>
                  <th className="px-4 py-3 text-center">Canceladas</th>
                </tr>
              </thead>
              <tbody>
                {stats.inscripcionesPorEvento.map((e) => (
                  <tr key={e.eventoId} className="border-t hover:bg-gray-50">
                    <td className="px-4 py-3">{e.titulo}</td>
                    <td className="px-4 py-3 text-center font-bold">{e.total}</td>
                    <td className="px-4 py-3 text-center"><span className="badge bg-green-100 text-green-700 px-2 py-1 rounded-full text-xs">{e.confirmadas}</span></td>
                    <td className="px-4 py-3 text-center"><span className="badge bg-amber-100 text-amber-700 px-2 py-1 rounded-full text-xs">{e.pendientes}</span></td>
                    <td className="px-4 py-3 text-center"><span className="badge bg-red-100 text-red-700 px-2 py-1 rounded-full text-xs">{e.canceladas}</span></td>
                  </tr>
                ))}
                {stats.inscripcionesPorEvento.length === 0 && (
                  <tr><td colSpan={5} className="text-center text-slate-500 py-3">Sin datos</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-4">
        {/* Pagos por Estado */}
        <div className="lg:col-span-2">
          <div className="card h-full">
            <div className="px-4 pt-4">
              <h5 className="font-bold mb-0 text-slate-800 flex items-center">
                <CreditCard className="w-5 h-5 mr-2" />Pagos por Estado
              </h5>
            </div>
            <div className="p-4">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {stats.pagosPorEstado.map((p) => {
                  const colors = estadoPagoColor(p.estadoPago)
                  return (
                    <div key={p.estadoPago} className={`card border-l-4 ${colors.border}`}>
                      <div className="p-4 text-center">
                        <div className="text-slate-500 text-sm">{p.estadoPago}</div>
                        <div className="text-2xl font-bold">{p.cantidad}</div>
                        <div className="text-slate-500">${p.monto.toLocaleString('es-AR', { minimumFractionDigits: 2 })}</div>
                      </div>
                    </div>
                  )
                })}
                {stats.pagosPorEstado.length === 0 && (
                  <div className="col-span-full text-center text-slate-500 py-3">Sin datos</div>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Becas */}
        <div>
          <div className="card h-full">
            <div className="px-4 pt-4">
              <h5 className="font-bold mb-0 text-slate-800 flex items-center">
                <Award className="w-5 h-5 mr-2" />Becas
              </h5>
            </div>
            <div className="p-4">
              <div className="flex justify-around text-center mb-3">
                <div>
                  <div className="text-slate-500 text-sm">Usadas</div>
                  <div className="text-3xl font-bold text-red-500">{stats.becasUsadas}</div>
                </div>
                <div>
                  <div className="text-slate-500 text-sm">Disponibles</div>
                  <div className="text-3xl font-bold text-green-500">{stats.becasDisponibles}</div>
                </div>
              </div>
              {totalBecas > 0 && (
                <div className="h-2.5 rounded-full bg-gray-200 overflow-hidden flex">
                  <div
                    className="bg-red-500 h-full"
                    style={{ width: `${(stats.becasUsadas / totalBecas) * 100}%` }}
                  ></div>
                  <div
                    className="bg-green-500 h-full"
                    style={{ width: `${(stats.becasDisponibles / totalBecas) * 100}%` }}
                  ></div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default DashboardPage
