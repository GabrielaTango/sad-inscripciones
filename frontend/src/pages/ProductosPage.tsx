import { useState, useEffect } from 'react'
import { Package } from 'lucide-react'
import { productosService } from '../services/productosService'
import ProductoCard from '../components/Productos/ProductoCard'
import type { ProductoPublico } from '../types/models'

const ProductosPage = () => {
  const [productos, setProductos] = useState<ProductoPublico[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    const load = async () => {
      try {
        setProductos(await productosService.getAll())
      } catch {
        setError('No se pudieron cargar los productos.')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  return (
    <>
      <section className="page-header">
        <div className="max-w-7xl mx-auto px-4">
          <h1 className="font-bold text-3xl">Productos</h1>
          <p className="text-lg text-white/90 mb-0">
            Publicaciones y materiales de la Sociedad Argentina de Diabetes
          </p>
        </div>
      </section>

      <section className="py-16 md:py-24">
        <div className="max-w-7xl mx-auto px-4">
          {loading ? (
            <div className="text-center py-16">
              <div className="animate-spin w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full mx-auto"></div>
            </div>
          ) : error ? (
            <div className="alert-danger">{error}</div>
          ) : productos.length === 0 ? (
            <div className="text-center py-16 text-slate-500">
              <Package className="mx-auto" size={48} />
              <p className="mt-3">No hay productos disponibles por el momento.</p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {productos.map(p => <ProductoCard key={p.id} producto={p} />)}
            </div>
          )}
        </div>
      </section>
    </>
  )
}

export default ProductosPage
