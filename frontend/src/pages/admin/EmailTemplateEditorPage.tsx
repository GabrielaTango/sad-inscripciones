import { useEffect, useRef, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Save, Send, Eye } from 'lucide-react'
import EmailEditor, { type EditorRef } from 'react-email-editor'
import { emailTemplatesService, uploadEmailImage } from '../../services/emailTemplatesService'
import type { EmailTemplate } from '../../types/models'

const MERGE_TAGS = {
  Nombre: { name: 'Nombre', value: '{{Nombre}}', sample: 'Juan' },
  Apellido: { name: 'Apellido', value: '{{Apellido}}', sample: 'Pérez' },
  Evento: { name: 'Evento', value: '{{Evento}}', sample: 'Congreso SAD 2026' },
  Lugar: { name: 'Lugar', value: '{{Lugar}}', sample: 'Hotel Sheraton' },
  FechaEvento: { name: 'Fecha del evento', value: '{{FechaEvento}}', sample: '15/08/2026' },
  Importe: { name: 'Importe', value: '{{Importe}}', sample: '$ 25.000,00' },
  MontoReserva: { name: 'Monto de la reserva', value: '{{MontoReserva}}', sample: '$ 7.500,00' },
  SaldoRestante: { name: 'Saldo restante', value: '{{SaldoRestante}}', sample: '$ 17.500,00' },
  Cuotas: { name: 'Cuotas', value: '{{Cuotas}}', sample: '3' },
  NumeroInscripcion: { name: 'N.º de inscripción', value: '{{NumeroInscripcion}}', sample: '1234' },
}

const EmailTemplateEditorPage = () => {
  const { codigo = '' } = useParams<{ codigo: string }>()
  const navigate = useNavigate()
  const emailEditorRef = useRef<EditorRef>(null)

  const [template, setTemplate] = useState<EmailTemplate | null>(null)
  const [asunto, setAsunto] = useState('')
  const [activo, setActivo] = useState(true)
  const [editorReady, setEditorReady] = useState(false)
  const [saving, setSaving] = useState(false)
  const [testing, setTesting] = useState(false)
  const [testTo, setTestTo] = useState('')
  const [message, setMessage] = useState<{ type: 'ok' | 'err'; text: string } | null>(null)

  // Carga inicial
  useEffect(() => {
    if (!codigo) return
    emailTemplatesService.get(codigo)
      .then((t) => {
        setTemplate(t)
        setAsunto(t.asunto)
        setActivo(t.activo)
      })
      .catch((e) => setMessage({ type: 'err', text: e instanceof Error ? e.message : 'Error' }))
  }, [codigo])

  // Cuando el editor está listo Y tenemos el template cargado, le inyectamos el design.
  useEffect(() => {
    if (!editorReady || !template || !emailEditorRef.current?.editor) return

    const unlayer = emailEditorRef.current.editor

    if (template.bodyJson) {
      try {
        unlayer.loadDesign(JSON.parse(template.bodyJson))
      } catch {
        // ignoramos: si el JSON es inválido, queda canvas en blanco
      }
    }
  }, [editorReady, template])

  const onReady = () => {
    const unlayer = emailEditorRef.current?.editor
    if (!unlayer) return

    // Custom upload: las imágenes van a /uploads/email/ del backend.
    unlayer.registerCallback('image', (file: { attachments: File[] }, done: (data: { progress: number; url: string }) => void) => {
      uploadEmailImage(file.attachments[0])
        .then(({ url }) => done({ progress: 100, url }))
        .catch((e) => {
          setMessage({ type: 'err', text: e instanceof Error ? e.message : 'Error subiendo imagen' })
          done({ progress: 0, url: '' })
        })
    })

    setEditorReady(true)
  }

  const exportAndDo = (action: (html: string, designJson: string) => Promise<void> | void) => {
    const unlayer = emailEditorRef.current?.editor
    if (!unlayer) return
    unlayer.exportHtml((data: { design: object; html: string }) => {
      void action(data.html, JSON.stringify(data.design))
    })
  }

  const handleSave = () => {
    if (!codigo) return
    setMessage(null)
    setSaving(true)
    exportAndDo(async (html, designJson) => {
      try {
        await emailTemplatesService.update(codigo, {
          asunto,
          bodyHtml: html,
          bodyJson: designJson,
          activo,
        })
        setMessage({ type: 'ok', text: 'Template guardado.' })
      } catch (e) {
        setMessage({ type: 'err', text: e instanceof Error ? e.message : 'Error al guardar' })
      } finally {
        setSaving(false)
      }
    })
  }

  const handleTest = () => {
    if (!testTo.trim()) {
      setMessage({ type: 'err', text: 'Ingresá un destinatario para la prueba.' })
      return
    }
    setMessage(null)
    setTesting(true)
    exportAndDo(async (html) => {
      try {
        const r = await emailTemplatesService.enviarPrueba(codigo, testTo.trim(), asunto, html)
        setMessage({ type: 'ok', text: r.message || 'Mail de prueba enviado.' })
      } catch (e) {
        setMessage({ type: 'err', text: e instanceof Error ? e.message : 'Error al enviar prueba' })
      } finally {
        setTesting(false)
      }
    })
  }

  const handlePreview = () => {
    const unlayer = emailEditorRef.current?.editor as { showPreview?: (mode: string) => void } | undefined
    unlayer?.showPreview?.('desktop')
  }

  return (
    <div className="flex flex-col h-[calc(100vh-100px)]">
      <div className="flex flex-wrap items-center gap-3 mb-3">
        <button onClick={() => navigate('/admin/email-templates')}
                className="text-slate-600 hover:text-slate-900">
          <ArrowLeft size={18} className="inline mr-1" />Volver
        </button>
        <h2 className="font-bold text-slate-800 flex-1">
          {template?.nombre ?? 'Cargando...'}
        </h2>
        <button onClick={handlePreview} className="btn-secondary">
          <Eye size={16} className="inline mr-1" />Preview
        </button>
        <button onClick={handleSave} disabled={saving || !editorReady} className="btn-primary">
          <Save size={16} className="inline mr-1" />
          {saving ? 'Guardando...' : 'Guardar'}
        </button>
      </div>

      {message && (
        <div className={message.type === 'ok' ? 'alert-success' : 'alert-danger'}>
          {message.text}
        </div>
      )}

      <div className="bg-white rounded shadow p-3 mb-3">
        <div className="grid grid-cols-1 md:grid-cols-12 gap-3 items-end">
          <div className="md:col-span-7">
            <label className="form-label text-xs">Asunto</label>
            <input className="form-input" value={asunto} onChange={(e) => setAsunto(e.target.value)} />
          </div>
          <div className="md:col-span-2">
            <label className="form-label text-xs">Estado</label>
            <select className="form-input" value={activo ? '1' : '0'}
                    onChange={(e) => setActivo(e.target.value === '1')}>
              <option value="1">Activo</option>
              <option value="0">Inactivo</option>
            </select>
          </div>
          <div className="md:col-span-3 flex gap-2">
            <input type="email" className="form-input flex-1" placeholder="probar@ejemplo.com"
                   value={testTo} onChange={(e) => setTestTo(e.target.value)} />
            <button onClick={handleTest} disabled={testing || !editorReady} className="btn-secondary">
              <Send size={14} className="inline mr-1" />
              {testing ? '...' : 'Probar'}
            </button>
          </div>
        </div>
      </div>

      {template && !template.bodyJson && (
        <div className="alert-info mb-3 text-sm">
          Este template todavía no tiene un diseño visual guardado. Empezá a armarlo desde cero;
          al guardar, el diseño visual queda en la base y vas a poder seguir editándolo desde acá.
        </div>
      )}

      <div className="flex-1 min-h-[500px] border rounded overflow-hidden bg-white">
        <EmailEditor
          ref={emailEditorRef}
          onReady={onReady}
          minHeight="100%"
          options={{
            mergeTags: MERGE_TAGS,
            appearance: { theme: 'light' },
            displayMode: 'email',
          }}
        />
      </div>
    </div>
  )
}

export default EmailTemplateEditorPage
