import { api } from './api'
import type { EventoPrecio, EventoPrecioForm } from '../types/models'

const BASE = '/eventoprecios'

export const eventoPreciosService = {
  getAll: () => api.get<EventoPrecio[]>(BASE),
  getByEventoId: (eventoId: number) => api.get<EventoPrecio[]>(`${BASE}?eventoId=${eventoId}`),
  getById: (id: number) => api.get<EventoPrecio>(`${BASE}/${id}`),
  create: (data: EventoPrecioForm) => api.post<EventoPrecio>(BASE, data),
  update: (id: number, data: EventoPrecioForm) => api.put<void>(`${BASE}/${id}`, data),
  remove: (id: number) => api.delete<void>(`${BASE}/${id}`),
}
