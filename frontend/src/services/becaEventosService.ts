import { api } from './api'
import type { BecaEvento, BecaEventoForm } from '../types/models'

const BASE = '/becaeventos'

export const becaEventosService = {
  getAll: () => api.get<BecaEvento[]>(BASE),
  getByEventoId: (eventoId: number) => api.get<BecaEvento[]>(`${BASE}?eventoId=${eventoId}`),
  getById: (id: number) => api.get<BecaEvento>(`${BASE}/${id}`),
  create: (data: BecaEventoForm) => api.post<BecaEvento>(BASE, data),
  update: (id: number, data: BecaEventoForm) => api.put<void>(`${BASE}/${id}`, data),
  remove: (id: number) => api.delete<void>(`${BASE}/${id}`),
}
