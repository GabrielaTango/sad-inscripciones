import { useEffect, useState } from 'react'
import { CreditCard, Save } from 'lucide-react'
import { configuracionMercadoPagoService } from '../../services/configuracionMercadoPagoService'
import type { ConfiguracionMercadoPagoForm } from '../../types/models'

const emptyForm: ConfiguracionMercadoPagoForm = {
  accessToken: '',
  frontendBaseUrl: '',
}

const ConfiguracionMercadoPagoPage = () => {
  const [form, setForm] = useState<ConfiguracionMercadoPagoForm>(emptyForm)
  const [tieneAccessToken, setTieneAccessToken] = useState(false)
  const [accessTokenPreview, setAccessTokenPreview] = useState<string | null>(null)
  const [updatedAt, setUpdatedAt] = useState<string | undefined>()
  const [updatedBy, setUpdatedBy] = useState<string | null | undefined>()
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState<{ type: 'ok' | 'err'; text: string } | null>(null)

  const load = async () => {
    try {
      const data = await configuracionMercadoPagoService.get()
      setForm({
        accessToken: '',
        frontendBaseUrl: data.frontendBaseUrl ?? '',
      })
      setTieneAccessToken(data.tieneAccessToken)
      setAccessTokenPreview(data.accessTokenPreview ?? null)
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
      const payload: ConfiguracionMercadoPagoForm = { ...form }
      if (!payload.accessToken) delete payload.accessToken // backend preserva el actual
      await configuracionMercadoPagoService.update(payload)
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
        <CreditCard className="text-primary" />
        <h2 className="font-bold text-slate-800">Configuración de MercadoPago</h2>
      </div>

      <p className="text-sm text-slate-600 mb-4">
        Credenciales y parámetros usados al generar preferencias de pago contra MercadoPago.
        El AccessToken se guarda cifrado en la base de datos.
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
            <label className="form-label">
              Access Token {tieneAccessToken && <span className="text-xs text-slate-500">(dejá vacío para no cambiar)</span>}
            </label>
            <input
              type="password"
              className="form-input"
              autoComplete="new-password"
              value={form.accessToken ?? ''}
              placeholder={tieneAccessToken ? (accessTokenPreview ? `${accessTokenPreview}` : '••••••••') : 'APP_USR-... o TEST-...'}
              onChange={(e) => setForm({ ...form, accessToken: e.target.value })}
            />
            <p className="text-xs text-slate-500 mt-1">
              Obtené tu Access Token desde el panel de MercadoPago Developers. Usá tokens TEST para sandbox.
            </p>
          </div>
        </fieldset>

        <fieldset className="space-y-3">
          <legend className="text-sm font-semibold text-slate-700">URLs</legend>
          <div>
            <label className="form-label">Frontend Base URL *</label>
            <input
              className="form-input"
              required
              value={form.frontendBaseUrl}
              onChange={(e) => setForm({ ...form, frontendBaseUrl: e.target.value })}
              placeholder="https://tu-dominio.com"
            />
            <p className="text-xs text-slate-500 mt-1">
              MercadoPago redirige al usuario a <code>{'{Frontend Base URL}/pago/resultado'}</code> tras el pago.
              No incluyas barra final.
            </p>
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

export default ConfiguracionMercadoPagoPage
