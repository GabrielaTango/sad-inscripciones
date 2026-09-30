import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Receipt, Link as LinkIcon } from 'lucide-react'
import DataTable from '../../components/Admin/DataTable'
import ConfirmDialog from '../../components/Admin/ConfirmDialog'
import { productosService } from '../../services/productosService'
import type { Producto } from '../../types/models'

const ProductosAdminPage = () => {
  const navigate = useNavigate()
  const [data, setData] = useState<Producto[]>([])
  const [loading, setLoading] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [deleteId, setDeleteId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [copiedId, setCopiedId] = useState<number | null>(null)

  const load = useCallback(async () => {
    setData(await productosService.getAllAdmin())
  }, [])

  useEffect(() => { load() }, [load])

  const openEdit = (item: Producto) => { navigate(`/admin/productos/${item.id}`) }
  const openDelete = (item: Producto) => { setDeleteId(item.id); setShowConfirm(true) }
  const verVentas = (item: Producto) => { navigate(`/admin/productos/ventas?productoId=${item.id}`) }

  const copiarLinkCompra = async (item: Producto) => {
    const link = `${window.location.origin}/productos/${item.id}/comprar`
    try {
      await navigator.clipboard.writeText(link)
      setCopiedId(item.id)
      setTimeout(() => setCopiedId(null), 2000)
    } catch {
      setError('No se pudo copiar el link al portapapeles')
    }
  }

  const handleDelete = async () => {
    if (!deleteId) return
    setLoading(true)
    try { await productosService.remove(deleteId); setShowConfirm(false); await load() }
    catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setLoading(false) }
  }

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'nombre', label: 'Nombre' },
    { key: 'precio', label: 'Precio', render: (item: Producto) => `$${item.precio.toFixed(2)}` },
    { key: 'camposExtra', label: 'Campos Extra', render: (item: Producto) => item.camposExtra.length },
    { key: 'activo', label: 'Activo', render: (item: Producto) => item.activo ? <span className="badge bg-green-100 text-green-700">Si</span> : <span className="badge bg-gray-100 text-gray-700">No</span> },
  ]

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="font-bold text-slate-800">Productos</h2>
        <button className="btn-primary" onClick={() => navigate('/admin/productos/nuevo')}><Plus className="inline mr-1" size={16} />Nuevo</button>
      </div>

      {error && <div className="alert-danger">{error}</div>}

      <DataTable
        data={data as unknown as Record<string, unknown>[]}
        columns={columns as never}
        onEdit={openEdit as never}
        onDelete={openDelete as never}
        actions={(item: unknown) => {
          const producto = item as Producto
          return (
            <>
              <button className="btn-outline-secondary btn-sm p-1.5" onClick={() => verVentas(producto)} title="Ver ventas de este producto">
                <Receipt className="w-3.5 h-3.5" />
              </button>
              <button className="btn-outline-primary btn-sm p-1.5" onClick={() => copiarLinkCompra(producto)} title="Copiar link de compra">
                <LinkIcon className="w-3.5 h-3.5" />
              </button>
              {copiedId === producto.id && <span className="text-xs text-green-600">Copiado!</span>}
            </>
          )
        }}
      />

      <ConfirmDialog show={showConfirm} title="Eliminar Producto" message="Esta seguro que desea eliminar este producto?" onConfirm={handleDelete} onCancel={() => setShowConfirm(false)} loading={loading} />
    </div>
  )
}

export default ProductosAdminPage
