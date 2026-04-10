import { X } from 'lucide-react'

interface FormModalProps {
  show: boolean
  title: string
  onClose: () => void
  onSubmit: (e: React.FormEvent) => void
  children: React.ReactNode
  loading?: boolean
  size?: 'sm' | 'lg' | 'xl'
}

const sizeClasses = {
  sm: 'max-w-sm',
  lg: 'max-w-2xl',
  xl: 'max-w-4xl',
}

const FormModal = ({ show, title, onClose, onSubmit, children, loading, size }: FormModalProps) => {
  if (!show) return null

  const widthClass = size ? sizeClasses[size] : 'max-w-lg'

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className={`bg-white rounded-lg shadow-xl w-full ${widthClass} mx-4 max-h-[90vh] flex flex-col`}>
        <form onSubmit={onSubmit} className="flex flex-col h-full">
          <div className="flex items-center justify-between p-4 border-b border-gray-200">
            <h5 className="text-lg font-semibold">{title}</h5>
            <button type="button" onClick={onClose} disabled={loading} className="text-gray-400 hover:text-gray-600">
              <X className="w-5 h-5" />
            </button>
          </div>
          <div className="p-4 overflow-y-auto flex-1">{children}</div>
          <div className="flex justify-end gap-2 p-4 border-t border-gray-200">
            <button type="button" className="btn-secondary" onClick={onClose} disabled={loading}>
              Cancelar
            </button>
            <button type="submit" className="btn-primary" disabled={loading}>
              {loading ? 'Guardando...' : 'Guardar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export default FormModal
