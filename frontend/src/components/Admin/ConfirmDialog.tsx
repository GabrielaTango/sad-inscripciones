import { X } from 'lucide-react'

interface ConfirmDialogProps {
  show: boolean
  title: string
  message: string
  onConfirm: () => void
  onCancel: () => void
  loading?: boolean
}

const ConfirmDialog = ({ show, title, message, onConfirm, onCancel, loading }: ConfirmDialogProps) => {
  if (!show) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md mx-4">
        <div className="flex items-center justify-between p-4 border-b border-gray-200">
          <h5 className="text-lg font-semibold">{title}</h5>
          <button onClick={onCancel} disabled={loading} className="text-gray-400 hover:text-gray-600">
            <X className="w-5 h-5" />
          </button>
        </div>
        <div className="p-4">
          <p className="text-gray-600">{message}</p>
        </div>
        <div className="flex justify-end gap-2 p-4 border-t border-gray-200">
          <button className="btn-secondary" onClick={onCancel} disabled={loading}>
            Cancelar
          </button>
          <button className="btn-danger" onClick={onConfirm} disabled={loading}>
            {loading ? 'Eliminando...' : 'Eliminar'}
          </button>
        </div>
      </div>
    </div>
  )
}

export default ConfirmDialog
