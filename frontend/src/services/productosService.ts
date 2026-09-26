import { api } from './api'
import type { Producto, ProductoPublico, ProductoForm } from '../types/models'

const BASE = '/productos'

export const productosService = {
  getAll: () => api.get<ProductoPublico[]>(BASE),
  getById: (id: number) => api.get<ProductoPublico>(`${BASE}/${id}`),
  getAllAdmin: () => api.get<Producto[]>(`${BASE}/admin`),
  getByIdAdmin: (id: number) => api.get<Producto>(`${BASE}/admin/${id}`),
  create: (data: ProductoForm) => api.post<Producto>(BASE, data),
  update: (id: number, data: ProductoForm) => api.put<void>(`${BASE}/${id}`, data),
  remove: (id: number) => api.delete<{ deleted: string }>(`${BASE}/${id}`),
}
