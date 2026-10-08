import { api } from './api'
import type { VentaProductoAdmin, VentaProductoAdminFiltros, VentaProductoConsultaResultado, VentaProductoCreateForm, VentaProductoCreateResult, VentaProductoEstado } from '../types/models'

const BASE = '/ventas-producto'

const buildParams = (filtros: VentaProductoAdminFiltros) => {
  const params = new URLSearchParams()
  if (filtros.productoId) params.append('productoId', String(filtros.productoId))
  if (filtros.estado) params.append('estado', filtros.estado)
  if (filtros.desde) params.append('desde', filtros.desde)
  if (filtros.hasta) params.append('hasta', filtros.hasta)
  if (filtros.texto) params.append('texto', filtros.texto)
  return params
}

export const ventasProductoService = {
  crear: (data: VentaProductoCreateForm) => api.post<VentaProductoCreateResult>(BASE, data),
  verificar: (publicRef: string) => api.post<VentaProductoEstado>(`${BASE}/${publicRef}/verificar`, {}),
  listAdmin: (filtros: VentaProductoAdminFiltros) => {
    const qs = buildParams(filtros).toString()
    return api.get<VentaProductoAdmin[]>(`${BASE}${qs ? `?${qs}` : ''}`)
  },
  consultarPendientes: () => api.post<VentaProductoConsultaResultado>(`${BASE}/consultar-pendientes`, {}),
  exportExcel: async (filtros: VentaProductoAdminFiltros) => {
    const token = localStorage.getItem('sad_token')
    const qs = buildParams(filtros).toString()
    const response = await fetch(`/api${BASE}/export${qs ? `?${qs}` : ''}`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    })
    if (!response.ok) throw new Error('Error al exportar')
    const blob = await response.blob()
    const cd = response.headers.get('content-disposition') || ''
    const match = cd.match(/filename="?([^";]+)"?/i)
    const fechaArchivo = new Date().toISOString().slice(0, 10)
    const filename = match?.[1] ?? `ventas-producto_${fechaArchivo}.xlsx`
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    window.URL.revokeObjectURL(url)
  },
}
