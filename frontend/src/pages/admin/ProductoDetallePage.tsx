import { useState, useEffect, useCallback } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { ArrowLeft, ArrowRight, Check, Plus, Trash2, ArrowUp, ArrowDown } from 'lucide-react'
import { productosService } from '../../services/productosService'
import type { CampoExtraProducto, ProductoForm } from '../../types/models'

const emptyCampoExtra = (): CampoExtraProducto => ({ key: '', label: '', type: 'text', options: [], required: false })

const emptyProductoForm: ProductoForm = {
  nombre: '', descripcion: '', precio: 0, activo: true, imagenUrl: '', camposExtra: [], mailAsunto: '', mailCuerpoHtml: '',
}

const ProductoDetallePage = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const isNew = !id
  const productoId = Number(id) || 0

  const [form, setForm] = useState<ProductoForm>(emptyProductoForm)
  const [formOriginal, setFormOriginal] = useState<ProductoForm>(emptyProductoForm)
  const [initialLoading, setInitialLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  // Texto crudo de las opciones mientras se edita, para no perder comas o espacios finales al tipear.
  const [opcionesDraft, setOpcionesDraft] = useState<Record<number, string>>({})

  const formDirty = isNew || JSON.stringify(form) !== JSON.stringify(formOriginal)

  const load = useCallback(async () => {
    if (isNew) { setInitialLoading(false); return }
    const producto = await productosService.getByIdAdmin(productoId)
    const loadedForm: ProductoForm = {
      nombre: producto.nombre,
      descripcion: producto.descripcion || '',
      precio: producto.precio,
      activo: producto.activo,
      imagenUrl: producto.imagenUrl || '',
      camposExtra: producto.camposExtra,
      mailAsunto: producto.mailAsunto || '',
      mailCuerpoHtml: producto.mailCuerpoHtml || '',
    }
    setForm(loadedForm)
    setFormOriginal(loadedForm)
    setInitialLoading(false)
  }, [productoId, isNew])

  useEffect(() => { load() }, [load])

  const save = async (andContinue: boolean) => {
    setSaving(true); setError(''); setSuccess('')
    try {
      if (isNew) {
        const created = await productosService.create(form)
        if (andContinue) navigate(`/admin/productos/${created.id}`, { replace: true })
        else navigate('/admin/productos')
      } else {
        await productosService.update(productoId, form)
        setSuccess('Producto actualizado correctamente')
        await load()
        setTimeout(() => setSuccess(''), 3000)
      }
    } catch (err) { setError(err instanceof Error ? err.message : 'Error al guardar') }
    finally { setSaving(false) }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    await save(false)
  }

  // Extra-field editor handlers
  const addCampoExtra = () => setForm({ ...form, camposExtra: [...form.camposExtra, emptyCampoExtra()] })

  const updateCampoExtra = (index: number, patch: Partial<CampoExtraProducto>) => {
    const camposExtra = form.camposExtra.map((c, i) => i === index ? { ...c, ...patch } : c)
    setForm({ ...form, camposExtra })
  }

  const removeCampoExtra = (index: number) => {
    setForm({ ...form, camposExtra: form.camposExtra.filter((_, i) => i !== index) })
  }

  const moveCampoExtra = (index: number, direction: -1 | 1) => {
    const target = index + direction
    if (target < 0 || target >= form.camposExtra.length) return
    const camposExtra = [...form.camposExtra]
    ;[camposExtra[index], camposExtra[target]] = [camposExtra[target], camposExtra[index]]
    setForm({ ...form, camposExtra })
  }

  const disponibles = ['Nombre', 'Apellido', 'Dni', 'Email', 'Producto', 'Importe', ...form.camposExtra.filter(c => c.key).map(c => c.key)]

  if (initialLoading) return <div className="text-center py-5"><div className="animate-spin w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full"></div></div>

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center">
          <Link to="/admin/productos" className="btn-outline mr-3"><ArrowLeft className="w-4 h-4" /></Link>
          <h2 className="font-bold text-slate-800">{isNew ? 'Nuevo Producto' : form.nombre}</h2>
        </div>
        <form onSubmit={handleSubmit} id="productoForm" className="flex gap-2">
          {error && <div className="alert-danger py-1 px-2 mb-0 mr-2">{error}</div>}
          {success && <div className="alert-success py-1 px-2 mb-0 mr-2">{success}</div>}
          {isNew ? (
            <>
              <button type="submit" className="btn-primary" disabled={saving || !formDirty}>
                {saving ? <><span className="animate-spin w-4 h-4 border-2 border-current border-t-transparent rounded-full inline-block mr-1"></span>Guardando...</> : <><Check className="w-4 h-4 mr-1 inline" />Guardar</>}
              </button>
              <button type="button" className="btn-outline-primary" disabled={saving || !formDirty} onClick={() => save(true)}>
                {saving ? <><span className="animate-spin w-4 h-4 border-2 border-current border-t-transparent rounded-full inline-block mr-1"></span>Guardando...</> : <><ArrowRight className="w-4 h-4 mr-1 inline" />Guardar y continuar</>}
              </button>
            </>
          ) : (
            <button type="submit" className="btn-primary" disabled={saving || !formDirty}>
              {saving ? <><span className="animate-spin w-4 h-4 border-2 border-current border-t-transparent rounded-full inline-block mr-1"></span>Guardando...</> : <><Check className="w-4 h-4 mr-1 inline" />Guardar</>}
            </button>
          )}
        </form>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-4">
        <div className="md:col-span-6">
          <label className="form-label">Nombre *</label>
          <input type="text" className="form-input" form="productoForm" value={form.nombre} onChange={e => setForm({ ...form, nombre: e.target.value })} required />
        </div>
        <div className="md:col-span-3">
          <label className="form-label">Precio *</label>
          <input type="number" step="0.01" className="form-input" form="productoForm" value={form.precio} onChange={e => setForm({ ...form, precio: Number(e.target.value) })} required />
        </div>
        <div className="md:col-span-3">
          <div className="form-check mt-4">
            <input type="checkbox" className="form-check-input" form="productoForm" id="productoActivo" checked={form.activo} onChange={e => setForm({ ...form, activo: e.target.checked })} />
            <label className="text-sm" htmlFor="productoActivo">Activo</label>
          </div>
        </div>
        <div className="md:col-span-12">
          <label className="form-label">Descripcion</label>
          <textarea className="form-input" form="productoForm" rows={2} value={form.descripcion || ''} onChange={e => setForm({ ...form, descripcion: e.target.value })} />
        </div>
        <div className="md:col-span-12">
          <label className="form-label">URL de Imagen</label>
          <input type="text" className="form-input" form="productoForm" value={form.imagenUrl || ''} onChange={e => setForm({ ...form, imagenUrl: e.target.value })} placeholder="https://..." />
        </div>

        {/* Campos extra */}
        <div className="md:col-span-12">
          <div className="card">
            <div className="p-4">
              <div className="flex items-center justify-between mb-3">
                <strong>Campos Extra del Formulario de Compra</strong>
                <button type="button" className="btn-outline-primary btn-sm" onClick={addCampoExtra}><Plus className="w-4 h-4 mr-1 inline" />Agregar campo</button>
              </div>

              {form.camposExtra.length === 0 && (
                <p className="text-slate-500">Sin campos extra. El comprador solo completara sus datos basicos (DNI, nombre, apellido, email).</p>
              )}

              {form.camposExtra.map((campo, index) => (
                <div key={index} className="grid grid-cols-1 md:grid-cols-12 gap-3 items-start border-t border-gray-200 py-3">
                  <div className="md:col-span-3">
                    <label className="form-label">Clave (key) *</label>
                    <input type="text" className="form-input" value={campo.key} onChange={e => updateCampoExtra(index, { key: e.target.value })} placeholder="ej: talle" required />
                  </div>
                  <div className="md:col-span-3">
                    <label className="form-label">Etiqueta (label) *</label>
                    <input type="text" className="form-input" value={campo.label} onChange={e => updateCampoExtra(index, { label: e.target.value })} placeholder="ej: Talle" required />
                  </div>
                  <div className="md:col-span-2">
                    <label className="form-label">Tipo *</label>
                    <select className="form-select" value={campo.type} onChange={e => updateCampoExtra(index, { type: e.target.value as CampoExtraProducto['type'] })}>
                      <option value="text">Texto</option>
                      <option value="number">Numero</option>
                      <option value="select">Seleccion</option>
                    </select>
                  </div>
                  {campo.type === 'select' && (
                    <div className="md:col-span-3">
                      <label className="form-label">Opciones (separadas por coma)</label>
                      <input
                        type="text"
                        className="form-input"
                        value={opcionesDraft[index] ?? campo.options.join(', ')}
                        onChange={e => {
                          const raw = e.target.value
                          setOpcionesDraft(d => ({ ...d, [index]: raw }))
                          updateCampoExtra(index, { options: raw.split(',').map(o => o.trim()).filter(o => o.length > 0) })
                        }}
                        onBlur={() => setOpcionesDraft(d => { const rest = { ...d }; delete rest[index]; return rest })}
                        placeholder="S, M, L, XL"
                      />
                    </div>
                  )}
                  <div className={campo.type === 'select' ? 'md:col-span-1' : 'md:col-span-4'}>
                    <div className="form-check mt-6">
                      <input type="checkbox" className="form-check-input" checked={campo.required} onChange={e => updateCampoExtra(index, { required: e.target.checked })} />
                      <label className="text-sm">Obligatorio</label>
                    </div>
                  </div>
                  <div className="md:col-span-12 flex gap-1 justify-end">
                    <button type="button" className="btn-outline btn-sm p-1.5" title="Subir" disabled={index === 0} onClick={() => moveCampoExtra(index, -1)}><ArrowUp className="w-3.5 h-3.5" /></button>
                    <button type="button" className="btn-outline btn-sm p-1.5" title="Bajar" disabled={index === form.camposExtra.length - 1} onClick={() => moveCampoExtra(index, 1)}><ArrowDown className="w-3.5 h-3.5" /></button>
                    <button type="button" className="btn-outline-danger btn-sm p-1.5" title="Eliminar" onClick={() => removeCampoExtra(index)}><Trash2 className="w-3.5 h-3.5" /></button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Email de confirmacion */}
        <div className="md:col-span-12">
          <div className="card">
            <div className="p-4">
              <strong>Email de Confirmacion de Compra</strong>
              <div className="grid grid-cols-1 md:grid-cols-12 gap-4 mt-3">
                <div className="md:col-span-12">
                  <label className="form-label">Asunto</label>
                  <input type="text" className="form-input" form="productoForm" value={form.mailAsunto || ''} onChange={e => setForm({ ...form, mailAsunto: e.target.value })} />
                </div>
                <div className="md:col-span-12">
                  <label className="form-label">Cuerpo (HTML)</label>
                  <textarea className="form-input" form="productoForm" rows={8} value={form.mailCuerpoHtml || ''} onChange={e => setForm({ ...form, mailCuerpoHtml: e.target.value })} />
                </div>
                <div className="md:col-span-12">
                  <div className="bg-blue-50 border border-blue-200 rounded p-3 text-sm text-slate-700">
                    <strong>Variables disponibles:</strong>{' '}
                    {disponibles.map(v => <code key={v} className="bg-white border border-gray-200 rounded px-1 mr-1">{`{{${v}}}`}</code>)}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default ProductoDetallePage
