import { useEffect, useState } from 'react'
import { MessageSquare, Save } from 'lucide-react'
import {
  configuracionContactoService,
  type ConfiguracionContactoForm,
} from '@/services/configuracionContactoService'

const emptyForm: ConfiguracionContactoForm = {
  emailDestino: '',
  activo: false,
}

const ConfiguracionContactoPage = () => {
  const [form, setForm] = useState<ConfiguracionContactoForm>(emptyForm)
  const [updatedAt, setUpdatedAt] = useState<string | null>(null)
  const [updatedBy, setUpdatedBy] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState<{ type: 'ok' | 'err'; text: string } | null>(null)

  const load = async () => {
    try {
      const data = await configuracionContactoService.get()
      setForm({ emailDestino: data.emailDestino, activo: data.activo })
      setUpdatedAt(data.updatedAt)
      setUpdatedBy(data.updatedBy)
    } catch (err) {
      setMessage({
        type: 'err',
        text: err instanceof Error ? err.message : 'Error cargando configuración',
      })
    }
  }

  useEffect(() => {
    load()
  }, [])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setMessage(null)
    try {
      await configuracionContactoService.update(form)
      setMessage({ type: 'ok', text: 'Configuración guardada.' })
      await load()
    } catch (err) {
      setMessage({
        type: 'err',
        text: err instanceof Error ? err.message : 'Error al guardar',
      })
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="max-w-3xl">
      <div className="flex items-center gap-2 mb-4">
        <MessageSquare className="text-primary" />
        <h2 className="font-bold text-slate-800">Configuración de Contacto</h2>
      </div>

      <p className="text-sm text-slate-600 mb-4">
        Email destino al que se envían las consultas enviadas desde el formulario público de{' '}
        <em>/contacto</em>. El envío usa la misma configuración SMTP que el resto de los mails
        (ver <em>Config. Email</em>).
      </p>

      {message && (
        <div
          className={
            message.type === 'ok'
              ? 'rounded-lg bg-green-50 border border-green-200 px-4 py-3 text-sm text-green-700 mb-4'
              : 'rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700 mb-4'
          }
        >
          {message.text}
        </div>
      )}

      <form
        onSubmit={handleSubmit}
        className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 space-y-4"
      >
        <div>
          <label className="form-label">Email destino *</label>
          <input
            type="email"
            className="form-input"
            value={form.emailDestino}
            onChange={(e) => setForm({ ...form, emailDestino: e.target.value })}
            placeholder="contacto@diabetes.org.ar"
            required
          />
          <p className="text-xs text-slate-500 mt-1">
            Acá llegan las consultas. Las respuestas que envíes desde tu cliente de mail van
            directamente al usuario (se setea <code>Reply-To</code>).
          </p>
        </div>

        <div className="flex items-center gap-2">
          <input
            id="activo"
            type="checkbox"
            checked={form.activo}
            onChange={(e) => setForm({ ...form, activo: e.target.checked })}
          />
          <label htmlFor="activo" className="text-sm text-slate-700">
            Activo (si está apagado, las consultas se guardan en DB pero no se envía mail)
          </label>
        </div>

        {updatedAt && (
          <p className="text-xs text-slate-500">
            Última actualización: {new Date(updatedAt).toLocaleString('es-AR')}
            {updatedBy ? ` por ${updatedBy}` : ''}
          </p>
        )}

        <div className="pt-2">
          <button type="submit" className="btn-primary" disabled={loading}>
            <Save className="w-4 h-4 mr-2 inline" />
            {loading ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </form>
    </div>
  )
}

export default ConfiguracionContactoPage
