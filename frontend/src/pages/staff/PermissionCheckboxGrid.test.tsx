import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import i18n from '@/i18n'
import { PermissionCheckboxGrid } from './PermissionCheckboxGrid'
import type { PermissionOption } from '@/features/staff/api'

const options = [
  { id: 'p1', name: 'CanViewStudents' },
  { id: 'p2', name: 'CanViewTeachers' },
  { id: 'p3', name: 'CanManageCourses' },
] as PermissionOption[]

function renderGrid(selected: string[], opts: PermissionOption[] = options) {
  const onChange = vi.fn()
  render(
    <PermissionCheckboxGrid options={opts} selectedIds={new Set(selected)} onChange={onChange} />,
  )
  return onChange
}

const checkbox = (key: string) =>
  screen.getByRole('checkbox', { name: i18n.t(`permissions.${key}`) })

describe('PermissionCheckboxGrid', () => {
  it('renders one checkbox per known permission plus select all', () => {
    renderGrid([])
    expect(screen.getAllByRole('checkbox')).toHaveLength(options.length + 1)
    expect(checkbox('CanViewStudents')).not.toBeChecked()
  })

  it('skips options the frontend does not know about', () => {
    renderGrid([], [...options, { id: 'p9', name: 'CanDoSomethingNew' } as PermissionOption])
    expect(screen.getAllByRole('checkbox')).toHaveLength(options.length + 1)
  })

  it('reflects the selected ids', () => {
    renderGrid(['p2'])
    expect(checkbox('CanViewTeachers')).toBeChecked()
    expect(checkbox('CanViewStudents')).not.toBeChecked()
  })

  it('adds a permission without dropping the existing ones', async () => {
    const user = userEvent.setup()
    const onChange = renderGrid(['p2'])

    await user.click(checkbox('CanViewStudents'))

    expect(onChange).toHaveBeenCalledWith(new Set(['p2', 'p1']))
  })

  it('removes an unchecked permission', async () => {
    const user = userEvent.setup()
    const onChange = renderGrid(['p1', 'p2'])

    await user.click(checkbox('CanViewStudents'))

    expect(onChange).toHaveBeenCalledWith(new Set(['p2']))
  })

  it('selects every available permission at once', async () => {
    const user = userEvent.setup()
    const onChange = renderGrid([])

    await user.click(screen.getByRole('checkbox', { name: i18n.t('permissions.selectAll') }))

    expect(onChange).toHaveBeenCalledWith(new Set(['p1', 'p2', 'p3']))
  })

  it('offers to deselect all once everything is selected', async () => {
    const user = userEvent.setup()
    const onChange = renderGrid(['p1', 'p2', 'p3'])

    await user.click(screen.getByRole('checkbox', { name: i18n.t('permissions.deselectAll') }))

    expect(onChange).toHaveBeenCalledWith(new Set())
  })
})
