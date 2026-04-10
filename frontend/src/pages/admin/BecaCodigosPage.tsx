import { useState, useEffect, useCallback } from 'react'
import { useParams, Link } from 'react-router-dom'
import { ArrowLeft, FileSpreadsheet } from 'lucide-react'
import DataTable from '../../components/Admin/DataTable'
import { becaCodigosService } from '../../services/becaCodigosService'
import { becaEventosService } from '../../services/becaEventosService'
import type { BecaCodigo, BecaEvento } from '../../types/models'

const BecaCodigosPage = () => {
  const { becaEventoId } = useParams<{ becaEventoId: string }>()
  const id = Number(becaEventoId)
  const [codigos, setCodigos] = useState<BecaCodigo[]>([])
  const [campana, setCampana] = useState<BecaEvento | null>(null)
  const [exporting, setExporting] = useState(false)

  const load = useCallback(async () => {
    const [codes, camp] = await Promise.all([becaCodigosService.getByBecaEventoId(id), becaEventosService.getById(id)])
    setCodigos(codes); setCampana(camp)
  }, [id])

  useEffect(() => { load() }, [load])

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'codigo', label: 'Codigo' },
    { key: 'usado', label: 'Usado', render: (i: BecaCodigo) => i.usado ? <span className="badge bg-red-100 text-red-700">Usado</span> : <span className="badge bg-green-100 text-green-700">Disponible</span> },
    { key: 'fechaUso', label: 'Fecha Uso', render: (i: BecaCodigo) => i.fechaUso ? new Date(i.fechaUso).toLocaleDateString('es-AR') : '-' },
    { key: 'inscripcionId', label: 'Inscripcion ID', render: (i: BecaCodigo) => i.inscripcionId ?? '-' },
  ]

  return (
    <div>
      <div className="flex items-center gap-3 mb-4">
        <Link to="/admin/becas" className="btn-outline"><ArrowLeft size={16} /></Link>
        <div>
          <h2 className="font-bold text-slate-800">Codigos de Beca</h2>
          {campana && <span className="text-sm text-slate-500">Campana: {campana.nombreCampana} ({campana.tipoDescuento}: {campana.valor})</span>}
        </div>
      </div>

      <div className="flex items-center gap-3 mb-3">
        <span className="badge bg-green-100 text-green-700 mr-2">Disponibles: {codigos.filter(c => !c.usado).length}</span>
        <span className="badge bg-red-100 text-red-700 mr-3">Usados: {codigos.filter(c => c.usado).length}</span>
        <button
          className="btn-outline-success btn-sm p-1.5"
          disabled={exporting || codigos.length === 0}
          onClick={async () => {
            setExporting(true)
            try { await becaCodigosService.exportExcel(id) } catch { alert('Error al exportar') }
            finally { setExporting(false) }
          }}
        >
          <FileSpreadsheet className="inline mr-1" size={16} />
          {exporting ? 'Exportando...' : 'Exportar Excel'}
        </button>
      </div>

      <DataTable data={codigos as unknown as Record<string, unknown>[]} columns={columns as never} />
    </div>
  )
}

export default BecaCodigosPage
