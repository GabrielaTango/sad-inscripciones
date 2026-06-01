import { useEffect, useRef, useState } from 'react'

// Tipado mínimo del SDK de PayPal (cargado por script, no por npm).
interface PayPalOrderActions {
  order: {
    create: (opts: unknown) => Promise<string>
    capture: () => Promise<unknown>
  }
}
interface PayPalOnClickActions {
  reject: () => Promise<void>
  resolve: () => Promise<void>
}
interface PayPalButtonsConfig {
  style?: Record<string, unknown>
  onClick?: (data: unknown, actions: PayPalOnClickActions) => Promise<void> | void
  createOrder: (data: unknown, actions: PayPalOrderActions) => Promise<string>
  onApprove: (data: { orderID: string }, actions: PayPalOrderActions) => Promise<void>
  onError?: (err: unknown) => void
  onCancel?: () => void
}
interface PayPalNamespace {
  Buttons: (config: PayPalButtonsConfig) => { render: (el: HTMLElement) => Promise<void> }
}
declare global {
  interface Window {
    paypal?: PayPalNamespace
  }
}

// Carga el SDK una sola vez por (clientId, currency). PayPal no soporta recargar con
// distintos parámetros sin recrear el script, así que cacheamos por esa clave.
let sdkState: { key: string; promise: Promise<void> } | null = null
function loadPayPalSdk(clientId: string, currency: string): Promise<void> {
  const key = `${clientId}|${currency}`
  if (sdkState && sdkState.key === key) return sdkState.promise
  const promise = new Promise<void>((resolve, reject) => {
    const existing = document.getElementById('paypal-sdk')
    if (existing) existing.remove()
    const s = document.createElement('script')
    s.id = 'paypal-sdk'
    s.src = `https://www.paypal.com/sdk/js?client-id=${encodeURIComponent(clientId)}&currency=${encodeURIComponent(currency)}&intent=capture`
    s.onload = () => resolve()
    s.onerror = () => reject(new Error('No se pudo cargar PayPal'))
    document.body.appendChild(s)
  })
  sdkState = { key, promise }
  return promise
}

interface PayPalCheckoutProps {
  clientId: string
  currency: string
  amount: number
  /** Crea la inscripción (estado Pendiente) y devuelve su id. Se llama al iniciar el pago. */
  createInscripcion: () => Promise<number>
  /** Llamado tras capturar el pago en PayPal. */
  onSuccess: (orderId: string, inscripcionId: number) => void
  /** Devuelve un mensaje de error si el pago no debe iniciarse (ej: faltan términos). null si OK. */
  validate?: () => string | null
}

const PayPalCheckout = ({ clientId, currency, amount, createInscripcion, onSuccess, validate }: PayPalCheckoutProps) => {
  const containerRef = useRef<HTMLDivElement>(null)
  const [error, setError] = useState('')

  // Refs para que el botón (renderizado una vez) use siempre los valores más recientes.
  const amountRef = useRef(amount)
  const createRef = useRef(createInscripcion)
  const successRef = useRef(onSuccess)
  const validateRef = useRef(validate)
  const inscripcionIdRef = useRef<number | null>(null)
  const renderedRef = useRef(false)

  // Sincroniza las refs después de cada render (las usan los callbacks async de PayPal).
  useEffect(() => {
    amountRef.current = amount
    createRef.current = createInscripcion
    successRef.current = onSuccess
    validateRef.current = validate
  })

  useEffect(() => {
    let cancelled = false
    loadPayPalSdk(clientId, currency)
      .then(() => {
        if (cancelled || !window.paypal || !containerRef.current || renderedRef.current) return
        renderedRef.current = true
        window.paypal
          .Buttons({
            style: { layout: 'vertical', color: 'gold', shape: 'rect', label: 'paypal' },
            onClick: (_data, actions) => {
              const msg = validateRef.current?.()
              if (msg) {
                setError(msg)
                return actions.reject()
              }
              setError('')
              return actions.resolve()
            },
            createOrder: (_data, actions) =>
              createRef.current().then((insId) => {
                inscripcionIdRef.current = insId
                return actions.order.create({
                  purchase_units: [
                    { amount: { value: amountRef.current.toFixed(2), currency_code: currency } },
                  ],
                })
              }),
            onApprove: (data, actions) =>
              actions.order.capture().then(() => {
                if (inscripcionIdRef.current != null) {
                  successRef.current(data.orderID, inscripcionIdRef.current)
                }
              }),
            onError: (err) => {
              console.error('PayPal error', err)
              setError('Hubo un error al procesar el pago con PayPal.')
            },
          })
          .render(containerRef.current)
          .catch(() => {
            /* render abortado (ej: navegación) */
          })
      })
      .catch(() => setError('No se pudo cargar PayPal. Verificá la configuración.'))
    return () => {
      cancelled = true
    }
  }, [clientId, currency])

  return (
    <div>
      {error && <div className="alert-danger">{error}</div>}
      <div ref={containerRef} />
    </div>
  )
}

export default PayPalCheckout
