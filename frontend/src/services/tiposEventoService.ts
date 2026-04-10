import { api } from './api'
import type { TipoEvento, TipoEventoForm } from '../types/models'

const BASE = '/tiposevento'

export const tiposEventoService = {
  getAll: () => api.get<TipoEvento[]>(BASE),
  getById: (id: number) => api.get<TipoEvento>(`${BASE}/${id}`),
  create: (data: TipoEventoForm) => api.post<TipoEvento>(BASE, data),
  update: (id: number, data: TipoEventoForm) => api.put<void>(`${BASE}/${id}`, data),
  remove: (id: number) => api.delete<void>(`${BASE}/${id}`),
}
