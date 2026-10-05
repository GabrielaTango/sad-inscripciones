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
  deleteImagen: (id: number) => api.delete<void>(`${BASE}/admin/${id}/imagen`),
  uploadImagen: (id: number, file: File) => uploadImagen(id, file),
}

/** Uploads the product image as multipart (api.ts only sends JSON). */
async function uploadImagen(id: number, file: File): Promise<void> {
  const token = localStorage.getItem('sad_token')
  const fd = new FormData()
  fd.append('file', file)

  const res = await fetch(`/api${BASE}/admin/${id}/imagen`, {
    method: 'PUT',
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    body: fd,
  })
  if (!res.ok) {
    const body = await res.json().catch(() => ({ message: 'Error subiendo imagen' }))
    throw new Error(body.message || body.error || `Error ${res.status}`)
  }
}
