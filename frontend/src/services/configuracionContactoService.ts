import { api } from './api'

export interface ConfiguracionContacto {
  emailDestino: string
  activo: boolean
  updatedAt: string
  updatedBy: string | null
}

export interface ConfiguracionContactoForm {
  emailDestino: string
  activo: boolean
}

const BASE = '/admin/configuracion-contacto'

export const configuracionContactoService = {
  get: () => api.get<ConfiguracionContacto>(BASE),
  update: (data: ConfiguracionContactoForm) => api.put<void>(BASE, data),
}
