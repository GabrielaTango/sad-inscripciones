import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import DataTable from '../../components/Admin/DataTable'
import ConfirmDialog from '../../components/Admin/ConfirmDialog'
import { eventosService } from '../../services/eventosService'
import { tiposEventoService } from '../../services/tiposEventoService'
import type { Evento, TipoEvento } from '../../types/models'

const EventosAdminPage = () => {
  const navigate = useNavigate()
  const [data, setData] = useState<Evento[]>([])
  const [tiposEvento, setTiposEvento] = useState<TipoEvento[]>([])
  const [loading, setLoading] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [deleteId, setDeleteId] = useState<number | null>(null)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    const [eventos, tipos] = await Promise.all([eventosService.getAll(), tiposEventoService.getAll()])
    setData(eventos); setTiposEvento(tipos)
  }, [])

  useEffect(() => { load() }, [load])

  const tipoNombre = (tipoId: number) => tiposEvento.find(t => t.id === tipoId)?.nombre || '-'
  const formatDate = (d: string) => d ? new Date(d).toLocaleDateString('es-AR') : '-'

  const openEdit = (item: Evento) => { navigate(`/admin/eventos/${item.id}`) }
  const openDelete = (item: Evento) => { setDeleteId(item.id); setShowConfirm(true) }

  const handleDelete = async () => {
    if (!deleteId) return
    setLoading(true)
    try { await eventosService.remove(deleteId); setShowConfirm(false); await load() }
    catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setLoading(false) }
  }

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'titulo', label: 'Titulo' },
    { key: 'tipoEventoId', label: 'Tipo', render: (item: Evento) => tipoNombre(item.tipoEventoId) },
    { key: 'modalidad', label: 'Modalidad' },
    { key: 'fechaInicio', label: 'Inicio', render: (item: Evento) => formatDate(item.fechaInicio) },
    { key: 'activo', label: 'Activo', render: (item: Evento) => item.activo ? <span className="badge bg-green-100 text-green-700">Si</span> : <span className="badge bg-gray-100 text-gray-700">No</span> },
  ]

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="font-bold text-slate-800">Eventos</h2>
        <button className="btn-primary" onClick={() => navigate('/admin/eventos/nuevo')}><Plus className="inline mr-1" size={16} />Nuevo</button>
      </div>

      {error && <div className="alert-danger">{error}</div>}

      <DataTable
        data={data as unknown as Record<string, unknown>[]}
        columns={columns as never}
        onEdit={openEdit as never}
        onDelete={openDelete as never}
      />

      <ConfirmDialog show={showConfirm} title="Eliminar Evento" message="Esta seguro que desea eliminar este evento?" onConfirm={handleDelete} onCancel={() => setShowConfirm(false)} loading={loading} />
    </div>
  )
}

export default EventosAdminPage
