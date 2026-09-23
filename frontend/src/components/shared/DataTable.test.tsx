import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { DataTable, type DataTableColumn } from './DataTable'

interface Row {
  id: string
  name: string
}

const columns: DataTableColumn<Row>[] = [{ key: 'name', header: 'Name', cell: (row) => row.name }]

describe('DataTable', () => {
  it('renders one row per item using rowKey/cell', () => {
    const rows: Row[] = [
      { id: '1', name: 'Alpha' },
      { id: '2', name: 'Beta' },
    ]
    render(<DataTable columns={columns} rows={rows} rowKey={(r) => r.id} />)

    expect(screen.getByText('Alpha')).toBeInTheDocument()
    expect(screen.getByText('Beta')).toBeInTheDocument()
  })

  it('shows the empty message when there are no rows and not loading', () => {
    render(
      <DataTable columns={columns} rows={[]} rowKey={(r) => r.id} emptyMessage="Nothing here" />,
    )
    expect(screen.getByText('Nothing here')).toBeInTheDocument()
  })

  it('shows skeleton placeholder rows instead of data or empty state while loading', () => {
    const rows: Row[] = [{ id: '1', name: 'Alpha' }]
    const { container } = render(
      <DataTable columns={columns} rows={rows} rowKey={(r) => r.id} isLoading skeletonRows={3} />,
    )
    expect(screen.queryByText('Alpha')).not.toBeInTheDocument()
    expect(container.querySelectorAll('tbody tr')).toHaveLength(3)
  })

  it('invokes onRowClick with the clicked row and only that row', async () => {
    const user = userEvent.setup()
    const onRowClick = vi.fn()
    const rows: Row[] = [
      { id: '1', name: 'Alpha' },
      { id: '2', name: 'Beta' },
    ]
    render(<DataTable columns={columns} rows={rows} rowKey={(r) => r.id} onRowClick={onRowClick} />)

    await user.click(screen.getByText('Beta'))

    expect(onRowClick).toHaveBeenCalledTimes(1)
    expect(onRowClick).toHaveBeenCalledWith(rows[1])
  })

  it('does not attach a click handler to rows when onRowClick is not provided', async () => {
    const user = userEvent.setup()
    const rows: Row[] = [{ id: '1', name: 'Alpha' }]
    render(<DataTable columns={columns} rows={rows} rowKey={(r) => r.id} />)

    // Should not throw, and the row should not carry the interactive cursor class.
    await user.click(screen.getByText('Alpha'))
    const row = screen.getByText('Alpha').closest('tr')
    expect(row).not.toHaveClass('cursor-pointer')
  })
})
