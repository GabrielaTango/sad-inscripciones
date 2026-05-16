import { useEffect, useRef, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Printer, ArrowLeft } from 'lucide-react'
import JsBarcode from 'jsbarcode'
import { useAuth } from '../context/AuthContext'
import { resumenCuentaService, type PagoCuentaCorriente } from '../services/resumenCuentaService'

interface ComprobanteJson {
  tComp?: string
  TComp?: string
  nComp?: string
  NComp?: string
  fechaVto?: string
  FechaVto?: string
  saldo?: number
  Saldo?: number
}

const parseComprobantes = (json: string | null | undefined): ComprobanteJson[] => {
  if (!json) return []
  try {
    const parsed = JSON.parse(json)
    return Array.isArray(parsed) ? parsed : []
  } catch {
    return []
  }
}

const pickStr = (...vals: (string | undefined)[]) => vals.find((v) => v !== undefined) ?? ''
const pickNum = (...vals: (number | undefined)[]) => vals.find((v) => v !== undefined) ?? 0

const formatDate = (d?: string | null) => (d ? new Date(d).toLocaleDateString('es-AR') : '-')
const formatCurrency = (n: number) =>
  `$${n.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`

const CuponPagoFacilPage = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const barcodeRef = useRef<SVGSVGElement>(null)

  const [pago, setPago] = useState<PagoCuentaCorriente | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login')
      return
    }
    if (!id) return
    resumenCuentaService
      .getPago(Number(id))
      .then(setPago)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error al cargar el cupón'))
      .finally(() => setLoading(false))
  }, [id, isAuthenticated, navigate])

  // Renderizar barcode cuando el pago carga.
  useEffect(() => {
    if (!pago?.codigoBarra || !barcodeRef.current) return
    try {
      JsBarcode(barcodeRef.current, pago.codigoBarra, {
        format: 'ITF',
        width: 1.2,
        height: 70,
        displayValue: true,
        fontSize: 12,
        margin: 4,
      })
    } catch (e) {
      console.error('Error generando código de barras', e)
    }
  }, [pago])

  if (loading) {
    return (
      <div className="max-w-3xl mx-auto px-4 py-16 text-center">
        <div className="animate-spin w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full inline-block" />
      </div>
    )
  }

  if (error || !pago) {
    return (
      <div className="max-w-3xl mx-auto px-4 py-16">
        <div className="alert-danger">{error || 'Cupón no encontrado'}</div>
      </div>
    )
  }

  if (pago.medioPago !== 'PagoFacil') {
    return (
      <div className="max-w-3xl mx-auto px-4 py-16">
        <div className="alert-danger">Este pago no es un cupón Pago Fácil.</div>
      </div>
    )
  }

  const comprobantes = parseComprobantes(pago.comprobantes)
  const cliente = pago.razonSoci?.trim() || pago.cuit

  return (
    <div className="bg-slate-50 min-h-screen">
      {/* Barra de acciones (oculta al imprimir) */}
      <div className="print:hidden bg-white border-b border-slate-200 sticky top-0 z-10">
        <div className="max-w-3xl mx-auto px-4 py-3 flex items-center justify-between">
          <button
            onClick={() => navigate('/resumen-cuenta')}
            className="text-slate-600 hover:text-slate-900 flex items-center gap-1 text-sm"
          >
            <ArrowLeft className="w-4 h-4" /> Volver al resumen
          </button>
          <button onClick={() => window.print()} className="btn-primary flex items-center gap-2">
            <Printer className="w-4 h-4" /> Imprimir
          </button>
        </div>
      </div>

      <div className="max-w-3xl mx-auto px-4 py-8 print:py-2">
        <div className="bg-white rounded-lg shadow print:shadow-none border border-slate-200 print:border-0">
          {/* Cabecera */}
          <div className="border-b border-slate-200 px-8 py-6 flex items-center justify-between">
            <div>
              <div className="text-xs uppercase tracking-wider text-slate-500">Sociedad Argentina de Diabetes</div>
              <h1 className="text-2xl font-bold text-slate-800 mt-1">Cupón de pago</h1>
            </div>
            <div className="text-right">
              <div className="text-xs text-slate-500">Cupón Nº</div>
              <div className="text-lg font-bold text-slate-800">#{pago.id}</div>
            </div>
          </div>

          {/* Datos del cliente */}
          <div className="px-8 py-5 border-b border-slate-200 grid grid-cols-2 gap-4">
            <div>
              <div className="text-xs uppercase tracking-wider text-slate-500">Cliente</div>
              <div className="text-base font-semibold text-slate-800 mt-1">{cliente}</div>
            </div>
            <div>
              <div className="text-xs uppercase tracking-wider text-slate-500">CUIT</div>
              <div className="text-base font-semibold text-slate-800 mt-1">{pago.cuit}</div>
            </div>
          </div>

          {/* Comprobantes */}
          <div className="px-8 py-5 border-b border-slate-200">
            <div className="text-xs uppercase tracking-wider text-slate-500 mb-3">Comprobantes incluidos</div>
            {comprobantes.length === 0 ? (
              <div className="text-sm text-slate-500 italic">Pago a cuenta (sin imputación específica).</div>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs uppercase tracking-wider text-slate-500 border-b border-slate-200">
                    <th className="py-2">Tipo</th>
                    <th className="py-2">Comprobante</th>
                    <th className="py-2">Vencimiento</th>
                    <th className="py-2 text-right">Saldo</th>
                  </tr>
                </thead>
                <tbody>
                  {comprobantes.map((c, idx) => (
                    <tr key={idx} className="border-b border-slate-100 last:border-0">
                      <td className="py-2">{pickStr(c.tComp, c.TComp)}</td>
                      <td className="py-2 font-medium">{pickStr(c.nComp, c.NComp)}</td>
                      <td className="py-2">{formatDate(pickStr(c.fechaVto, c.FechaVto))}</td>
                      <td className="py-2 text-right font-semibold">{formatCurrency(pickNum(c.saldo, c.Saldo))}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {/* Total + vencimiento */}
          <div className="px-8 py-5 border-b border-slate-200 grid grid-cols-2 gap-4">
            <div>
              <div className="text-xs uppercase tracking-wider text-slate-500">Vencimiento del cupón</div>
              <div className="text-base font-semibold text-slate-800 mt-1">{formatDate(pago.fechaVencimiento)}</div>
            </div>
            <div className="text-right">
              <div className="text-xs uppercase tracking-wider text-slate-500">Total a pagar</div>
              <div className="text-2xl font-bold text-blue-700 mt-1">{formatCurrency(pago.monto)}</div>
            </div>
          </div>

          {/* Código de barras */}
          <div className="px-8 py-6 text-center">
            <svg
              ref={barcodeRef}
              className="mx-auto block"
              style={{ maxWidth: '100%', height: 'auto' }}
            />
            <p className="text-xs text-slate-500 mt-3">
              Presente este cupón en cualquier sucursal Pago Fácil para abonar.
              <br />
              Una vez registrado el pago, se acreditará en su cuenta dentro de los siguientes días hábiles.
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}

export default CuponPagoFacilPage
