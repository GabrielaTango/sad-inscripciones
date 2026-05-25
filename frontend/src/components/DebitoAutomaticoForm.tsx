import { useEffect, useState } from 'react'
import { CreditCard, X, AlertTriangle, ShieldCheck } from 'lucide-react'

export type MarcaTarjeta = 'Visa' | 'Master'

export type EstadoSyncDebito = 'Sincronizado' | 'PendienteAlta' | 'PendienteBaja' | 'PendienteModificacion'

export interface DebitoAutomaticoInfo {
  activo: boolean
  marcaTarjeta?: string | null
  tarjetaUltimos4?: string | null
  vencimiento?: string | null
  fechaAlta?: string | null
  estadoSync?: EstadoSyncDebito
  bloqueado?: boolean
}

export interface DebitoAutomaticoFormData {
  marcaTarjeta: MarcaTarjeta
  numeroTarjeta: string
  vencimiento: string
}

interface Props {
  open: boolean
  onClose: () => void
  onSubmit: (data: DebitoAutomaticoFormData) => Promise<void>
  submitting?: boolean
  currentInfo?: DebitoAutomaticoInfo | null
  titulo?: string
}

const luhn = (n: string) => {
  let sum = 0
  let alt = false
  for (let i = n.length - 1; i >= 0; i--) {
    let d = Number(n[i])
    if (alt) {
      d *= 2
      if (d > 9) d -= 9
    }
    sum += d
    alt = !alt
  }
  return sum % 10 === 0
}

const formatNumero = (raw: string) => {
  const soloDigitos = raw.replace(/\D/g, '').slice(0, 16)
  return soloDigitos.replace(/(.{4})/g, '$1 ').trim()
}

const formatVencimiento = (raw: string) => {
  const soloDigitos = raw.replace(/\D/g, '').slice(0, 4)
  if (soloDigitos.length <= 2) return soloDigitos
  return `${soloDigitos.slice(0, 2)}/${soloDigitos.slice(2)}`
}

const vencimientoEsFuturo = (vto: string) => {
  const m = /^(\d{2})\/(\d{2})$/.exec(vto)
  if (!m) return false
  const mm = Number(m[1])
  const yy = Number(m[2])
  if (mm < 1 || mm > 12) return false
  const year = 2000 + yy
  // último día del mes
  const limite = new Date(year, mm, 0, 23, 59, 59)
  return limite >= new Date(new Date().setHours(0, 0, 0, 0))
}

const DebitoAutomaticoForm = ({
  open,
  onClose,
  onSubmit,
  submitting = false,
  currentInfo,
  titulo = 'Adherir a débito automático',
}: Props) => {
  const [marca, setMarca] = useState<MarcaTarjeta>('Visa')
  const [numero, setNumero] = useState('')
  const [vencimiento, setVencimiento] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    if (open) {
      setMarca((currentInfo?.marcaTarjeta as MarcaTarjeta) || 'Visa')
      setNumero('')
      setVencimiento(currentInfo?.vencimiento ?? '')
      setError('')
    }
  }, [open, currentInfo])

  if (!open) return null

  const numeroLimpio = numero.replace(/\s/g, '')

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    if (numeroLimpio.length !== 16) {
      setError('El número de tarjeta debe tener 16 dígitos.')
      return
    }
    if (!luhn(numeroLimpio)) {
      setError('El número de tarjeta no es válido.')
      return
    }
    if (!/^\d{2}\/\d{2}$/.test(vencimiento)) {
      setError('Vencimiento inválido. Formato MM/YY.')
      return
    }
    if (!vencimientoEsFuturo(vencimiento)) {
      setError('La fecha de vencimiento ya pasó.')
      return
    }

    try {
      await onSubmit({
        marcaTarjeta: marca,
        numeroTarjeta: numeroLimpio,
        vencimiento,
      })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al guardar el débito automático.')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 px-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md flex flex-col max-h-[90vh]">
        <form onSubmit={handleSubmit} className="flex flex-col">
          <div className="flex items-center justify-between p-4 border-b border-slate-200">
            <h5 className="text-lg font-semibold flex items-center gap-2 text-slate-800">
              <CreditCard className="w-5 h-5 text-blue-600" />
              {titulo}
            </h5>
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="text-slate-400 hover:text-slate-600"
              aria-label="Cerrar"
            >
              <X className="w-5 h-5" />
            </button>
          </div>

          <div className="p-4 overflow-y-auto space-y-4">
            {currentInfo?.activo && currentInfo.tarjetaUltimos4 && (
              <div className="rounded-lg bg-slate-50 border border-slate-200 p-3 text-sm text-slate-700 flex items-center gap-2">
                <ShieldCheck className="w-4 h-4 text-emerald-600" />
                <span>
                  Ya tiene débito activo con {currentInfo.marcaTarjeta} ****{currentInfo.tarjetaUltimos4}
                  {currentInfo.vencimiento ? ` (vence ${currentInfo.vencimiento})` : ''}.
                  Si completa el formulario, reemplazará los datos actuales.
                </span>
              </div>
            )}

            <div>
              <label className="form-label">Tipo de tarjeta</label>
              <div className="flex gap-2">
                {([
                  { id: 'Visa', logo: '/logo-visa.png' },
                  { id: 'Master', logo: '/master-logo.png' },
                ] as { id: MarcaTarjeta; logo: string }[]).map(opt => (
                  <button
                    type="button"
                    key={opt.id}
                    onClick={() => setMarca(opt.id)}
                    disabled={submitting}
                    className={`flex-1 px-3 py-2 rounded-lg border transition flex items-center justify-center h-14 ${
                      marca === opt.id
                        ? 'border-blue-600 bg-blue-50 ring-2 ring-blue-200'
                        : 'border-slate-200 hover:border-slate-300'
                    }`}
                    aria-pressed={marca === opt.id}
                    aria-label={opt.id}
                  >
                    <img
                      src={opt.logo}
                      alt={opt.id}
                      className={`max-h-8 max-w-[80%] object-contain transition ${
                        marca === opt.id ? 'opacity-100' : 'opacity-60'
                      }`}
                    />
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="form-label" htmlFor="da-numero">Número de tarjeta</label>
              <input
                id="da-numero"
                type="text"
                inputMode="numeric"
                autoComplete="cc-number"
                className="form-input font-mono tracking-wider"
                value={numero}
                onChange={e => setNumero(formatNumero(e.target.value))}
                placeholder="XXXX XXXX XXXX XXXX"
                disabled={submitting}
              />
            </div>

            <div>
              <label className="form-label" htmlFor="da-vto">Vencimiento (MM/YY)</label>
              <input
                id="da-vto"
                type="text"
                inputMode="numeric"
                autoComplete="cc-exp"
                className="form-input font-mono w-32"
                value={vencimiento}
                onChange={e => setVencimiento(formatVencimiento(e.target.value))}
                placeholder="MM/YY"
                maxLength={5}
                disabled={submitting}
              />
            </div>

            {error && (
              <div className="alert-danger flex items-center gap-2 text-sm">
                <AlertTriangle className="w-4 h-4" />
                {error}
              </div>
            )}

            <p className="text-xs text-slate-500 leading-relaxed">
              Los datos de la tarjeta se almacenan en forma cifrada y se utilizan únicamente para
              gestionar el débito automático en SAD.
            </p>
          </div>

          <div className="flex justify-end gap-2 p-4 border-t border-slate-200">
            <button
              type="button"
              className="btn-secondary"
              onClick={onClose}
              disabled={submitting}
            >
              Cancelar
            </button>
            <button type="submit" className="btn-primary" disabled={submitting}>
              {submitting ? 'Guardando...' : 'Adherir'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export default DebitoAutomaticoForm
