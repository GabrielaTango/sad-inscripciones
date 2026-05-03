import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Mail, Pencil } from 'lucide-react'
import { emailTemplatesService } from '../../services/emailTemplatesService'
import type { EmailTemplateListItem } from '../../types/models'

const EmailTemplatesListPage = () => {
  const [items, setItems] = useState<EmailTemplateListItem[]>([])
  const [error, setError] = useState('')

  useEffect(() => {
    emailTemplatesService.list()
      .then(setItems)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error'))
  }, [])

  return (
    <div className="max-w-4xl">
      <div className="flex items-center gap-2 mb-4">
        <Mail className="text-primary" />
        <h2 className="font-bold text-slate-800">Templates de Email</h2>
      </div>

      <p className="text-sm text-slate-600 mb-4">
        Editá los mails que el sistema envía automáticamente. Los cambios se guardan en la base
        y aplican a partir del próximo envío.
      </p>

      {error && <div className="alert-danger">{error}</div>}

      <div className="bg-white rounded shadow overflow-hidden">
        <table className="w-full">
          <thead className="bg-slate-50">
            <tr>
              <th className="text-left p-3 text-sm font-semibold text-slate-700">Template</th>
              <th className="text-left p-3 text-sm font-semibold text-slate-700">Código</th>
              <th className="text-center p-3 text-sm font-semibold text-slate-700">Estado</th>
              <th className="text-left p-3 text-sm font-semibold text-slate-700">Última edición</th>
              <th className="p-3"></th>
            </tr>
          </thead>
          <tbody>
            {items.length === 0 && !error && (
              <tr><td colSpan={5} className="p-6 text-center text-slate-500">Sin templates.</td></tr>
            )}
            {items.map((t) => (
              <tr key={t.codigo} className="border-t hover:bg-slate-50">
                <td className="p-3 font-medium">{t.nombre}</td>
                <td className="p-3 text-sm text-slate-500"><code>{t.codigo}</code></td>
                <td className="p-3 text-center">
                  {t.activo
                    ? <span className="badge bg-green-100 text-green-700">Activo</span>
                    : <span className="badge bg-gray-100 text-gray-700">Inactivo</span>}
                </td>
                <td className="p-3 text-sm text-slate-600">
                  {new Date(t.updatedAt).toLocaleString('es-AR')}
                </td>
                <td className="p-3 text-right">
                  <Link to={`/admin/email-templates/${t.codigo}`} className="btn-primary text-sm">
                    <Pencil size={14} className="inline mr-1" />Editar
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

export default EmailTemplatesListPage
