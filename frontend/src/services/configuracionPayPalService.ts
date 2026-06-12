import { api } from './api'
import type { ConfiguracionPayPal, ConfiguracionPayPalForm, ConfiguracionPayPalPublic } from '../types/models'

const BASE = '/admin/configuracion-paypal'

export const configuracionPayPalService = {
  get: () => api.get<ConfiguracionPayPal>(BASE),
  update: (data: ConfiguracionPayPalForm) => api.put<void>(BASE, data),
  getPublic: () => api.get<ConfiguracionPayPalPublic>('/configuracion-paypal/public'),
}
