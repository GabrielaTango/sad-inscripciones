import { api } from './api'
import type { TipoAlumno, TipoAlumnoForm } from '../types/models'

const BASE = '/tiposalumno'

export const tiposAlumnoService = {
  getAll: () => api.get<TipoAlumno[]>(BASE),
  getById: (id: number) => api.get<TipoAlumno>(`${BASE}/${id}`),
  create: (data: TipoAlumnoForm) => api.post<TipoAlumno>(BASE, data),
  update: (id: number, data: TipoAlumnoForm) => api.put<void>(`${BASE}/${id}`, data),
  remove: (id: number) => api.delete<void>(`${BASE}/${id}`),
}
