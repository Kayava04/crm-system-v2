import { describe, expect, it } from 'vitest'
import { Users } from 'lucide-react'
import { isNavItemVisible, navSections, type NavItem } from './navConfig'

const base: NavItem = { to: '/x', labelKey: 'nav.x', icon: Users }

const allItems = navSections.flatMap((s) => s.items)

function visiblePaths(permissions: string[], roles: string[]) {
  return allItems.filter((i) => isNavItemVisible(i, permissions, roles)).map((i) => i.to)
}

describe('isNavItemVisible', () => {
  it('shows an item that requires nothing', () => {
    expect(isNavItemVisible(base, [], [])).toBe(true)
  })

  it('requires the permission when only a permission is set', () => {
    const item = { ...base, permission: 'CanViewStudents' as const }
    expect(isNavItemVisible(item, ['CanViewStudents'], [])).toBe(true)
    expect(isNavItemVisible(item, [], ['Admin'])).toBe(false)
  })

  it('requires one of the roles when only roles are set', () => {
    const item = { ...base, roles: ['Teacher' as const, 'Student' as const] }
    expect(isNavItemVisible(item, [], ['Student'])).toBe(true)
    expect(isNavItemVisible(item, ['CanViewStudents'], ['Admin'])).toBe(false)
  })

  it('accepts either the permission or a role when both are set', () => {
    const item = { ...base, permission: 'CanViewSchedule' as const, roles: ['Teacher' as const] }
    expect(isNavItemVisible(item, ['CanViewSchedule'], ['Admin'])).toBe(true)
    expect(isNavItemVisible(item, [], ['Teacher'])).toBe(true)
    expect(isNavItemVisible(item, [], ['Student'])).toBe(false)
  })
})

describe('navSections', () => {
  it('has no duplicate routes', () => {
    const paths = allItems.map((i) => i.to)
    expect(new Set(paths).size).toBe(paths.length)
  })

  it('gives a student only their own pages', () => {
    expect(visiblePaths([], ['Student'])).toEqual(['/calendar', '/enrollments', '/invoices'])
  })

  it('gives a teacher only their own pages', () => {
    expect(visiblePaths([], ['Teacher'])).toEqual(['/calendar', '/my-students', '/payroll'])
  })

  it('hides staff management from an admin without CanManageAdmins', () => {
    const permissions = ['CanViewReports', 'CanViewStudents', 'CanViewPayments']
    const paths = visiblePaths(permissions, ['Admin'])
    expect(paths).toContain('/students')
    expect(paths).toContain('/billing/invoices')
    expect(paths).not.toContain('/staff')
  })
})
