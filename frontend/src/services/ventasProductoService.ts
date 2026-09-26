import { api } from './api'
import type { VentaProductoCreateForm, VentaProductoCreateResult } from '../types/models'

const BASE = '/ventas-producto'

export const ventasProductoService = {
  crear: (data: VentaProductoCreateForm) => api.post<VentaProductoCreateResult>(BASE, data),
}
