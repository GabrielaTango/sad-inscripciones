import { useState, useEffect, useCallback } from 'react'
import { Plus, KeyRound, X } from 'lucide-react'
import DataTable from '../../components/Admin/DataTable'
import FormModal from '../../components/Admin/FormModal'
import ConfirmDialog from '../../components/Admin/ConfirmDialog'
import { usuariosService } from '../../services/usuariosService'
import type { Usuario, UsuarioCreateForm, UsuarioUpdateForm } from '../../types/models'

const emptyCreateForm: UsuarioCreateForm = { username: '', password: '', nombreCompleto: '', email: '', activo: true }
const emptyUpdateForm: UsuarioUpdateForm = { username: '', nombreCompleto: '', email: '', activo: true }

const UsuariosAdminPage = () => {
  const [data, setData] = useState<Usuario[]>([])
  const [loading, setLoading] = useState(false)
  const [showForm, setShowForm] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [showPasswordModal, setShowPasswordModal] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [deleteId, setDeleteId] = useState<number | null>(null)
  const [passwordUserId, setPasswordUserId] = useState<number | null>(null)
  const [createForm, setCreateForm] = useState<UsuarioCreateForm>(emptyCreateForm)
  const [updateForm, setUpdateForm] = useState<UsuarioUpdateForm>(emptyUpdateForm)
  const [passwordNueva, setPasswordNueva] = useState('')
  const [error, setError] = useState('')
  const [passwordError, setPasswordError] = useState('')

  const load = useCallback(async () => {
    setData(await usuariosService.getAll())
  }, [])

  useEffect(() => { load() }, [load])

  const openCreate = () => {
    setCreateForm(emptyCreateForm)
    setEditId(null)
    setError('')
    setShowForm(true)
  }

  const openEdit = (item: Usuario) => {
    setUpdateForm({
      username: item.username,
      nombreCompleto: item.nombreCompleto,
      email: item.email || '',
      activo: item.activo,
    })
    setEditId(item.id)
    setError('')
    setShowForm(true)
  }

  const openDelete = (item: Usuario) => {
    setDeleteId(item.id)
    setShowConfirm(true)
  }

  const openChangePassword = (item: Usuario) => {
    setPasswordUserId(item.id)
    setPasswordNueva('')
    setPasswordError('')
    setShowPasswordModal(true)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError('')
    try {
      if (editId) {
        await usuariosService.update(editId, updateForm)
      } else {
        await usuariosService.create(createForm)
      }
      setShowForm(false)
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error')
    } finally {
      setLoading(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteId) return
    setLoading(true)
    try {
      await usuariosService.remove(deleteId)
      setShowConfirm(false)
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error')
    } finally {
      setLoading(false)
    }
  }

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!passwordUserId) return
    setLoading(true)
    setPasswordError('')
    try {
      await usuariosService.resetPassword(passwordUserId, { passwordNueva })
      setShowPasswordModal(false)
    } catch (err) {
      setPasswordError(err instanceof Error ? err.message : 'Error')
    } finally {
      setLoading(false)
    }
  }

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'username', label: 'Usuario' },
    { key: 'nombreCompleto', label: 'Nombre Completo' },
    { key: 'email', label: 'Email' },
    {
      key: 'activo',
      label: 'Activo',
      render: (item: Usuario) =>
        item.activo
          ? <span className="badge bg-green-100 text-green-700">Si</span>
          : <span className="badge bg-gray-100 text-gray-700">No</span>,
    },
  ]

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="font-bold text-slate-800">Usuarios</h2>
        <button className="btn-primary" onClick={openCreate}>
          <Plus className="w-4 h-4 mr-1 inline" />Nuevo
        </button>
      </div>

      <DataTable
        data={data as unknown as Record<string, unknown>[]}
        columns={columns as never}
        onEdit={openEdit as never}
        onDelete={openDelete as never}
        actions={(item: unknown) => (
          <button
            className="inline-flex items-center justify-center p-1.5 border border-amber-500 bg-white text-amber-500 rounded-lg text-sm hover:bg-amber-500 hover:text-white transition-colors"
            onClick={() => openChangePassword(item as Usuario)}
            title="Cambiar contrasena"
          >
            <KeyRound className="w-4 h-4" />
          </button>
        )}
      />

      {/* Create / Edit Modal */}
      <FormModal
        show={showForm}
        title={editId ? 'Editar Usuario' : 'Nuevo Usuario'}
        onClose={() => setShowForm(false)}
        onSubmit={handleSubmit}
        loading={loading}
      >
        {error && <div className="alert-danger">{error}</div>}
        <div className="mb-3">
          <label className="form-label">Usuario *</label>
          <input
            type="text"
            className="form-input"
            value={editId ? updateForm.username : createForm.username}
            onChange={(e) =>
              editId
                ? setUpdateForm({ ...updateForm, username: e.target.value })
                : setCreateForm({ ...createForm, username: e.target.value })
            }
            required
          />
        </div>
        {!editId && (
          <div className="mb-3">
            <label className="form-label">Contrasena *</label>
            <input
              type="password"
              className="form-input"
              value={createForm.password}
              onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })}
              required
              minLength={4}
            />
          </div>
        )}
        <div className="mb-3">
          <label className="form-label">Nombre Completo *</label>
          <input
            type="text"
            className="form-input"
            value={editId ? updateForm.nombreCompleto : createForm.nombreCompleto}
            onChange={(e) =>
              editId
                ? setUpdateForm({ ...updateForm, nombreCompleto: e.target.value })
                : setCreateForm({ ...createForm, nombreCompleto: e.target.value })
            }
            required
          />
        </div>
        <div className="mb-3">
          <label className="form-label">Email</label>
          <input
            type="email"
            className="form-input"
            value={editId ? updateForm.email || '' : createForm.email || ''}
            onChange={(e) =>
              editId
                ? setUpdateForm({ ...updateForm, email: e.target.value })
                : setCreateForm({ ...createForm, email: e.target.value })
            }
          />
        </div>
        <div className="form-check">
          <input
            type="checkbox"
            className="form-check-input"
            id="activo"
            checked={editId ? updateForm.activo : createForm.activo}
            onChange={(e) =>
              editId
                ? setUpdateForm({ ...updateForm, activo: e.target.checked })
                : setCreateForm({ ...createForm, activo: e.target.checked })
            }
          />
          <label className="text-sm" htmlFor="activo">Activo</label>
        </div>
      </FormModal>

      {/* Delete Confirm */}
      <ConfirmDialog
        show={showConfirm}
        title="Eliminar Usuario"
        message="Esta seguro que desea eliminar este usuario?"
        onConfirm={handleDelete}
        onCancel={() => setShowConfirm(false)}
        loading={loading}
      />

      {/* Reset Password Modal */}
      {showPasswordModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white rounded-lg shadow-xl w-full max-w-md mx-4">
            <form onSubmit={handleResetPassword}>
              <div className="flex items-center justify-between p-4 border-b border-gray-200">
                <h5 className="text-lg font-semibold">Cambiar Contrasena</h5>
                <button type="button" className="text-gray-400 hover:text-gray-600" onClick={() => setShowPasswordModal(false)} disabled={loading}><X className="w-5 h-5" /></button>
              </div>
              <div className="p-4">
                {passwordError && <div className="alert-danger">{passwordError}</div>}
                <div className="mb-3">
                  <label className="form-label">Nueva Contrasena *</label>
                  <input
                    type="password"
                    className="form-input"
                    value={passwordNueva}
                    onChange={(e) => setPasswordNueva(e.target.value)}
                    required
                    minLength={4}
                  />
                </div>
              </div>
              <div className="flex justify-end gap-2 p-4 border-t border-gray-200">
                <button type="button" className="btn-secondary" onClick={() => setShowPasswordModal(false)} disabled={loading}>
                  Cancelar
                </button>
                <button type="submit" className="btn-primary" disabled={loading}>
                  {loading ? 'Guardando...' : 'Cambiar'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}

export default UsuariosAdminPage
