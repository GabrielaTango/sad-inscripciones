import { useEffect, useState } from 'react'
import { Mail, Send, Save } from 'lucide-react'
import { configuracionEmailService } from '../../services/configuracionEmailService'
import type { ConfiguracionEmailForm } from '../../types/models'

const emptyForm: ConfiguracionEmailForm = {
  host: '',
  port: 587,
  enableSsl: true,
  usuario: '',
  password: '',
  fromEmail: '',
  fromName: 'Inscripciones SAD',
  replyTo: '',
  bccCopia: '',
  asunto: 'Confirmación de inscripción - {{Evento}}',
  activo: false,
  ignorarCertificadoSsl: false,
}

const ConfiguracionEmailPage = () => {
  const [form, setForm] = useState<ConfiguracionEmailForm>(emptyForm)
  const [tienePassword, setTienePassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [testTo, setTestTo] = useState('')
  const [testing, setTesting] = useState(false)
  const [message, setMessage] = useState<{ type: 'ok' | 'err'; text: string } | null>(null)

  const load = async () => {
    try {
      const data = await configuracionEmailService.get()
      setForm({
        host: data.host,
        port: data.port,
        enableSsl: data.enableSsl,
        usuario: data.usuario,
        password: '',
        fromEmail: data.fromEmail,
        fromName: data.fromName,
        replyTo: data.replyTo ?? '',
        bccCopia: data.bccCopia ?? '',
        asunto: data.asunto,
        activo: data.activo,
        ignorarCertificadoSsl: data.ignorarCertificadoSsl,
      })
      setTienePassword(data.tienePassword)
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
      const payload: ConfiguracionEmailForm = { ...form }
      if (!payload.password) delete payload.password // backend preserva el actual
      await configuracionEmailService.update(payload)
      setMessage({ type: 'ok', text: 'Configuración guardada.' })
      await load()
    } catch (err) {
      setMessage({ type: 'err', text: err instanceof Error ? err.message : 'Error al guardar' })
    } finally {
      setLoading(false)
    }
  }

  const handleTest = async () => {
    if (!testTo.trim()) {
      setMessage({ type: 'err', text: 'Ingresá un destinatario para la prueba.' })
      return
    }
    setTesting(true)
    setMessage(null)
    try {
      const r = await configuracionEmailService.enviarPrueba(testTo.trim())
      setMessage({ type: 'ok', text: r.message || 'Mail de prueba enviado.' })
    } catch (err) {
      setMessage({ type: 'err', text: err instanceof Error ? err.message : 'Error al enviar prueba' })
    } finally {
      setTesting(false)
    }
  }

  return (
    <div className="max-w-3xl">
      <div className="flex items-center gap-2 mb-4">
        <Mail className="text-primary" />
        <h2 className="font-bold text-slate-800">Configuración de Email</h2>
      </div>

      <p className="text-sm text-slate-600 mb-4">
        SMTP que se usa para mandar el mail de confirmación cuando una inscripción pasa al estado <em>Confirmada</em>.
      </p>

      {message && (
        <div className={message.type === 'ok' ? 'alert-success' : 'alert-danger'}>
          {message.text}
        </div>
      )}

      <form onSubmit={handleSubmit} className="bg-white rounded shadow p-5 space-y-5">
        <fieldset className="space-y-3">
          <legend className="text-sm font-semibold text-slate-700">Servidor SMTP</legend>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div className="md:col-span-2">
              <label className="form-label">Host *</label>
              <input className="form-input" required value={form.host}
                     onChange={(e) => setForm({ ...form, host: e.target.value })}
                     placeholder="smtp.gmail.com" />
            </div>
            <div>
              <label className="form-label">Puerto *</label>
              <input type="number" className="form-input" required value={form.port}
                     onChange={(e) => setForm({ ...form, port: parseInt(e.target.value || '0', 10) })} />
            </div>
          </div>
          <div className="form-check">
            <input type="checkbox" id="ssl" className="form-check-input" checked={form.enableSsl}
                   onChange={(e) => setForm({ ...form, enableSsl: e.target.checked })} />
            <label htmlFor="ssl" className="text-sm">Habilitar SSL/TLS</label>
          </div>
          <div className="form-check">
            <input type="checkbox" id="ignoraCert" className="form-check-input" checked={form.ignorarCertificadoSsl}
                   onChange={(e) => setForm({ ...form, ignorarCertificadoSsl: e.target.checked })} />
            <label htmlFor="ignoraCert" className="text-sm">
              Ignorar errores de certificado SSL
              <span className="text-xs text-slate-500 block">
                Activá esto solo si tu hosting usa un certificado de otro hostname (ej: hostings compartidos como DattaWeb).
              </span>
            </label>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="form-label">Usuario</label>
              <input className="form-input" value={form.usuario}
                     onChange={(e) => setForm({ ...form, usuario: e.target.value })} />
            </div>
            <div>
              <label className="form-label">
                Contraseña {tienePassword && <span className="text-xs text-slate-500">(dejá vacío para no cambiar)</span>}
              </label>
              <input type="password" className="form-input" value={form.password ?? ''}
                     placeholder={tienePassword ? '••••••••' : ''}
                     onChange={(e) => setForm({ ...form, password: e.target.value })} />
            </div>
          </div>
        </fieldset>

        <fieldset className="space-y-3">
          <legend className="text-sm font-semibold text-slate-700">Remitente</legend>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="form-label">Email del remitente *</label>
              <input type="email" className="form-input" required value={form.fromEmail}
                     onChange={(e) => setForm({ ...form, fromEmail: e.target.value })} />
            </div>
            <div>
              <label className="form-label">Nombre del remitente</label>
              <input className="form-input" value={form.fromName}
                     onChange={(e) => setForm({ ...form, fromName: e.target.value })} />
            </div>
            <div>
              <label className="form-label">Reply-To</label>
              <input className="form-input" value={form.replyTo ?? ''}
                     onChange={(e) => setForm({ ...form, replyTo: e.target.value })} />
            </div>
            <div>
              <label className="form-label">BCC (separados por coma)</label>
              <input className="form-input" value={form.bccCopia ?? ''}
                     onChange={(e) => setForm({ ...form, bccCopia: e.target.value })} />
            </div>
          </div>
        </fieldset>

        <fieldset className="space-y-3">
          <legend className="text-sm font-semibold text-slate-700">Mensaje</legend>
          <div>
            <label className="form-label">Asunto</label>
            <input className="form-input" value={form.asunto}
                   onChange={(e) => setForm({ ...form, asunto: e.target.value })} />
            <p className="text-xs text-slate-500 mt-1">
              Variables disponibles: <code>{'{{Nombre}}'}</code> <code>{'{{Apellido}}'}</code> <code>{'{{Evento}}'}</code> <code>{'{{Lugar}}'}</code> <code>{'{{FechaEvento}}'}</code> <code>{'{{Importe}}'}</code> <code>{'{{Cuotas}}'}</code> <code>{'{{NumeroInscripcion}}'}</code>
            </p>
          </div>
          <div className="form-check">
            <input type="checkbox" id="activo" className="form-check-input" checked={form.activo}
                   onChange={(e) => setForm({ ...form, activo: e.target.checked })} />
            <label htmlFor="activo" className="text-sm">
              Activo — si está apagado, no se mandan mails (útil para testing).
            </label>
          </div>
        </fieldset>

        <div className="flex justify-end pt-2 border-t">
          <button type="submit" className="btn-primary" disabled={loading}>
            <Save size={16} className="inline mr-1" />
            {loading ? 'Guardando...' : 'Guardar configuración'}
          </button>
        </div>
      </form>

      <div className="bg-white rounded shadow p-5 mt-5">
        <h3 className="font-semibold text-slate-700 mb-2">Probar configuración</h3>
        <p className="text-sm text-slate-600 mb-3">
          Envía un mail de prueba con la configuración actual. Guardá los cambios antes de probar.
        </p>
        <div className="flex gap-2">
          <input type="email" className="form-input flex-1" placeholder="destinatario@ejemplo.com"
                 value={testTo} onChange={(e) => setTestTo(e.target.value)} />
          <button type="button" className="btn-primary" onClick={handleTest} disabled={testing}>
            <Send size={16} className="inline mr-1" />
            {testing ? 'Enviando...' : 'Enviar prueba'}
          </button>
        </div>
      </div>
    </div>
  )
}

export default ConfiguracionEmailPage
