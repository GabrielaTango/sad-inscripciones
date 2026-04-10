import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, List } from 'lucide-react'
import DataTable from '../../components/Admin/DataTable'
import FormModal from '../../components/Admin/FormModal'
import ConfirmDialog from '../../components/Admin/ConfirmDialog'
import { becaEventosService } from '../../services/becaEventosService'
import { eventosService } from '../../services/eventosService'
import type { BecaEvento, BecaEventoForm, Evento } from '../../types/models'

const emptyForm: BecaEventoForm = { eventoId: 0, nombreCampana: '', tipoDescuento: 'Porcentaje', valor: 0, cantidadTotalCodigos: 1, acumulable: false, activo: true }

const BecaEventosAdminPage = () => {
  const navigate = useNavigate()
  const [data, setData] = useState<BecaEvento[]>([])
  const [eventos, setEventos] = useState<Evento[]>([])
  const [loading, setLoading] = useState(false)
  const [showForm, setShowForm] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [deleteId, setDeleteId] = useState<number | null>(null)
  const [form, setForm] = useState<BecaEventoForm>(emptyForm)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    const [becas, evs] = await Promise.all([becaEventosService.getAll(), eventosService.getAll()])
    setData(becas); setEventos(evs)
  }, [])

  useEffect(() => { load() }, [load])

  const eventoTitulo = (id: number) => eventos.find(e => e.id === id)?.titulo || '-'

  const openCreate = () => { setForm(emptyForm); setEditId(null); setError(''); setShowForm(true) }
  const openEdit = (item: BecaEvento) => {
    setForm({ eventoId: item.eventoId, nombreCampana: item.nombreCampana, tipoDescuento: item.tipoDescuento, valor: item.valor, cantidadTotalCodigos: item.cantidadTotalCodigos, fechaVencimiento: item.fechaVencimiento?.slice(0, 16), acumulable: item.acumulable, activo: item.activo })
    setEditId(item.id); setError(''); setShowForm(true)
  }
  const openDelete = (item: BecaEvento) => { setDeleteId(item.id); setShowConfirm(true) }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setLoading(true); setError('')
    try {
      if (editId) await becaEventosService.update(editId, form)
      else await becaEventosService.create(form)
      setShowForm(false); await load()
    } catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setLoading(false) }
  }

  const handleDelete = async () => {
    if (!deleteId) return; setLoading(true)
    try { await becaEventosService.remove(deleteId); setShowConfirm(false); await load() }
    catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setLoading(false) }
  }

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'eventoId', label: 'Evento', render: (i: BecaEvento) => eventoTitulo(i.eventoId) },
    { key: 'nombreCampana', label: 'Campana' },
    { key: 'tipoDescuento', label: 'Tipo' },
    { key: 'valor', label: 'Valor' },
    { key: 'cantidadTotalCodigos', label: 'Codigos' },
    { key: 'activo', label: 'Activo', render: (i: BecaEvento) => i.activo ? <span className="badge bg-green-100 text-green-700">Si</span> : <span className="badge bg-gray-100 text-gray-700">No</span> },
  ]

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="font-bold text-slate-800">Campanas de Beca</h2>
        <button className="btn-primary" onClick={openCreate}><Plus className="w-4 h-4 mr-1 inline" />Nueva Campana</button>
      </div>

      <DataTable
        data={data as unknown as Record<string, unknown>[]}
        columns={columns as never}
        onEdit={openEdit as never}
        onDelete={openDelete as never}
        actions={(item: unknown) => (
          <button className="btn-outline-primary btn-sm p-1.5" onClick={() => navigate(`/admin/becas/${(item as BecaEvento).id}/codigos`)} title="Ver Codigos"><List className="w-4 h-4" /></button>
        )}
      />

      <FormModal show={showForm} title={editId ? 'Editar Campana' : 'Nueva Campana de Beca'} onClose={() => setShowForm(false)} onSubmit={handleSubmit} loading={loading} size="lg">
        {error && <div className="alert-danger">{error}</div>}
        <div className="grid grid-cols-1 md:grid-cols-12 gap-4">
          <div className="md:col-span-6"><label className="form-label">Evento *</label>
            <select className="form-select" value={form.eventoId} onChange={e => setForm({ ...form, eventoId: Number(e.target.value) })} required>
              <option value={0}>Seleccionar...</option>{eventos.map(e => <option key={e.id} value={e.id}>{e.titulo}</option>)}
            </select></div>
          <div className="md:col-span-6"><label className="form-label">Nombre Campana *</label><input type="text" className="form-input" value={form.nombreCampana} onChange={e => setForm({ ...form, nombreCampana: e.target.value })} required /></div>
          <div className="md:col-span-4"><label className="form-label">Tipo Descuento *</label>
            <select className="form-select" value={form.tipoDescuento} onChange={e => setForm({ ...form, tipoDescuento: e.target.value })} required>
              <option value="Porcentaje">Porcentaje</option><option value="MontoFijo">Monto Fijo</option>
            </select></div>
          <div className="md:col-span-4"><label className="form-label">Valor *</label><input type="number" step="0.01" className="form-input" value={form.valor} onChange={e => setForm({ ...form, valor: Number(e.target.value) })} required /></div>
          <div className="md:col-span-4"><label className="form-label">Cantidad Codigos *</label><input type="number" className="form-input" value={form.cantidadTotalCodigos} onChange={e => setForm({ ...form, cantidadTotalCodigos: Number(e.target.value) })} required disabled={!!editId} /></div>
          <div className="md:col-span-6"><label className="form-label">Vencimiento</label><input type="datetime-local" className="form-input" value={form.fechaVencimiento || ''} onChange={e => setForm({ ...form, fechaVencimiento: e.target.value || undefined })} /></div>
          <div className="md:col-span-3"><div className="form-check mt-4"><input type="checkbox" className="form-check-input" checked={form.acumulable} onChange={e => setForm({ ...form, acumulable: e.target.checked })} /><label className="text-sm">Acumulable</label></div></div>
          <div className="md:col-span-3"><div className="form-check mt-4"><input type="checkbox" className="form-check-input" checked={form.activo} onChange={e => setForm({ ...form, activo: e.target.checked })} /><label className="text-sm">Activo</label></div></div>
        </div>
      </FormModal>

      <ConfirmDialog show={showConfirm} title="Eliminar Campana" message="Esta seguro que desea eliminar esta campana de beca?" onConfirm={handleDelete} onCancel={() => setShowConfirm(false)} loading={loading} />
    </div>
  )
}

export default BecaEventosAdminPage
