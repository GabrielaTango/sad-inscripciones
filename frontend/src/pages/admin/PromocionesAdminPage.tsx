import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Ticket } from 'lucide-react'
import DataTable from '../../components/Admin/DataTable'
import FormModal from '../../components/Admin/FormModal'
import ConfirmDialog from '../../components/Admin/ConfirmDialog'
import { promocionesService } from '../../services/promocionesService'
import { tiposAlumnoService } from '../../services/tiposAlumnoService'
import type { Promocion, PromocionForm, TipoAlumno } from '../../types/models'

const emptyForm: PromocionForm = {
  nombre: '', descripcion: '', tipoAlumnoId: null, cantidadCursosRequeridos: 1,
  periodoMeses: 12, tipoDescuento: 'Porcentaje', valor: 0, acumulable: false,
  fechaVigenciaDesde: '', fechaVigenciaHasta: '', diasValidezCupon: 90, activo: true
}

const PromocionesAdminPage = () => {
  const navigate = useNavigate()
  const [data, setData] = useState<Promocion[]>([])
  const [tiposAlumno, setTiposAlumno] = useState<TipoAlumno[]>([])
  const [loading, setLoading] = useState(false)
  const [showForm, setShowForm] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [deleteId, setDeleteId] = useState<number | null>(null)
  const [form, setForm] = useState<PromocionForm>(emptyForm)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    const [promos, tipos] = await Promise.all([promocionesService.getAll(), tiposAlumnoService.getAll()])
    setData(promos); setTiposAlumno(tipos)
  }, [])

  useEffect(() => { load() }, [load])

  const tipoAlumnoNombre = (id?: number) => id ? tiposAlumno.find(t => t.id === id)?.nombre || '-' : 'Todos'

  const openCreate = () => { setForm(emptyForm); setEditId(null); setError(''); setShowForm(true) }
  const openEdit = (item: Promocion) => {
    setForm({
      nombre: item.nombre, descripcion: item.descripcion || '', tipoAlumnoId: item.tipoAlumnoId ?? null,
      cantidadCursosRequeridos: item.cantidadCursosRequeridos, periodoMeses: item.periodoMeses,
      tipoDescuento: item.tipoDescuento, valor: item.valor, acumulable: item.acumulable,
      fechaVigenciaDesde: item.fechaVigenciaDesde?.slice(0, 16) || '', fechaVigenciaHasta: item.fechaVigenciaHasta?.slice(0, 16) || '',
      diasValidezCupon: item.diasValidezCupon, activo: item.activo
    })
    setEditId(item.id); setError(''); setShowForm(true)
  }
  const openDelete = (item: Promocion) => { setDeleteId(item.id); setShowConfirm(true) }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setLoading(true); setError('')
    try {
      if (editId) await promocionesService.update(editId, form)
      else await promocionesService.create(form)
      setShowForm(false); await load()
    } catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setLoading(false) }
  }

  const handleDelete = async () => {
    if (!deleteId) return; setLoading(true)
    try { await promocionesService.remove(deleteId); setShowConfirm(false); await load() }
    catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setLoading(false) }
  }

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'nombre', label: 'Nombre' },
    { key: 'tipoAlumnoId', label: 'Tipo Alumno', render: (i: Promocion) => tipoAlumnoNombre(i.tipoAlumnoId) },
    { key: 'cantidadCursosRequeridos', label: 'Cursos Req.' },
    { key: 'periodoMeses', label: 'Periodo (meses)' },
    { key: 'tipoDescuento', label: 'Tipo Desc.' },
    { key: 'valor', label: 'Valor' },
    { key: 'activo', label: 'Activo', render: (i: Promocion) => i.activo ? <span className="badge bg-green-100 text-green-700">Si</span> : <span className="badge bg-gray-100 text-gray-700">No</span> },
  ]

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="font-bold text-slate-800">Promociones</h2>
        <button className="btn-primary" onClick={openCreate}><Plus className="w-4 h-4 mr-1 inline" />Nueva Promocion</button>
      </div>

      <DataTable
        data={data as unknown as Record<string, unknown>[]}
        columns={columns as never}
        onEdit={openEdit as never}
        onDelete={openDelete as never}
        actions={(item: unknown) => (
          <button className="btn-outline-primary btn-sm p-1.5" onClick={() => navigate(`/admin/promociones/${(item as Promocion).id}/cupones`)} title="Ver Cupones"><Ticket className="w-4 h-4" /></button>
        )}
      />

      <FormModal show={showForm} title={editId ? 'Editar Promocion' : 'Nueva Promocion'} onClose={() => setShowForm(false)} onSubmit={handleSubmit} loading={loading} size="lg">
        {error && <div className="alert-danger">{error}</div>}
        <div className="grid grid-cols-1 md:grid-cols-12 gap-4">
          <div className="md:col-span-8"><label className="form-label">Nombre *</label><input type="text" className="form-input" value={form.nombre} onChange={e => setForm({ ...form, nombre: e.target.value })} required /></div>
          <div className="md:col-span-4"><label className="form-label">Tipo Alumno</label>
            <select className="form-select" value={form.tipoAlumnoId ?? ''} onChange={e => setForm({ ...form, tipoAlumnoId: e.target.value ? Number(e.target.value) : null })}>
              <option value="">Todos</option>{tiposAlumno.filter(t => t.activo).map(t => <option key={t.id} value={t.id}>{t.nombre}</option>)}
            </select></div>
          <div className="md:col-span-12"><label className="form-label">Descripcion</label><input type="text" className="form-input" value={form.descripcion || ''} onChange={e => setForm({ ...form, descripcion: e.target.value })} /></div>
          <div className="md:col-span-4"><label className="form-label">Cursos Requeridos *</label><input type="number" min="1" className="form-input" value={form.cantidadCursosRequeridos} onChange={e => setForm({ ...form, cantidadCursosRequeridos: Number(e.target.value) })} required /></div>
          <div className="md:col-span-4"><label className="form-label">Periodo (meses) *</label><input type="number" min="1" className="form-input" value={form.periodoMeses} onChange={e => setForm({ ...form, periodoMeses: Number(e.target.value) })} required /></div>
          <div className="md:col-span-4"><label className="form-label">Dias Validez Cupon</label><input type="number" min="1" className="form-input" value={form.diasValidezCupon} onChange={e => setForm({ ...form, diasValidezCupon: Number(e.target.value) })} /></div>
          <div className="md:col-span-4"><label className="form-label">Tipo Descuento *</label>
            <select className="form-select" value={form.tipoDescuento} onChange={e => setForm({ ...form, tipoDescuento: e.target.value })} required>
              <option value="Porcentaje">Porcentaje</option><option value="MontoFijo">Monto Fijo</option>
            </select></div>
          <div className="md:col-span-4"><label className="form-label">Valor *</label><input type="number" step="0.01" className="form-input" value={form.valor} onChange={e => setForm({ ...form, valor: Number(e.target.value) })} required /></div>
          <div className="md:col-span-2"><div className="form-check mt-4"><input type="checkbox" className="form-check-input" checked={form.acumulable} onChange={e => setForm({ ...form, acumulable: e.target.checked })} /><label className="text-sm">Acumulable</label></div></div>
          <div className="md:col-span-2"><div className="form-check mt-4"><input type="checkbox" className="form-check-input" checked={form.activo} onChange={e => setForm({ ...form, activo: e.target.checked })} /><label className="text-sm">Activo</label></div></div>
          <div className="md:col-span-6"><label className="form-label">Vigencia Desde *</label><input type="datetime-local" className="form-input" value={form.fechaVigenciaDesde} onChange={e => setForm({ ...form, fechaVigenciaDesde: e.target.value })} required /></div>
          <div className="md:col-span-6"><label className="form-label">Vigencia Hasta *</label><input type="datetime-local" className="form-input" value={form.fechaVigenciaHasta} onChange={e => setForm({ ...form, fechaVigenciaHasta: e.target.value })} required /></div>
        </div>
      </FormModal>

      <ConfirmDialog show={showConfirm} title="Eliminar Promocion" message="Esta seguro que desea eliminar esta promocion?" onConfirm={handleDelete} onCancel={() => setShowConfirm(false)} loading={loading} />
    </div>
  )
}

export default PromocionesAdminPage
