import { api } from './api'
import type { SocioData } from '../types/models'

export const authService = {
  getSocioData: () => api.get<SocioData>('/auth/socio-data'),
  getSocioDataByCuit: (cuit: string) => api.get<SocioData>(`/auth/socio-data/${encodeURIComponent(cuit)}`),
}
