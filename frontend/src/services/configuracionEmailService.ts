import { api } from './api'
import type { ConfiguracionEmail, ConfiguracionEmailForm } from '../types/models'

const BASE = '/admin/configuracion-email'

export const configuracionEmailService = {
  get: () => api.get<ConfiguracionEmail>(BASE),
  update: (data: ConfiguracionEmailForm) => api.put<void>(BASE, data),
  enviarPrueba: (destinatario: string) =>
    api.post<{ message: string }>(`${BASE}/test`, { destinatario }),
}
