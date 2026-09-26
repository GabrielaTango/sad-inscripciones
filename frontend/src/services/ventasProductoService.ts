import { api } from './api'
import type { VentaProductoCreateForm, VentaProductoCreateResult, VentaProductoEstado } from '../types/models'

const BASE = '/ventas-producto'

export const ventasProductoService = {
  crear: (data: VentaProductoCreateForm) => api.post<VentaProductoCreateResult>(BASE, data),
  verificar: (publicRef: string) => api.post<VentaProductoEstado>(`${BASE}/${publicRef}/verificar`, {}),
}
