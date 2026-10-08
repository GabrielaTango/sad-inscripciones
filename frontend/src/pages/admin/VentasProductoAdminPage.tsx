import { useState, useEffect, useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Download, RefreshCw } from 'lucide-react'
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
  const [desde, setDesde] = useState('')
  const [hasta, setHasta] = useState('')
  const [texto, setTexto] = useState('')
  const [estadoFilter, setEstadoFilter] = useState('Todas')
  const [loading, setLoading] = useState(false)
  const [exporting, setExporting] = useState(false)
  const [consultando, setConsultando] = useState(false)
  const [resumenConsulta, setResumenConsulta] = useState('')
  const [error, setError] = useState('')

  useEffect(() => { productosService.getAllAdmin().then(setProductos) }, [])

  const filtros = useMemo(() => ({
    productoId: productoFilter ? Number(productoFilter) : undefined,
    estado: estadoFilter,
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

  const handleConsultar = async () => {
    if (consultando) return
    setConsultando(true)
    setError('')
    setResumenConsulta('')
    try {
      const r = await ventasProductoService.consultarPendientes()
      setResumenConsulta(
        `Consultadas: ${r.consultadas} · Pagadas: ${r.pagadas} · Impagas: ${r.impagas} · Siguen pendientes: ${r.siguenPendientes} · Errores: ${r.errores}`
      )
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al consultar pagos pendientes')
    } finally {
      setConsultando(false)
    }
  }

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

  // Solo las ventas pagadas suman al total; pendientes e impagas no son plata cobrada.
  const totalImporte = data.filter(v => v.estado === 'Pagada').reduce((acc, v) => acc + v.importe, 0)

  const estadoBadgeClass: Record<string, string> = {
    Pagada: 'bg-green-100 text-green-700',
    Pendiente: 'bg-amber-100 text-amber-700',
    Impaga: 'bg-red-100 text-red-700',
  }

  const baseColumns = [
    {
      key: 'estado',
      label: 'Estado',
      render: (v: VentaProductoAdmin) => (
        <span className={`badge ${estadoBadgeClass[v.estado] ?? 'bg-gray-100 text-gray-700'}`}>{v.estado}</span>
      ),
    },
    { key: 'fechaAlta', label: 'Fecha alta', render: (v: VentaProductoAdmin) => new Date(v.fechaAlta).toLocaleString('es-AR') },
    {
      key: 'fechaPago',
      label: 'Fecha pago',
      render: (v: VentaProductoAdmin) => v.fechaPago
        ? new Date(v.fechaPago).toLocaleString('es-AR')
        : v.estado === 'Impaga'
          ? `Consultada: ${new Date(v.updatedAt).toLocaleString('es-AR')}`
          : '-',
    },
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
          <option value="Todas">Todas</option>
          <option value="Pagada">Pagada</option>
          <option value="Pendiente">Pendiente</option>
          <option value="Impaga">Impaga</option>
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
          className="btn-primary flex items-center gap-2 disabled:opacity-60"
          onClick={handleConsultar}
          disabled={consultando}
          title="Consultar en MercadoPago todas las ventas pendientes"
        >
          <RefreshCw className={`w-4 h-4 ${consultando ? 'animate-spin' : ''}`} />
          {consultando ? 'Consultando...' : 'Consultar pagos pendientes'}
        </button>
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

      {resumenConsulta && <div className="mb-3 text-sm text-slate-700">{resumenConsulta}</div>}

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
