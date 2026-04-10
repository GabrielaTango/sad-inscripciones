import { useState, useMemo } from 'react'
import { Pencil, Trash2, ChevronUp, ChevronDown } from 'lucide-react'

interface Column<T> {
  key: string
  label: string
  render?: (item: T) => React.ReactNode
  sortable?: boolean
}

interface DataTableProps<T> {
  data: T[]
  columns: Column<T>[]
  onEdit?: (item: T) => void
  onDelete?: (item: T) => void
  actions?: (item: T) => React.ReactNode
  keyField?: string
  searchPlaceholder?: string
}

function DataTable<T extends Record<string, unknown>>({
  data,
  columns,
  onEdit,
  onDelete,
  actions,
  keyField = 'id',
  searchPlaceholder = 'Buscar...',
}: DataTableProps<T>) {
  const [search, setSearch] = useState('')
  const [sortKey, setSortKey] = useState<string | null>(null)
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc')
  const [page, setPage] = useState(0)
  const pageSize = 10

  const filtered = useMemo(() => {
    if (!search) return data
    const lower = search.toLowerCase()
    return data.filter((item) =>
      columns.some((col) => {
        const val = item[col.key]
        return val != null && String(val).toLowerCase().includes(lower)
      })
    )
  }, [data, search, columns])

  const sorted = useMemo(() => {
    if (!sortKey) return filtered
    return [...filtered].sort((a, b) => {
      const aVal = a[sortKey]
      const bVal = b[sortKey]
      if (aVal == null) return 1
      if (bVal == null) return -1
      const cmp = String(aVal).localeCompare(String(bVal), undefined, { numeric: true })
      return sortDir === 'asc' ? cmp : -cmp
    })
  }, [filtered, sortKey, sortDir])

  const paged = sorted.slice(page * pageSize, (page + 1) * pageSize)
  const totalPages = Math.ceil(sorted.length / pageSize)

  const handleSort = (key: string) => {
    if (sortKey === key) {
      setSortDir(sortDir === 'asc' ? 'desc' : 'asc')
    } else {
      setSortKey(key)
      setSortDir('asc')
    }
  }

  return (
    <div>
      <div className="mb-3">
        <input
          type="text"
          className="form-input"
          placeholder={searchPlaceholder}
          value={search}
          onChange={(e) => {
            setSearch(e.target.value)
            setPage(0)
          }}
        />
      </div>

      <div className="overflow-x-auto rounded-lg border border-gray-200">
        <table className="w-full text-sm">
          <thead className="bg-slate-800 text-white">
            <tr>
              {columns.map((col) => (
                <th
                  key={col.key}
                  className={`px-4 py-3 text-left font-semibold ${col.sortable !== false ? 'cursor-pointer select-none' : ''}`}
                  onClick={() => col.sortable !== false && handleSort(col.key)}
                >
                  <span className="flex items-center gap-1">
                    {col.label}
                    {sortKey === col.key && (
                      sortDir === 'asc' ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />
                    )}
                  </span>
                </th>
              ))}
              {(onEdit || onDelete || actions) && <th className="px-4 py-3 text-left font-semibold">Acciones</th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {paged.length === 0 ? (
              <tr>
                <td colSpan={columns.length + (onEdit || onDelete || actions ? 1 : 0)} className="text-center text-slate-500 py-8">
                  No se encontraron registros
                </td>
              </tr>
            ) : (
              paged.map((item) => (
                <tr key={String(item[keyField])} className="hover:bg-gray-50 even:bg-gray-50/50">
                  {columns.map((col) => (
                    <td key={col.key} className="px-4 py-3">{col.render ? col.render(item) : String(item[col.key] ?? '')}</td>
                  ))}
                  {(onEdit || onDelete || actions) && (
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-1">
                        {onEdit && (
                          <button className="btn-outline-primary btn-sm p-1.5" onClick={() => onEdit(item)} title="Editar">
                            <Pencil className="w-3.5 h-3.5" />
                          </button>
                        )}
                        {onDelete && (
                          <button className="btn-outline-danger btn-sm p-1.5" onClick={() => onDelete(item)} title="Eliminar">
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        )}
                        {actions && actions(item)}
                      </div>
                    </td>
                  )}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex justify-end mt-3 gap-1">
          <button
            className={`px-3 py-1.5 text-sm rounded border ${page === 0 ? 'opacity-50 cursor-not-allowed border-gray-200 text-gray-400' : 'border-gray-300 text-gray-700 hover:bg-gray-50'}`}
            onClick={() => setPage(page - 1)}
            disabled={page === 0}
          >
            Anterior
          </button>
          {Array.from({ length: totalPages }, (_, i) => (
            <button
              key={i}
              className={`px-3 py-1.5 text-sm rounded border ${page === i ? 'bg-blue-600 text-white border-blue-600' : 'border-gray-300 text-gray-700 hover:bg-gray-50'}`}
              onClick={() => setPage(i)}
            >
              {i + 1}
            </button>
          ))}
          <button
            className={`px-3 py-1.5 text-sm rounded border ${page === totalPages - 1 ? 'opacity-50 cursor-not-allowed border-gray-200 text-gray-400' : 'border-gray-300 text-gray-700 hover:bg-gray-50'}`}
            onClick={() => setPage(page + 1)}
            disabled={page === totalPages - 1}
          >
            Siguiente
          </button>
        </div>
      )}
    </div>
  )
}

export default DataTable
