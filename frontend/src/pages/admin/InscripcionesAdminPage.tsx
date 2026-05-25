import { useState, useEffect, useCallback, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { Check, Bookmark, X, RefreshCw, Receipt, Download } from 'lucide-react'
import DataTable from '../../components/Admin/DataTable'
import ConfirmDialog from '../../components/Admin/ConfirmDialog'
import { inscripcionesService } from '../../services/inscripcionesService'
import { eventosService } from '../../services/eventosService'
import type { Inscripcion, Evento } from '../../types/models'

const InscripcionesAdminPage = () => {
  const navigate = useNavigate()
  const [data, setData] = useState<Inscripcion[]>([])
  const [eventos, setEventos] = useState<Evento[]>([])
  const [eventoFilter, setEventoFilter] = useState<number | ''>('')
  const [estadoFilter, setEstadoFilter] = useState<string>('')
  const [showConfirm, setShowConfirm] = useState(false)
  const [deleteId, setDeleteId] = useState<number | null>(null)
  const [loading, setLoading] = useState(false)
  const [verifyingId, setVerifyingId] = useState<number | null>(null)
  const [verifyResult, setVerifyResult] = useState<string>('')
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    const evs = await eventosService.getAll()
    setEventos(evs)
    const inscs = eventoFilter ? await inscripcionesService.getByEventoId(Number(eventoFilter)) : await inscripcionesService.getAll()
    setData(inscs)
  }, [eventoFilter])

  useEffect(() => { load() }, [load])

  // Filtrado client-side por estado sobre lo que ya cargó el filtro de evento.
  const displayData = useMemo(
    () => estadoFilter ? data.filter(i => i.estado === estadoFilter) : data,
    [data, estadoFilter])

  const eventoTitulo = (id: number) => eventos.find(e => e.id === id)?.titulo || '-'

  const handleEstado = async (id: number, estado: string) => {
    try { await inscripcionesService.updateEstado(id, estado); await load() }
    catch (err) { setError(err instanceof Error ? err.message : 'Error') }
  }

  const handleVerificarMP = async (id: number) => {
    setVerifyingId(id); setError(''); setVerifyResult('')
    try {
      const r = await inscripcionesService.verificarMP(id)
      const msg = r.cambio
        ? `Inscripcion ${id}: ${r.estadoAnterior} → ${r.estadoNuevo} (aprobado: $${r.montoAprobado.toFixed(2)}, ${r.pagosEncontrados} pagos MP, ${r.pagosNuevos} nuevos)`
        : `Inscripcion ${id}: sin cambios (estado ${r.estadoNuevo}, aprobado $${r.montoAprobado.toFixed(2)}, ${r.pagosEncontrados} pagos MP)`
      setVerifyResult(msg)
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error verificando MP')
    } finally {
      setVerifyingId(null)
    }
  }

  const handleDelete = async () => {
    if (!deleteId) return; setLoading(true)
    try { await inscripcionesService.remove(deleteId); setShowConfirm(false); await load() }
    catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setLoading(false) }
  }

  const [exporting, setExporting] = useState(false)

  const handleExport = async () => {
    if (displayData.length === 0 || exporting) return
    setExporting(true)
    setError('')
    try {
      await inscripcionesService.exportExcel(
        eventoFilter ? Number(eventoFilter) : undefined,
        estadoFilter || undefined)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al exportar')
    } finally {
      setExporting(false)
    }
  }

  const estadoBadge = (estado: string) => {
    const map: Record<string, string> = { Pendiente: 'bg-amber-100 text-amber-700', Reservada: 'bg-blue-100 text-blue-700', Confirmada: 'bg-green-100 text-green-700', Cancelada: 'bg-red-100 text-red-700', Rechazada: 'bg-gray-100 text-gray-700' }
    return map[estado] || 'bg-blue-100 text-blue-700'
  }

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'eventoId', label: 'Evento', render: (i: Inscripcion) => eventoTitulo(i.eventoId) },
    { key: 'nombre', label: 'Nombre', render: (i: Inscripcion) => `${i.nombre} ${i.apellido}` },
    { key: 'documento', label: 'Documento', render: (i: Inscripcion) => i.documento || '-' },
    { key: 'email', label: 'Email' },
    { key: 'precioFinal', label: 'Precio', render: (i: Inscripcion) => `$${i.precioFinal.toFixed(2)}` },
    { key: 'estado', label: 'Estado', render: (i: Inscripcion) => <span className={`badge ${estadoBadge(i.estado)}`}>{i.estado}</span> },
    { key: 'fechaInscripcion', label: 'Fecha', render: (i: Inscripcion) => new Date(i.fechaInscripcion).toLocaleDateString('es-AR') },
    { key: 'sincronizadoTango', label: 'Tango', render: (i: Inscripcion) => i.sincronizadoTango ? <span className="badge bg-green-100 text-green-700">Si</span> : <span className="badge bg-gray-100 text-gray-700">No</span> },
  ]

  return (
    <div>
      <h2 className="font-bold text-slate-800 mb-4">Inscripciones</h2>
      {error && <div className="alert-danger">{error}</div>}
      {verifyResult && (
        <div className="alert-info flex items-center justify-between">
          <span>{verifyResult}</span>
          <button className="ml-4 p-1 hover:opacity-70" onClick={() => setVerifyResult('')}><X className="w-4 h-4" /></button>
        </div>
      )}

      <div className="mb-3 flex items-center gap-2 flex-wrap">
        <select className="form-select w-auto" value={eventoFilter} onChange={e => setEventoFilter(e.target.value ? Number(e.target.value) : '')}>
          <option value="">Todos los eventos</option>
          {eventos.map(e => <option key={e.id} value={e.id}>{e.titulo}</option>)}
        </select>
        <select className="form-select w-auto" value={estadoFilter} onChange={e => setEstadoFilter(e.target.value)}>
          <option value="">Todos los estados</option>
          <option value="Pendiente">Pendiente</option>
          <option value="Reservada">Reservada</option>
          <option value="Confirmada">Confirmada</option>
          <option value="Cancelada">Cancelada</option>
          <option value="Rechazada">Rechazada</option>
        </select>
        <button
          type="button"
          className="btn-accent flex items-center gap-2 disabled:opacity-60"
          onClick={handleExport}
          disabled={displayData.length === 0 || exporting}
          title="Descargar inscripciones filtradas a Excel"
        >
          <Download className={`w-4 h-4 ${exporting ? 'animate-pulse' : ''}`} />
          {exporting ? 'Exportando...' : `Exportar a Excel (${displayData.length})`}
        </button>
      </div>

      <DataTable
        data={displayData as unknown as Record<string, unknown>[]}
        columns={columns as never}
        onDelete={((item: Inscripcion) => { setDeleteId(item.id); setShowConfirm(true) }) as never}
        actions={(item: unknown) => {
          const insc = item as Inscripcion
          return (
            <>
              <button
                className="btn-outline-info btn-sm p-1.5"
                onClick={() => navigate(`/admin/pagos?inscripcionId=${insc.id}`)}
                title="Ver pagos de esta inscripcion"
              >
                <Receipt className="w-4 h-4" />
              </button>
              {(insc.estado === 'Pendiente' || insc.estado === 'Reservada') && (
                <button
                  className="btn-outline-secondary btn-sm p-1.5"
                  disabled={verifyingId !== null}
                  onClick={() => handleVerificarMP(insc.id)}
                  title="Verificar pago en Mercado Pago"
                >
                  {verifyingId === insc.id
                    ? <span className="animate-spin w-4 h-4 border-2 border-current border-t-transparent rounded-full inline-block"></span>
                    : <RefreshCw className="w-4 h-4" />}
                </button>
              )}
              {insc.estado !== 'Confirmada' && (
                <button className="btn-outline-success btn-sm p-1.5" onClick={() => handleEstado(insc.id, 'Confirmada')} title="Confirmar"><Check className="w-4 h-4" /></button>
              )}
              {insc.estado !== 'Reservada' && (
                <button className="btn-outline-primary btn-sm p-1.5" onClick={() => handleEstado(insc.id, 'Reservada')} title="Reservar"><Bookmark className="w-4 h-4" /></button>
              )}
              {insc.estado !== 'Cancelada' && (
                <button className="inline-flex items-center justify-center p-1.5 border border-amber-500 bg-white text-amber-500 rounded-lg text-sm hover:bg-amber-500 hover:text-white transition-colors" onClick={() => handleEstado(insc.id, 'Cancelada')} title="Cancelar"><X className="w-4 h-4" /></button>
              )}
            </>
          )
        }}
      />

      <ConfirmDialog show={showConfirm} title="Eliminar Inscripcion" message="Esta seguro que desea eliminar esta inscripcion?" onConfirm={handleDelete} onCancel={() => setShowConfirm(false)} loading={loading} />
    </div>
  )
}

export default InscripcionesAdminPage
