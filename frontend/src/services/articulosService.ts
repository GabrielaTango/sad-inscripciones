import { api } from './api'

export interface Articulo {
  codArticu: string
  descripcion: string
}

export const articulosService = {
  buscar: (q: string) => api.get<Articulo[]>(`/articulos/buscar?q=${encodeURIComponent(q)}`),
}
