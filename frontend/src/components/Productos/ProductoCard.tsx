import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Package, ShoppingCart } from 'lucide-react'
import type { ProductoPublico } from '../../types/models'

interface Props {
  producto: ProductoPublico
}

const ProductoCard = ({ producto }: Props) => {
  const [imgError, setImgError] = useState(false)

  return (
    <div className="bg-white rounded-2xl shadow-sm border border-slate-200 hover:shadow-xl transition-all duration-300 h-full overflow-hidden flex flex-col">
      <div className="h-48 bg-slate-100 flex items-center justify-center">
        {producto.imagenUrl && !imgError ? (
          <img
            src={producto.imagenUrl}
            alt={producto.nombre}
            className="w-full h-full object-cover"
            onError={() => setImgError(true)}
          />
        ) : (
          <Package className="text-slate-300" size={48} />
        )}
      </div>
      <div className="p-6 flex flex-col flex-grow">
        <h5 className="font-bold text-lg text-slate-800 mb-1">{producto.nombre}</h5>
        {producto.descripcion && (
          <p className="text-slate-600 text-sm line-clamp-3 mb-3">{producto.descripcion}</p>
        )}
        <div className="flex-grow" />
        <p className="text-xl font-bold text-slate-800 mb-3">
          {producto.precio.toLocaleString('es-AR', { style: 'currency', currency: 'ARS' })}
        </p>
        <Link to={`/productos/${producto.id}/comprar`} className="btn-primary btn-sm self-start inline-flex items-center">
          <ShoppingCart className="mr-1" size={14} />Comprar
        </Link>
      </div>
    </div>
  )
}

export default ProductoCard
