import { api } from './api'
import type { Evento, EventoForm } from '../types/models'

const BASE = '/eventos'

export const eventosService = {
  getAll: () => api.get<Evento[]>(BASE),
  getActivos: () => api.get<Evento[]>(`${BASE}/activos`),
  getById: (id: number) => api.get<Evento>(`${BASE}/${id}`),
  create: (data: EventoForm) => api.post<Evento>(BASE, data),
  update: (id: number, data: EventoForm & { activo: boolean }) => api.put<void>(`${BASE}/${id}`, data),
  remove: (id: number) => api.delete<void>(`${BASE}/${id}`),
  uploadTerminos: async (id: number, file: File): Promise<{ terminosArchivo: string }> => {
    const formData = new FormData()
    formData.append('archivo', file)
    const token = localStorage.getItem('sad_token')
    const res = await fetch(`/api/eventos/${id}/terminos`, {
      method: 'POST',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: formData,
    })
    if (!res.ok) {
      const body = await res.json().catch(() => ({ error: 'Error al subir archivo' }))
      throw new Error(body.error || `Error ${res.status}`)
    }
    return res.json()
  },
  deleteTerminos: (id: number) => api.delete<void>(`${BASE}/${id}/terminos`),
}
