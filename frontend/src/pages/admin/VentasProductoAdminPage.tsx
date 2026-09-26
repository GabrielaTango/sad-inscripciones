import { useState, useEffect, useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Download } from 'lucide-react'
import DataTable from '../../components/Admin/DataTable'
import { productosService } from '../../services/productosService'
import { ventasProductoService } from '../../services/ventasProductoService'
import type { Producto, VentaProductoAdmin } from '../../types/models'

const VentasProductoAdminPage = () => {
  const [searchParams] = useSearchParams()
  const productoIdInicial = searchParams.get('productoId')

  const [productos, setProductos] = useState<Producto[]>([])
  const [data, setData] = useState<VentaProductoAdmin[]>([])
  const [productoFilter, setProductoFilter] = useState<number | ''>(productoIdInicial ? Number(productoIdInicial) : '')
  const [estadoFilter, setEstadoFilter] = useState<string>('Pagada')
  const [desde, setDesde] = useState('')
  const [hasta, setHasta] = useState('')
  const [texto, setTexto] = useState('')
  const [loading, setLoading] = useState(false)
  const [exporting, setExporting] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => { productosService.getAllAdmin().then(setProductos) }, [])

  const filtros = useMemo(() => ({
    productoId: productoFilter ? Number(productoFilter) : undefined,
    estado: estadoFilter === 'Todas' ? undefined : estadoFilter,
    desde: desde || undefined,
    hasta: hasta || undefined,
    texto: texto || undefined,
  }), [productoFilter, estadoFilter, desde, hasta, texto])

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      setData(await ventasProductoService.listAdmin(filtros))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al cargar ventas')
    } finally {
      setLoading(false)
    }
  }, [filtros])

  // Debounce solo para el filtro de texto libre; el resto dispara la carga de inmediato.
  useEffect(() => {
    const timer = setTimeout(load, texto ? 400 : 0)
    return () => clearTimeout(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [productoFilter, estadoFilter, desde, hasta, texto])

  const handleExport = async () => {
    if (data.length === 0 || exporting) return
    setExporting(true)
    setError('')
    try {
      await ventasProductoService.exportExcel(filtros)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al exportar')
    } finally {
      setExporting(false)
    }
  }

  // Columnas de campos extra: las del producto filtrado (en su orden), o vacio si
  // no hay filtro (se muestra un cell "clave: valor" compacto en su lugar).
  const productoSeleccionado = productos.find(p => p.id === productoFilter)
  const camposExtraColumnas = productoFilter ? (productoSeleccionado?.camposExtra ?? []) : []

  const totalImporte = data.reduce((acc, v) => acc + v.importe, 0)

  const estadoBadge = (estado: string) => {
    const map: Record<string, string> = { Pendiente: 'bg-amber-100 text-amber-700', Pagada: 'bg-green-100 text-green-700', Rechazada: 'bg-red-100 text-red-700' }
    return map[estado] || 'bg-blue-100 text-blue-700'
  }

  const baseColumns = [
    { key: 'fechaPago', label: 'Fecha pago', render: (v: VentaProductoAdmin) => v.fechaPago ? new Date(v.fechaPago).toLocaleString('es-AR') : '-' },
    { key: 'productoNombre', label: 'Producto' },
    { key: 'dni', label: 'DNI' },
    { key: 'apellido', label: 'Apellido y nombre', render: (v: VentaProductoAdmin) => `${v.apellido}, ${v.nombre}` },
    { key: 'email', label: 'Email' },
  ]

  const extraColumns = camposExtraColumnas.length > 0
    ? camposExtraColumnas.map(campo => ({
        key: `extra_${campo.key}`,
        label: campo.label,
        render: (v: VentaProductoAdmin) => v.datosExtra[campo.key] ?? '-',
      }))
    : [{
        key: 'datosExtra',
        label: 'Campos extra',
        render: (v: VentaProductoAdmin) => Object.entries(v.datosExtra).length === 0
          ? '-'
          : Object.entries(v.datosExtra).map(([k, val]) => `${k}: ${val}`).join(', '),
      }]

  const tailColumns = [
    { key: 'importe', label: 'Importe', render: (v: VentaProductoAdmin) => `$${v.importe.toFixed(2)}` },
    { key: 'mpPaymentId', label: 'Nro pago MP', render: (v: VentaProductoAdmin) => v.mpPaymentId ?? '-' },
    { key: 'estado', label: 'Estado', render: (v: VentaProductoAdmin) => <span className={`badge ${estadoBadge(v.estado)}`}>{v.estado}</span> },
    { key: 'mailEnviado', label: 'Mail', render: (v: VentaProductoAdmin) => v.mailEnviado ? <span className="badge bg-green-100 text-green-700">Si</span> : <span className="badge bg-gray-100 text-gray-700">No</span> },
  ]

  const columns = [...baseColumns, ...extraColumns, ...tailColumns]

  return (
    <div>
      <h2 className="font-bold text-slate-800 mb-4">Ventas de Productos</h2>
      {error && <div className="alert-danger">{error}</div>}

      <div className="mb-3 flex items-center gap-2 flex-wrap">
        <select className="form-select w-auto" value={productoFilter} onChange={e => setProductoFilter(e.target.value ? Number(e.target.value) : '')}>
          <option value="">Todos los productos</option>
          {productos.map(p => <option key={p.id} value={p.id}>{p.nombre}</option>)}
        </select>
        <select className="form-select w-auto" value={estadoFilter} onChange={e => setEstadoFilter(e.target.value)}>
          <option value="Pagada">Pagada</option>
          <option value="Pendiente">Pendiente</option>
          <option value="Todas">Todas</option>
        </select>
        <label className="text-sm text-slate-600 flex items-center gap-1">
          Desde
          <input type="date" className="form-input w-auto" value={desde} onChange={e => setDesde(e.target.value)} />
        </label>
        <label className="text-sm text-slate-600 flex items-center gap-1">
          Hasta
          <input type="date" className="form-input w-auto" value={hasta} onChange={e => setHasta(e.target.value)} />
        </label>
        <input
          type="text"
          className="form-input w-auto"
          placeholder="Buscar por DNI, nombre, apellido o email..."
          value={texto}
          onChange={e => setTexto(e.target.value)}
        />
        <button
          type="button"
          className="btn-accent flex items-center gap-2 disabled:opacity-60"
          onClick={handleExport}
          disabled={data.length === 0 || exporting}
          title="Descargar ventas filtradas a Excel"
        >
          <Download className={`w-4 h-4 ${exporting ? 'animate-pulse' : ''}`} />
          {exporting ? 'Exportando...' : `Exportar a Excel (${data.length})`}
        </button>
      </div>

      <div className="mb-3 text-sm text-slate-600">
        {loading ? 'Cargando...' : `${data.length} venta${data.length === 1 ? '' : 's'} — Total: $${totalImporte.toFixed(2)}`}
      </div>

      <DataTable
        data={data as unknown as Record<string, unknown>[]}
        columns={columns as never}
      />
    </div>
  )
}

export default VentasProductoAdminPage
