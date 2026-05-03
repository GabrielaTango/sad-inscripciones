import { api } from './api'
import type { EmailTemplate, EmailTemplateForm, EmailTemplateListItem } from '../types/models'

const BASE = '/admin/email-templates'

export const emailTemplatesService = {
  list: () => api.get<EmailTemplateListItem[]>(BASE),
  get: (codigo: string) => api.get<EmailTemplate>(`${BASE}/${codigo}`),
  update: (codigo: string, data: EmailTemplateForm) => api.put<void>(`${BASE}/${codigo}`, data),
  enviarPrueba: (codigo: string, destinatario: string, asunto: string, bodyHtml: string) =>
    api.post<{ message: string }>(`${BASE}/${codigo}/test`, { destinatario, asunto, bodyHtml }),
}

/**
 * Sube una imagen para usar en templates de mail.
 * Devuelve la URL absoluta servida desde /uploads/email/.
 */
export async function uploadEmailImage(file: File): Promise<{ url: string }> {
  const token = localStorage.getItem('sad_token')
  const fd = new FormData()
  fd.append('file', file)

  const res = await fetch('/api/admin/email-uploads', {
    method: 'POST',
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    body: fd,
  })
  if (!res.ok) {
    const body = await res.json().catch(() => ({ message: 'Error subiendo imagen' }))
    throw new Error(body.message || `Error ${res.status}`)
  }
  return res.json()
}
