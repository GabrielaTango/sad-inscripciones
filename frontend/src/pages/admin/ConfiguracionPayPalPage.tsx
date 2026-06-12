import { useEffect, useState } from 'react'
import { Wallet, Save } from 'lucide-react'
import { configuracionPayPalService } from '../../services/configuracionPayPalService'
import type { ConfiguracionPayPalForm } from '../../types/models'

const emptyForm: ConfiguracionPayPalForm = {
  clientId: '',
  moneda: 'USD',
}

const ConfiguracionPayPalPage = () => {
  const [form, setForm] = useState<ConfiguracionPayPalForm>(emptyForm)
  const [updatedAt, setUpdatedAt] = useState<string | undefined>()
  const [updatedBy, setUpdatedBy] = useState<string | null | undefined>()
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState<{ type: 'ok' | 'err'; text: string } | null>(null)

  const load = async () => {
    try {
      const data = await configuracionPayPalService.get()
      setForm({ clientId: data.clientId ?? '', moneda: data.moneda || 'USD' })
      setUpdatedAt(data.updatedAt)
      setUpdatedBy(data.updatedBy)
    } catch (err) {
      setMessage({ type: 'err', text: err instanceof Error ? err.message : 'Error cargando configuración' })
    }
  }

  useEffect(() => { load() }, [])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setMessage(null)
    try {
      await configuracionPayPalService.update(form)
      setMessage({ type: 'ok', text: 'Configuración guardada.' })
      await load()
    } catch (err) {
      setMessage({ type: 'err', text: err instanceof Error ? err.message : 'Error al guardar' })
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="max-w-3xl">
      <div className="flex items-center gap-2 mb-4">
        <Wallet className="text-primary" />
        <h2 className="font-bold text-slate-800">Configuración de PayPal</h2>
      </div>

      <p className="text-sm text-slate-600 mb-4">
        Credenciales usadas para cobrar a los alumnos extranjeros vía PayPal (un único pago, en dólares).
        El Client-ID es público: se usa para cargar el checkout de PayPal en el navegador.
      </p>

      {message && (
        <div className={message.type === 'ok' ? 'alert-success' : 'alert-danger'}>
          {message.text}
        </div>
      )}

      <form onSubmit={handleSubmit} className="bg-white rounded shadow p-5 space-y-5">
        <fieldset className="space-y-3">
          <legend className="text-sm font-semibold text-slate-700">Credenciales</legend>
          <div>
            <label className="form-label">Client-ID *</label>
            <input
              className="form-input"
              required
              value={form.clientId}
              onChange={(e) => setForm({ ...form, clientId: e.target.value })}
              placeholder="Absy_eJmmpJ2lSeW..."
            />
            <p className="text-xs text-slate-500 mt-1">
              Obtené tu Client-ID desde el panel de PayPal Developers (app REST).
            </p>
          </div>
          <div>
            <label className="form-label">Moneda</label>
            <input
              className="form-input"
              value={form.moneda}
              onChange={(e) => setForm({ ...form, moneda: e.target.value.toUpperCase() })}
              placeholder="USD"
              maxLength={10}
            />
            <p className="text-xs text-slate-500 mt-1">PayPal cobra en esta moneda. Por defecto USD.</p>
          </div>
        </fieldset>

        {(updatedAt || updatedBy) && (
          <p className="text-xs text-slate-500">
            Última actualización: {updatedAt ? new Date(updatedAt).toLocaleString() : '-'}
            {updatedBy ? ` por ${updatedBy}` : ''}
          </p>
        )}

        <div className="flex justify-end pt-2 border-t">
          <button type="submit" className="btn-primary" disabled={loading}>
            <Save size={16} className="inline mr-1" />
            {loading ? 'Guardando...' : 'Guardar configuración'}
          </button>
        </div>
      </form>
    </div>
  )
}

export default ConfiguracionPayPalPage
