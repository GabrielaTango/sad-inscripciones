import { api } from './api'
import type { Usuario, UsuarioCreateForm, UsuarioUpdateForm } from '../types/models'

const BASE = '/usuarios'

export const usuariosService = {
  getAll: () => api.get<Usuario[]>(BASE),
  getById: (id: number) => api.get<Usuario>(`${BASE}/${id}`),
  create: (data: UsuarioCreateForm) => api.post<Usuario>(BASE, data),
  update: (id: number, data: UsuarioUpdateForm) => api.put<void>(`${BASE}/${id}`, data),
  cambiarPassword: (id: number, data: { passwordActual: string; passwordNueva: string }) =>
    api.put<void>(`${BASE}/${id}/password`, data),
  resetPassword: (id: number, data: { passwordNueva: string }) =>
    api.put<void>(`${BASE}/${id}/reset-password`, data),
  remove: (id: number) => api.delete<void>(`${BASE}/${id}`),
}
