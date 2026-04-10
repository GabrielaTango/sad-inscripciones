import { api } from './api'
import type { Provincia } from '../types/models'

export const provinciasService = {
  getAll: () => api.get<Provincia[]>('/provincias'),
}
