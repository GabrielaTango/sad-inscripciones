import { api } from './api'
import type { Promocion, PromocionForm } from '../types/models'

const BASE = '/promociones'

export const promocionesService = {
  getAll: () => api.get<Promocion[]>(BASE),
  getById: (id: number) => api.get<Promocion>(`${BASE}/${id}`),
  create: (data: PromocionForm) => api.post<Promocion>(BASE, data),
  update: (id: number, data: PromocionForm) => api.put<void>(`${BASE}/${id}`, data),
  remove: (id: number) => api.delete<void>(`${BASE}/${id}`),
}
