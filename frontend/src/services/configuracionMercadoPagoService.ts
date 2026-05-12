import { api } from './api'
import type { ConfiguracionMercadoPago, ConfiguracionMercadoPagoForm } from '../types/models'

const BASE = '/admin/configuracion-mercadopago'

export const configuracionMercadoPagoService = {
  get: () => api.get<ConfiguracionMercadoPago>(BASE),
  update: (data: ConfiguracionMercadoPagoForm) => api.put<void>(BASE, data),
}
