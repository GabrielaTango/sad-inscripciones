import { api } from './api'
import type { Pago, PagoForm } from '../types/models'

const BASE = '/pagos'

export const pagosService = {
  getAll: () => api.get<Pago[]>(BASE),
  getByInscripcionId: (inscripcionId: number) => api.get<Pago[]>(`${BASE}?inscripcionId=${inscripcionId}`),
  getById: (id: number) => api.get<Pago>(`${BASE}/${id}`),
  create: (data: PagoForm) => api.post<Pago>(BASE, data),
  updateEstado: (id: number, estadoPago: string) => api.patch<void>(`${BASE}/${id}/estado`, { estadoPago }),
  remove: (id: number) => api.delete<void>(`${BASE}/${id}`),
}
