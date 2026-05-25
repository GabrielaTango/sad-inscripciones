import { useState, useEffect, useCallback } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { ArrowLeft, ArrowRight, Check, Plus, FileText, Eye, Upload, Trash2 } from 'lucide-react'
import AsyncSelect from 'react-select/async'
import DataTable from '../../components/Admin/DataTable'
import FormModal from '../../components/Admin/FormModal'
import ConfirmDialog from '../../components/Admin/ConfirmDialog'
import { eventosService } from '../../services/eventosService'
import { tiposEventoService } from '../../services/tiposEventoService'
import { eventoPreciosService } from '../../services/eventoPreciosService'
import { eventoProvinciaBeneficiosService } from '../../services/eventoProvinciaBeneficiosService'
import { eventoArticuloRegalosService } from '../../services/eventoArticuloRegalosService'
import { becaEventosService } from '../../services/becaEventosService'
import { tiposAlumnoService } from '../../services/tiposAlumnoService'
import { articulosService } from '../../services/articulosService'
import { provinciasService } from '../../services/provinciasService'
import type { Articulo } from '../../services/articulosService'
import type { Evento, EventoForm, EventoPrecio, EventoPrecioForm, EventoProvinciaBeneficio, EventoProvinciaBeneficioForm, EventoArticuloRegalo, EventoArticuloRegaloForm, BecaEvento, BecaEventoForm, TipoAlumno, TipoEvento, Provincia } from '../../types/models'

type ArticuloOption = { value: string; label: string }

const emptyEventoForm: EventoForm = {
  tipoEventoId: 0, titulo: '', descripcion: '', fechaInicio: '', fechaFin: '',
  fechaCierreInscripcion: '', lugar: '', modalidad: 'Presencial', maxInscriptos: null,
}

const EventoDetallePage = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const isNew = !id
  const eventoId = Number(id) || 0

  const [evento, setEvento] = useState<Evento | null>(null)
  const [tiposAlumno, setTiposAlumno] = useState<TipoAlumno[]>([])
  const [tiposEvento, setTiposEvento] = useState<TipoEvento[]>([])
  const [provincias, setProvincias] = useState<Provincia[]>([])
  const [tab, setTab] = useState('principal')

  // Evento form
  const [eventoForm, setEventoForm] = useState<EventoForm>(emptyEventoForm)
  const [eventoFormOriginal, setEventoFormOriginal] = useState<EventoForm>(emptyEventoForm)
  const [eventoSaving, setEventoSaving] = useState(false)
  const [eventoError, setEventoError] = useState('')
  const [eventoSuccess, setEventoSuccess] = useState('')

  const eventoFormDirty = isNew || JSON.stringify(eventoForm) !== JSON.stringify(eventoFormOriginal)

  // Sub-entity state
  const [precios, setPrecios] = useState<EventoPrecio[]>([])
  const [beneficios, setBeneficios] = useState<EventoProvinciaBeneficio[]>([])
  const [regalos, setRegalos] = useState<EventoArticuloRegalo[]>([])
  const [becas, setBecas] = useState<BecaEvento[]>([])

  const [terminosLoading, setTerminosLoading] = useState(false)

  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [deleteId, setDeleteId] = useState<number | null>(null)

  const [precioForm, setPrecioForm] = useState<EventoPrecioForm>({ eventoId, tipoAlumnoId: 0, precioBase: 0, precioCuotas: null, cantidadCuotas: 6, permiteDescuento: true, activo: true })
  const [beneficioForm, setBeneficioForm] = useState<EventoProvinciaBeneficioForm>({ eventoId, provinciaCodigo: '', aplicaPrecioSocio: false, porcentajeDescuento: 0, activo: true })
  const [regaloForm, setRegaloForm] = useState<EventoArticuloRegaloForm>({ eventoId, tipoAlumnoId: 0, articuloCodigo: '', cantidad: 1, activo: true })
  const [becaForm, setBecaForm] = useState<BecaEventoForm>({ eventoId, nombreCampana: '', tipoDescuento: 'Porcentaje', valor: 0, cantidadTotalCodigos: 1, acumulable: false, activo: true })

  const [precioArticuloSelected, setPrecioArticuloSelected] = useState<ArticuloOption | null>(null)
  const [regaloArticuloSelected, setRegaloArticuloSelected] = useState<ArticuloOption | null>(null)

  const loadArticuloOptions = async (inputValue: string): Promise<ArticuloOption[]> => {
    if (inputValue.length < 2) return []
    const items: Articulo[] = await articulosService.buscar(inputValue)
    return items.map(a => ({ value: a.codArticu, label: `${a.codArticu} - ${a.descripcion}` }))
  }

  const [initialLoading, setInitialLoading] = useState(true)

  const load = useCallback(async () => {
    if (isNew) {
      const [tiposEv] = await Promise.all([tiposEventoService.getAll()])
      setTiposEvento(tiposEv)
      setInitialLoading(false)
      return
    }
    const [ev, tipos, tiposEv, provs, p, b, r, be] = await Promise.all([
      eventosService.getById(eventoId), tiposAlumnoService.getAll(), tiposEventoService.getAll(),
      provinciasService.getAll().catch(() => [] as Provincia[]),
      eventoPreciosService.getByEventoId(eventoId), eventoProvinciaBeneficiosService.getByEventoId(eventoId),
      eventoArticuloRegalosService.getByEventoId(eventoId),
      becaEventosService.getByEventoId(eventoId),
    ])
    setEvento(ev); setTiposAlumno(tipos); setTiposEvento(tiposEv); setProvincias(provs); setPrecios(p); setBeneficios(b); setRegalos(r); setBecas(be)
    const loadedForm: EventoForm = {
      tipoEventoId: ev.tipoEventoId, titulo: ev.titulo, descripcion: ev.descripcion || '',
      fechaInicio: ev.fechaInicio?.slice(0, 16) || '', fechaFin: ev.fechaFin?.slice(0, 16) || '',
      fechaCierreInscripcion: ev.fechaCierreInscripcion?.slice(0, 16) || '',
      lugar: ev.lugar || '', modalidad: ev.modalidad, maxInscriptos: ev.maxInscriptos ?? null,
      activo: ev.activo,
    }
    setEventoForm(loadedForm)
    setEventoFormOriginal(loadedForm)
    setInitialLoading(false)
  }, [eventoId, isNew])

  useEffect(() => { load() }, [load])

  const tipoAlumnoNombre = (id: number) => tiposAlumno.find(t => t.id === id)?.nombre || '-'

  // Evento save handlers
  const saveEvento = async (andContinue: boolean) => {
    setEventoSaving(true); setEventoError(''); setEventoSuccess('')
    try {
      if (isNew) {
        const created = await eventosService.create(eventoForm)
        if (andContinue) {
          navigate(`/admin/eventos/${created.id}`, { replace: true })
        } else {
          navigate('/admin/eventos')
        }
      } else {
        await eventosService.update(eventoId, { ...eventoForm, activo: eventoForm.activo ?? true })
        setEventoSuccess('Evento actualizado correctamente')
        await load()
        setTimeout(() => setEventoSuccess(''), 3000)
      }
    } catch (err) { setEventoError(err instanceof Error ? err.message : 'Error al guardar') }
    finally { setEventoSaving(false) }
  }

  const handleEventoSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    await saveEvento(false)
  }

  // Sub-entity handlers
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setLoading(true); setError('')
    try {
      if (tab === 'precios') {
        if (editId) await eventoPreciosService.update(editId, precioForm)
        else await eventoPreciosService.create(precioForm)
      } else if (tab === 'beneficios') {
        if (editId) await eventoProvinciaBeneficiosService.update(editId, beneficioForm)
        else await eventoProvinciaBeneficiosService.create(beneficioForm)
      } else if (tab === 'regalos') {
        if (editId) await eventoArticuloRegalosService.update(editId, regaloForm)
        else await eventoArticuloRegalosService.create(regaloForm)
      } else if (tab === 'becas') {
        if (editId) await becaEventosService.update(editId, becaForm)
        else await becaEventosService.create(becaForm)
      }
      setShowForm(false); await load()
    } catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setLoading(false) }
  }

  const handleDelete = async () => {
    if (!deleteId) return; setLoading(true)
    try {
      if (tab === 'precios') await eventoPreciosService.remove(deleteId)
      else if (tab === 'beneficios') await eventoProvinciaBeneficiosService.remove(deleteId)
      else if (tab === 'regalos') await eventoArticuloRegalosService.remove(deleteId)
      else if (tab === 'becas') await becaEventosService.remove(deleteId)
      setShowConfirm(false); await load()
    } catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setLoading(false) }
  }

  const openCreate = () => {
    setEditId(null); setError('')
    if (tab === 'precios') { setPrecioForm({ eventoId, tipoAlumnoId: 0, precioBase: 0, precioCuotas: null, cantidadCuotas: 6, permiteDescuento: true, activo: true }); setPrecioArticuloSelected(null) }
    if (tab === 'beneficios') setBeneficioForm({ eventoId, provinciaCodigo: '', aplicaPrecioSocio: false, porcentajeDescuento: 0, activo: true })
    if (tab === 'regalos') { setRegaloForm({ eventoId, tipoAlumnoId: 0, articuloCodigo: '', cantidad: 1, activo: true }); setRegaloArticuloSelected(null) }
    if (tab === 'becas') setBecaForm({ eventoId, nombreCampana: '', tipoDescuento: 'Porcentaje', valor: 0, cantidadTotalCodigos: 1, acumulable: false, activo: true })
    setShowForm(true)
  }

  const renderFormFields = () => {
    if (tab === 'precios') return (
      <div className="grid grid-cols-1 md:grid-cols-12 gap-4">
        <div className="md:col-span-6"><label className="form-label">Tipo Alumno *</label>
          <select className="form-select" value={precioForm.tipoAlumnoId} onChange={e => setPrecioForm({ ...precioForm, tipoAlumnoId: Number(e.target.value) })} required>
            <option value={0}>Seleccionar...</option>{tiposAlumno.filter(t => t.activo).map(t => <option key={t.id} value={t.id}>{t.nombre}</option>)}
          </select></div>
        <div className="md:col-span-6"><label className="form-label">Precio 1 Pago *</label><input type="number" step="0.01" className="form-input" value={precioForm.precioBase} onChange={e => setPrecioForm({ ...precioForm, precioBase: Number(e.target.value) })} required /></div>
        <div className="md:col-span-4"><label className="form-label">Precio Total Cuotas</label><input type="number" step="0.01" className="form-input" value={precioForm.precioCuotas ?? ''} onChange={e => setPrecioForm({ ...precioForm, precioCuotas: e.target.value ? Number(e.target.value) : null })} /></div>
        <div className="md:col-span-2"><label className="form-label">Cant. Cuotas</label><input type="number" className="form-input" value={precioForm.cantidadCuotas} onChange={e => setPrecioForm({ ...precioForm, cantidadCuotas: Number(e.target.value) || 6 })} /></div>
        <div className="md:col-span-12"><label className="form-label">Articulo</label>
          <AsyncSelect<ArticuloOption>
            value={precioArticuloSelected}
            loadOptions={loadArticuloOptions}
            onChange={opt => { setPrecioArticuloSelected(opt); setPrecioForm({ ...precioForm, articuloCodigo: opt?.value || '' }) }}
            isClearable
            placeholder="Buscar articulo..."
            noOptionsMessage={({ inputValue }) => inputValue.length < 2 ? 'Escriba al menos 2 caracteres' : 'Sin resultados'}
            loadingMessage={() => 'Buscando...'}
          /></div>
        <div className="md:col-span-3"><div className="form-check mt-4"><input type="checkbox" className="form-check-input" checked={precioForm.permiteDescuento} onChange={e => setPrecioForm({ ...precioForm, permiteDescuento: e.target.checked })} /><label className="text-sm">Permite Descuento</label></div></div>
        <div className="md:col-span-3"><div className="form-check mt-4"><input type="checkbox" className="form-check-input" checked={precioForm.activo} onChange={e => setPrecioForm({ ...precioForm, activo: e.target.checked })} /><label className="text-sm">Activo</label></div></div>
      </div>
    )
    if (tab === 'beneficios') return (
      <div className="grid grid-cols-1 md:grid-cols-12 gap-4">
        <div className="md:col-span-6"><label className="form-label">Provincia *</label>
          <select className="form-select" value={beneficioForm.provinciaCodigo} onChange={e => setBeneficioForm({ ...beneficioForm, provinciaCodigo: e.target.value })} required>
            <option value="">Seleccionar...</option>
            {provincias.map(p => <option key={p.codProvin} value={p.codProvin}>{p.nombrePro}</option>)}
          </select></div>
        <div className="md:col-span-6"><label className="form-label">% Descuento *</label><input type="number" step="0.01" className="form-input" value={beneficioForm.porcentajeDescuento} onChange={e => setBeneficioForm({ ...beneficioForm, porcentajeDescuento: Number(e.target.value) })} required /></div>
        <div className="md:col-span-6"><div className="form-check"><input type="checkbox" className="form-check-input" checked={beneficioForm.aplicaPrecioSocio} onChange={e => setBeneficioForm({ ...beneficioForm, aplicaPrecioSocio: e.target.checked })} /><label className="text-sm">Aplica Precio Socio</label></div></div>
        <div className="md:col-span-6"><div className="form-check"><input type="checkbox" className="form-check-input" checked={beneficioForm.activo} onChange={e => setBeneficioForm({ ...beneficioForm, activo: e.target.checked })} /><label className="text-sm">Activo</label></div></div>
      </div>
    )
    if (tab === 'regalos') return (
      <div className="grid grid-cols-1 md:grid-cols-12 gap-4">
        <div className="md:col-span-6"><label className="form-label">Tipo Alumno *</label>
          <select className="form-select" value={regaloForm.tipoAlumnoId} onChange={e => setRegaloForm({ ...regaloForm, tipoAlumnoId: Number(e.target.value) })} required>
            <option value={0}>Seleccionar...</option>{tiposAlumno.filter(t => t.activo).map(t => <option key={t.id} value={t.id}>{t.nombre}</option>)}
          </select></div>
        <div className="md:col-span-6"><label className="form-label">Articulo *</label>
          <AsyncSelect<ArticuloOption>
            value={regaloArticuloSelected}
            loadOptions={loadArticuloOptions}
            onChange={opt => {
              setRegaloArticuloSelected(opt)
              const desc = opt ? opt.label.split(' - ').slice(1).join(' - ') : ''
              setRegaloForm({ ...regaloForm, articuloCodigo: opt?.value || '', descripcionArticulo: desc })
            }}
            isClearable
            placeholder="Buscar articulo..."
            noOptionsMessage={({ inputValue }) => inputValue.length < 2 ? 'Escriba al menos 2 caracteres' : 'Sin resultados'}
            loadingMessage={() => 'Buscando...'}
          /></div>
        <div className="md:col-span-3"><label className="form-label">Cantidad</label><input type="number" className="form-input" value={regaloForm.cantidad} onChange={e => setRegaloForm({ ...regaloForm, cantidad: Number(e.target.value) })} /></div>
        <div className="md:col-span-3"><div className="form-check mt-4"><input type="checkbox" className="form-check-input" checked={regaloForm.activo} onChange={e => setRegaloForm({ ...regaloForm, activo: e.target.checked })} /><label className="text-sm">Activo</label></div></div>
        <div className="md:col-span-12"><label className="form-label">Condicion Especial</label><input type="text" className="form-input" value={regaloForm.condicionEspecial || ''} onChange={e => setRegaloForm({ ...regaloForm, condicionEspecial: e.target.value })} /></div>
      </div>
    )
    if (tab === 'becas') return (
      <div className="grid grid-cols-1 md:grid-cols-12 gap-4">
        <div className="md:col-span-6"><label className="form-label">Nombre Campana *</label><input type="text" className="form-input" value={becaForm.nombreCampana} onChange={e => setBecaForm({ ...becaForm, nombreCampana: e.target.value })} required /></div>
        <div className="md:col-span-3"><label className="form-label">Tipo Descuento *</label>
          <select className="form-select" value={becaForm.tipoDescuento} onChange={e => setBecaForm({ ...becaForm, tipoDescuento: e.target.value })} required>
            <option value="Porcentaje">Porcentaje</option><option value="MontoFijo">Monto Fijo</option>
          </select></div>
        <div className="md:col-span-3"><label className="form-label">Valor *</label><input type="number" step="0.01" className="form-input" value={becaForm.valor} onChange={e => setBecaForm({ ...becaForm, valor: Number(e.target.value) })} required /></div>
        <div className="md:col-span-4"><label className="form-label">Cantidad Codigos *</label><input type="number" className="form-input" value={becaForm.cantidadTotalCodigos} onChange={e => setBecaForm({ ...becaForm, cantidadTotalCodigos: Number(e.target.value) })} required /></div>
        <div className="md:col-span-4"><label className="form-label">Vencimiento</label><input type="datetime-local" className="form-input" value={becaForm.fechaVencimiento || ''} onChange={e => setBecaForm({ ...becaForm, fechaVencimiento: e.target.value || undefined })} /></div>
        <div className="md:col-span-2"><div className="form-check mt-4"><input type="checkbox" className="form-check-input" checked={becaForm.acumulable} onChange={e => setBecaForm({ ...becaForm, acumulable: e.target.checked })} /><label className="text-sm">Acumulable</label></div></div>
        <div className="md:col-span-2"><div className="form-check mt-4"><input type="checkbox" className="form-check-input" checked={becaForm.activo} onChange={e => setBecaForm({ ...becaForm, activo: e.target.checked })} /><label className="text-sm">Activo</label></div></div>
      </div>
    )
    return null
  }

  const tabData = () => {
    if (tab === 'precios') return { data: precios, columns: [
      { key: 'id', label: 'ID' },
      { key: 'tipoAlumnoId', label: 'Tipo Alumno', render: (i: EventoPrecio) => tipoAlumnoNombre(i.tipoAlumnoId) },
      { key: 'precioBase', label: 'Precio', render: (i: EventoPrecio) => `$${i.precioBase.toFixed(2)}` },
      { key: 'permiteDescuento', label: 'Desc.', render: (i: EventoPrecio) => i.permiteDescuento ? 'Si' : 'No' },
      { key: 'activo', label: 'Activo', render: (i: EventoPrecio) => i.activo ? <span className="badge bg-green-100 text-green-700">Si</span> : <span className="badge bg-gray-100 text-gray-700">No</span> },
    ], onEdit: (i: EventoPrecio) => { setEditId(i.id); setPrecioForm({ eventoId, tipoAlumnoId: i.tipoAlumnoId, articuloCodigo: i.articuloCodigo, precioBase: i.precioBase, precioCuotas: i.precioCuotas ?? null, cantidadCuotas: i.cantidadCuotas || 6, permiteDescuento: i.permiteDescuento, activo: i.activo }); if (i.articuloCodigo) { const cod = i.articuloCodigo; articulosService.buscar(cod).then(items => { const found = items.find(a => a.codArticu === cod); setPrecioArticuloSelected(found ? { value: found.codArticu, label: `${found.codArticu} - ${found.descripcion}` } : { value: cod, label: cod }) }) } else { setPrecioArticuloSelected(null) }; setError(''); setShowForm(true) } }
    if (tab === 'beneficios') return { data: beneficios, columns: [
      { key: 'id', label: 'ID' }, { key: 'provinciaCodigo', label: 'Provincia', render: (i: EventoProvinciaBeneficio) => provincias.find(p => p.codProvin === i.provinciaCodigo)?.nombrePro || i.provinciaCodigo },
      { key: 'porcentajeDescuento', label: '% Desc.' }, { key: 'aplicaPrecioSocio', label: 'Precio Socio', render: (i: EventoProvinciaBeneficio) => i.aplicaPrecioSocio ? 'Si' : 'No' },
      { key: 'activo', label: 'Activo', render: (i: EventoProvinciaBeneficio) => i.activo ? <span className="badge bg-green-100 text-green-700">Si</span> : <span className="badge bg-gray-100 text-gray-700">No</span> },
    ], onEdit: (i: EventoProvinciaBeneficio) => { setEditId(i.id); setBeneficioForm({ eventoId, provinciaCodigo: i.provinciaCodigo, aplicaPrecioSocio: i.aplicaPrecioSocio, porcentajeDescuento: i.porcentajeDescuento, activo: i.activo }); setError(''); setShowForm(true) } }
    if (tab === 'regalos') return { data: regalos, columns: [
      { key: 'id', label: 'ID' }, { key: 'tipoAlumnoId', label: 'Tipo Alumno', render: (i: EventoArticuloRegalo) => tipoAlumnoNombre(i.tipoAlumnoId) },
      { key: 'articuloCodigo', label: 'Codigo' }, { key: 'descripcionArticulo', label: 'Descripcion' }, { key: 'cantidad', label: 'Cant.' },
      { key: 'activo', label: 'Activo', render: (i: EventoArticuloRegalo) => i.activo ? <span className="badge bg-green-100 text-green-700">Si</span> : <span className="badge bg-gray-100 text-gray-700">No</span> },
    ], onEdit: (i: EventoArticuloRegalo) => { setEditId(i.id); setRegaloForm({ eventoId, tipoAlumnoId: i.tipoAlumnoId, articuloCodigo: i.articuloCodigo, descripcionArticulo: i.descripcionArticulo, cantidad: i.cantidad, condicionEspecial: i.condicionEspecial, activo: i.activo }); setRegaloArticuloSelected(i.articuloCodigo ? { value: i.articuloCodigo, label: `${i.articuloCodigo} - ${i.descripcionArticulo || ''}` } : null); setError(''); setShowForm(true) } }
    if (tab === 'becas') return { data: becas, columns: [
      { key: 'id', label: 'ID' }, { key: 'nombreCampana', label: 'Campana' }, { key: 'tipoDescuento', label: 'Tipo' },
      { key: 'valor', label: 'Valor' }, { key: 'cantidadTotalCodigos', label: 'Codigos' },
      { key: 'activo', label: 'Activo', render: (i: BecaEvento) => i.activo ? <span className="badge bg-green-100 text-green-700">Si</span> : <span className="badge bg-gray-100 text-gray-700">No</span> },
    ], onEdit: (i: BecaEvento) => { setEditId(i.id); setBecaForm({ eventoId, nombreCampana: i.nombreCampana, tipoDescuento: i.tipoDescuento, valor: i.valor, cantidadTotalCodigos: i.cantidadTotalCodigos, fechaVencimiento: i.fechaVencimiento?.slice(0, 16), acumulable: i.acumulable, activo: i.activo }); setError(''); setShowForm(true) } }
    return { data: [], columns: [], onEdit: () => {} }
  }

  const td = tabData()
  const tabLabels: Record<string, string> = { principal: 'Principal', precios: 'Precios', beneficios: 'Beneficios Provincia', regalos: 'Articulos Regalo', becas: 'Becas' }

  if (initialLoading) return <div className="text-center py-5"><div className="animate-spin w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full"></div></div>

  return (
    <div>
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center">
          <Link to="/admin/eventos" className="btn-outline mr-3"><ArrowLeft className="w-4 h-4" /></Link>
          <h2 className="font-bold text-slate-800">{isNew ? 'Nuevo Evento' : evento?.titulo}</h2>
          {!isNew && evento && <span className="text-sm text-slate-500 ml-3">{evento.modalidad} - {new Date(evento.fechaInicio).toLocaleDateString('es-AR')}</span>}
        </div>
        {/* Save buttons - always visible outside tabs */}
        <form onSubmit={handleEventoSubmit} id="eventoForm" className="flex gap-2">
          {eventoError && <div className="alert-danger py-1 px-2 mb-0 mr-2">{eventoError}</div>}
          {eventoSuccess && <div className="alert-success py-1 px-2 mb-0 mr-2">{eventoSuccess}</div>}
          {isNew ? (
            <>
              <button type="submit" className="btn-primary" disabled={eventoSaving || !eventoFormDirty}>
                {eventoSaving ? <><span className="animate-spin w-4 h-4 border-2 border-current border-t-transparent rounded-full inline-block mr-1"></span>Guardando...</> : <><Check className="w-4 h-4 mr-1 inline" />Guardar</>}
              </button>
              <button type="button" className="btn-outline-primary" disabled={eventoSaving || !eventoFormDirty} onClick={() => saveEvento(true)}>
                {eventoSaving ? <><span className="animate-spin w-4 h-4 border-2 border-current border-t-transparent rounded-full inline-block mr-1"></span>Guardando...</> : <><ArrowRight className="w-4 h-4 mr-1 inline" />Guardar y continuar</>}
              </button>
            </>
          ) : (
            <button type="submit" className="btn-primary" disabled={eventoSaving || !eventoFormDirty}>
              {eventoSaving ? <><span className="animate-spin w-4 h-4 border-2 border-current border-t-transparent rounded-full inline-block mr-1"></span>Guardando...</> : <><Check className="w-4 h-4 mr-1 inline" />Guardar</>}
            </button>
          )}
        </form>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-gray-200 mb-4">
        {Object.entries(tabLabels).map(([key, label]) => {
          const disabled = isNew && key !== 'principal'
          return (
            <button
              key={key}
              className={
                tab === key
                  ? 'px-4 py-2 text-sm font-medium border-b-2 border-blue-600 text-slate-800'
                  : disabled
                    ? 'px-4 py-2 text-sm font-medium text-gray-300 cursor-not-allowed'
                    : 'px-4 py-2 text-sm font-medium border-b-2 border-transparent text-slate-500 hover:text-gray-700'
              }
              onClick={() => { if (!disabled) setTab(key) }}
            >
              {label}
            </button>
          )
        })}
      </div>

      {/* Tab: Principal */}
      {tab === 'principal' && (
        <div className="grid grid-cols-1 md:grid-cols-12 gap-4">
          <div className="md:col-span-6">
            <label className="form-label">Tipo de Evento *</label>
            <select className="form-select" form="eventoForm" value={eventoForm.tipoEventoId} onChange={e => setEventoForm({ ...eventoForm, tipoEventoId: Number(e.target.value) })} required>
              <option value={0}>Seleccionar...</option>
              {tiposEvento.filter(t => t.activo).map(t => <option key={t.id} value={t.id}>{t.nombre}</option>)}
            </select>
          </div>
          <div className="md:col-span-6">
            <label className="form-label">Titulo *</label>
            <input type="text" className="form-input" form="eventoForm" value={eventoForm.titulo} onChange={e => setEventoForm({ ...eventoForm, titulo: e.target.value })} required />
          </div>
          <div className="md:col-span-12">
            <label className="form-label">Descripcion</label>
            <textarea className="form-input" form="eventoForm" rows={2} value={eventoForm.descripcion || ''} onChange={e => setEventoForm({ ...eventoForm, descripcion: e.target.value })} />
          </div>
          <div className="md:col-span-4">
            <label className="form-label">Fecha Inicio *</label>
            <input type="datetime-local" className="form-input" form="eventoForm" value={eventoForm.fechaInicio} onChange={e => setEventoForm({ ...eventoForm, fechaInicio: e.target.value })} required />
          </div>
          <div className="md:col-span-4">
            <label className="form-label">Fecha Fin *</label>
            <input type="datetime-local" className="form-input" form="eventoForm" value={eventoForm.fechaFin} onChange={e => setEventoForm({ ...eventoForm, fechaFin: e.target.value })} required />
          </div>
          <div className="md:col-span-4">
            <label className="form-label">Cierre Inscripcion *</label>
            <input type="datetime-local" className="form-input" form="eventoForm" value={eventoForm.fechaCierreInscripcion} onChange={e => setEventoForm({ ...eventoForm, fechaCierreInscripcion: e.target.value })} required />
          </div>
          <div className="md:col-span-4">
            <label className="form-label">Lugar</label>
            <input type="text" className="form-input" form="eventoForm" value={eventoForm.lugar || ''} onChange={e => setEventoForm({ ...eventoForm, lugar: e.target.value })} />
          </div>
          <div className="md:col-span-4">
            <label className="form-label">Modalidad *</label>
            <select className="form-select" form="eventoForm" value={eventoForm.modalidad} onChange={e => setEventoForm({ ...eventoForm, modalidad: e.target.value })} required>
              <option value="Presencial">Presencial</option>
              <option value="Online">Online</option>
              <option value="Hibrido">Hibrido</option>
            </select>
          </div>
          <div className="md:col-span-4">
            <label className="form-label">Max Inscriptos</label>
            <input type="number" className="form-input" form="eventoForm" value={eventoForm.maxInscriptos ?? ''} onChange={e => setEventoForm({ ...eventoForm, maxInscriptos: e.target.value ? Number(e.target.value) : null })} />
          </div>
          {!isNew && (
            <div className="md:col-span-12">
              <div className="form-check">
                <input type="checkbox" className="form-check-input" form="eventoForm" id="eventoActivo" checked={eventoForm.activo ?? true} onChange={e => setEventoForm({ ...eventoForm, activo: e.target.checked })} />
                <label className="text-sm" htmlFor="eventoActivo">Activo</label>
              </div>
            </div>
          )}

          {/* Terminos y Condiciones */}
          {!isNew && evento && (
            <div className="md:col-span-12">
              <div className="card">
                <div className="p-4 flex items-center justify-between">
                  <div>
                    <strong><FileText className="w-4 h-4 mr-2 inline" />Terminos y Condiciones</strong>
                    {evento.terminosArchivo ? (
                      <a href={`/api/eventos/${eventoId}/terminos/archivo`} target="_blank" rel="noopener noreferrer" className="ml-3">
                        <Eye className="w-4 h-4 mr-1 inline" />Ver archivo
                      </a>
                    ) : (
                      <span className="text-slate-500 ml-3">Sin archivo</span>
                    )}
                  </div>
                  <div>
                    {evento.terminosArchivo ? (
                      <button
                        className="btn-outline-danger btn-sm"
                        disabled={terminosLoading}
                        onClick={async () => {
                          setTerminosLoading(true)
                          try {
                            await eventosService.deleteTerminos(eventoId)
                            setEvento({ ...evento, terminosArchivo: undefined })
                          } catch (err) { setError(err instanceof Error ? err.message : 'Error') }
                          finally { setTerminosLoading(false) }
                        }}
                      >
                        {terminosLoading ? <span className="animate-spin w-4 h-4 border-2 border-current border-t-transparent rounded-full inline-block"></span> : <><Trash2 className="w-4 h-4 mr-1 inline" />Eliminar</>}
                      </button>
                    ) : (
                      <>
                        <input
                          type="file"
                          accept=".pdf"
                          id="terminosFile"
                          className="hidden"
                          onChange={async (e) => {
                            const file = e.target.files?.[0]
                            if (!file) return
                            setTerminosLoading(true)
                            try {
                              const result = await eventosService.uploadTerminos(eventoId, file)
                              setEvento({ ...evento, terminosArchivo: result.terminosArchivo })
                            } catch (err) { setError(err instanceof Error ? err.message : 'Error') }
                            finally { setTerminosLoading(false); e.target.value = '' }
                          }}
                        />
                        <label htmlFor="terminosFile" className="btn-outline-primary btn-sm mb-0" style={{ cursor: 'pointer' }}>
                          {terminosLoading ? <span className="animate-spin w-4 h-4 border-2 border-current border-t-transparent rounded-full inline-block"></span> : <><Upload className="w-4 h-4 mr-1 inline" />Subir archivo de T&C</>}
                        </label>
                      </>
                    )}
                  </div>
                </div>
              </div>
            </div>
          )}

          {isNew && (
            <div className="md:col-span-12">
              <div className="text-slate-500 mt-2">
                Guarde el evento para configurar precios, beneficios, regalos y becas.
              </div>
            </div>
          )}
        </div>
      )}

      {/* Other tabs content */}
      {tab !== 'principal' && !isNew && (
        <>
          <div className="flex justify-end mb-3">
            <button className="btn-primary btn-sm" onClick={openCreate}><Plus className="w-4 h-4 mr-1 inline" />Nuevo</button>
          </div>
          <DataTable
            data={td.data as unknown as Record<string, unknown>[]}
            columns={td.columns as never}
            onEdit={td.onEdit as never}
            onDelete={((item: { id: number }) => { setDeleteId(item.id); setShowConfirm(true) }) as never}
          />
        </>
      )}

      <FormModal show={showForm} title={`${editId ? 'Editar' : 'Nuevo'} - ${tabLabels[tab]}`} onClose={() => setShowForm(false)} onSubmit={handleSubmit} loading={loading} size="lg">
        {error && <div className="alert-danger">{error}</div>}
        {renderFormFields()}
      </FormModal>

      <ConfirmDialog show={showConfirm} title="Eliminar" message="Esta seguro que desea eliminar este registro?" onConfirm={handleDelete} onCancel={() => setShowConfirm(false)} loading={loading} />
    </div>
  )
}

export default EventoDetallePage
