import { useState, useEffect, useCallback } from 'react'
import { useParams, Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import DataTable from '../../components/Admin/DataTable'
import { promocionCuponesService } from '../../services/promocionCuponesService'
import { promocionesService } from '../../services/promocionesService'
import type { PromocionCupon, Promocion } from '../../types/models'

const PromocionCuponesPage = () => {
  const { promocionId } = useParams<{ promocionId: string }>()
  const id = Number(promocionId)
  const [cupones, setCupones] = useState<PromocionCupon[]>([])
  const [promocion, setPromocion] = useState<Promocion | null>(null)

  const load = useCallback(async () => {
    const [cups, promo] = await Promise.all([promocionCuponesService.getByPromocionId(id), promocionesService.getById(id)])
    setCupones(cups); setPromocion(promo)
  }, [id])

  useEffect(() => { load() }, [load])

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'documento', label: 'Documento' },
    { key: 'codigo', label: 'Codigo' },
    { key: 'tipoDescuento', label: 'Tipo' },
    { key: 'valor', label: 'Valor' },
    { key: 'usado', label: 'Estado', render: (i: PromocionCupon) => i.usado ? <span className="badge bg-red-100 text-red-700">Usado</span> : <span className="badge bg-green-100 text-green-700">Disponible</span> },
    { key: 'fechaUso', label: 'Fecha Uso', render: (i: PromocionCupon) => i.fechaUso ? new Date(i.fechaUso).toLocaleDateString('es-AR') : '-' },
    { key: 'fechaVencimiento', label: 'Vencimiento', render: (i: PromocionCupon) => i.fechaVencimiento ? new Date(i.fechaVencimiento).toLocaleDateString('es-AR') : '-' },
    { key: 'inscripcionDestinoId', label: 'Usado en Insc.', render: (i: PromocionCupon) => i.inscripcionDestinoId ?? '-' },
  ]

  return (
    <div>
      <div className="flex items-center gap-3 mb-4">
        <Link to="/admin/promociones" className="btn-outline"><ArrowLeft size={16} /></Link>
        <div>
          <h2 className="font-bold text-slate-800">Cupones Generados</h2>
          {promocion && <span className="text-sm text-slate-500">Promocion: {promocion.nombre} ({promocion.tipoDescuento}: {promocion.valor})</span>}
        </div>
      </div>

      <div className="mb-3">
        <span className="badge bg-green-100 text-green-700 mr-2">Disponibles: {cupones.filter(c => !c.usado).length}</span>
        <span className="badge bg-red-100 text-red-700">Usados: {cupones.filter(c => c.usado).length}</span>
      </div>

      <DataTable data={cupones as unknown as Record<string, unknown>[]} columns={columns as never} />
    </div>
  )
}

export default PromocionCuponesPage
