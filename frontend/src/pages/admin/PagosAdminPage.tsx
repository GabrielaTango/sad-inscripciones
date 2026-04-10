import { useState, useEffect, useCallback } from 'react'
import { Plus, Check, X } from 'lucide-react'
import DataTable from '../../components/Admin/DataTable'
import FormModal from '../../components/Admin/FormModal'
import { pagosService } from '../../services/pagosService'
import type { Pago, PagoForm } from '../../types/models'

const PagosAdminPage = () => {
  const [data, setData] = useState<Pago[]>([])
  const [loading, setLoading] = useState(false)
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState<PagoForm>({ inscripcionId: 0, medioPago: '', monto: 0 })
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setData(await pagosService.getAll())
  }, [])

  useEffect(() => { load() }, [load])

  const handleEstado = async (id: number, estado: string) => {
    try { await pagosService.updateEstado(id, estado); await load() }
    catch (err) { setError(err instanceof Error ? err.message : 'Error') }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setLoading(true); setError('')
    try { await pagosService.create(form); setShowForm(false); await load() }
    catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setLoading(false) }
  }

  const estadoBadge = (estado: string) => {
    const map: Record<string, string> = { Pendiente: 'bg-amber-100 text-amber-700', Confirmado: 'bg-green-100 text-green-700', Rechazado: 'bg-red-100 text-red-700' }
    return map[estado] || 'bg-blue-100 text-blue-700'
  }

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'inscripcionId', label: 'Inscripcion ID' },
    { key: 'medioPago', label: 'Medio' },
    { key: 'monto', label: 'Monto', render: (i: Pago) => `$${i.monto.toFixed(2)}` },
    { key: 'estadoPago', label: 'Estado', render: (i: Pago) => <span className={`badge ${estadoBadge(i.estadoPago)}`}>{i.estadoPago}</span> },
    { key: 'fechaPago', label: 'Fecha Pago', render: (i: Pago) => i.fechaPago ? new Date(i.fechaPago).toLocaleDateString('es-AR') : '-' },
  ]

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="font-bold text-slate-800">Pagos</h2>
        <button className="btn-primary" onClick={() => { setForm({ inscripcionId: 0, medioPago: '', monto: 0 }); setError(''); setShowForm(true) }}><Plus className="w-4 h-4 mr-1 inline" />Nuevo Pago</button>
      </div>
      {error && <div className="alert-danger">{error}</div>}

      <DataTable
        data={data as unknown as Record<string, unknown>[]}
        columns={columns as never}
        actions={(item: unknown) => {
          const pago = item as Pago
          return pago.estadoPago === 'Pendiente' ? (
            <>
              <button className="btn-outline-success btn-sm p-1.5" onClick={() => handleEstado(pago.id, 'Confirmado')} title="Confirmar"><Check className="w-4 h-4" /></button>
              <button className="btn-outline-danger btn-sm p-1.5" onClick={() => handleEstado(pago.id, 'Rechazado')} title="Rechazar"><X className="w-4 h-4" /></button>
            </>
          ) : null
        }}
      />

      <FormModal show={showForm} title="Nuevo Pago" onClose={() => setShowForm(false)} onSubmit={handleSubmit} loading={loading}>
        {error && <div className="alert-danger">{error}</div>}
        <div className="mb-3"><label className="form-label">Inscripcion ID *</label><input type="number" className="form-input" value={form.inscripcionId || ''} onChange={e => setForm({ ...form, inscripcionId: Number(e.target.value) })} required /></div>
        <div className="mb-3"><label className="form-label">Medio de Pago *</label>
          <select className="form-select" value={form.medioPago} onChange={e => setForm({ ...form, medioPago: e.target.value })} required>
            <option value="">Seleccionar...</option>
            <option value="Transferencia">Transferencia</option>
            <option value="Tarjeta">Tarjeta</option>
            <option value="Efectivo">Efectivo</option>
            <option value="MercadoPago">MercadoPago</option>
          </select>
        </div>
        <div className="mb-3"><label className="form-label">Monto *</label><input type="number" step="0.01" className="form-input" value={form.monto || ''} onChange={e => setForm({ ...form, monto: Number(e.target.value) })} required /></div>
        <div className="mb-3"><label className="form-label">Referencia Externa</label><input type="text" className="form-input" value={form.referenciaExterna || ''} onChange={e => setForm({ ...form, referenciaExterna: e.target.value })} /></div>
        <div className="mb-3"><label className="form-label">Observaciones</label><textarea className="form-input" rows={2} value={form.observaciones || ''} onChange={e => setForm({ ...form, observaciones: e.target.value })} /></div>
      </FormModal>
    </div>
  )
}

export default PagosAdminPage
