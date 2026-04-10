import { useState, useEffect, useCallback } from 'react'
import { Plus } from 'lucide-react'
import DataTable from '../../components/Admin/DataTable'
import FormModal from '../../components/Admin/FormModal'
import ConfirmDialog from '../../components/Admin/ConfirmDialog'
import { tiposAlumnoService } from '../../services/tiposAlumnoService'
import type { TipoAlumno, TipoAlumnoForm } from '../../types/models'

const emptyForm: TipoAlumnoForm = { nombre: '', activo: true }

const TiposAlumnoPage = () => {
  const [data, setData] = useState<TipoAlumno[]>([])
  const [loading, setLoading] = useState(false)
  const [showForm, setShowForm] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [deleteId, setDeleteId] = useState<number | null>(null)
  const [form, setForm] = useState<TipoAlumnoForm>(emptyForm)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setData(await tiposAlumnoService.getAll())
  }, [])

  useEffect(() => { load() }, [load])

  const openCreate = () => { setForm(emptyForm); setEditId(null); setError(''); setShowForm(true) }
  const openEdit = (item: TipoAlumno) => { setForm({ nombre: item.nombre, activo: item.activo }); setEditId(item.id); setError(''); setShowForm(true) }
  const openDelete = (item: TipoAlumno) => { setDeleteId(item.id); setShowConfirm(true) }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true); setError('')
    try {
      if (editId) await tiposAlumnoService.update(editId, form)
      else await tiposAlumnoService.create(form)
      setShowForm(false); await load()
    } catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setLoading(false) }
  }

  const handleDelete = async () => {
    if (!deleteId) return
    setLoading(true)
    try { await tiposAlumnoService.remove(deleteId); setShowConfirm(false); await load() }
    catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setLoading(false) }
  }

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'nombre', label: 'Nombre' },
    { key: 'activo', label: 'Activo', render: (item: TipoAlumno) => item.activo ? <span className="badge bg-green-100 text-green-700">Si</span> : <span className="badge bg-gray-100 text-gray-700">No</span> },
  ]

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="font-bold text-slate-800">Tipos de Alumno</h2>
        <button className="btn-primary" onClick={openCreate}><Plus className="inline mr-1" size={16} />Nuevo</button>
      </div>

      <DataTable data={data as unknown as Record<string, unknown>[]} columns={columns as never} onEdit={openEdit as never} onDelete={openDelete as never} />

      <FormModal show={showForm} title={editId ? 'Editar Tipo de Alumno' : 'Nuevo Tipo de Alumno'} onClose={() => setShowForm(false)} onSubmit={handleSubmit} loading={loading}>
        {error && <div className="alert-danger">{error}</div>}
        <div className="mb-3">
          <label className="form-label">Nombre *</label>
          <input type="text" className="form-input" value={form.nombre} onChange={(e) => setForm({ ...form, nombre: e.target.value })} required />
        </div>
        <div className="form-check">
          <input type="checkbox" className="form-check-input" id="activo" checked={form.activo} onChange={(e) => setForm({ ...form, activo: e.target.checked })} />
          <label className="text-sm" htmlFor="activo">Activo</label>
        </div>
      </FormModal>

      <ConfirmDialog show={showConfirm} title="Eliminar Tipo de Alumno" message="Esta seguro que desea eliminar este tipo de alumno?" onConfirm={handleDelete} onCancel={() => setShowConfirm(false)} loading={loading} />
    </div>
  )
}

export default TiposAlumnoPage
