import { api } from './api'
import type { EventoProvinciaBeneficio, EventoProvinciaBeneficioForm } from '../types/models'

const BASE = '/eventoprovinciabeneficios'

export const eventoProvinciaBeneficiosService = {
  getAll: () => api.get<EventoProvinciaBeneficio[]>(BASE),
  getByEventoId: (eventoId: number) => api.get<EventoProvinciaBeneficio[]>(`${BASE}?eventoId=${eventoId}`),
  getById: (id: number) => api.get<EventoProvinciaBeneficio>(`${BASE}/${id}`),
  create: (data: EventoProvinciaBeneficioForm) => api.post<EventoProvinciaBeneficio>(BASE, data),
  update: (id: number, data: EventoProvinciaBeneficioForm) => api.put<void>(`${BASE}/${id}`, data),
  remove: (id: number) => api.delete<void>(`${BASE}/${id}`),
}
