import { describe, expect, it, vi } from 'vitest'
import type { TFunction } from 'i18next'
import { enumLabel } from './enumLabels'

function fakeT(known: Record<string, string>): TFunction {
  const t = vi.fn((key: string, opts?: { defaultValue?: string }) => {
    return key in known ? known[key] : (opts?.defaultValue ?? key)
  })
  return t as unknown as TFunction
}

describe('enumLabel', () => {
  it('returns an em dash for a null or undefined value', () => {
    const t = fakeT({})
    expect(enumLabel(t, 'studentStatus', null)).toBe('—')
    expect(enumLabel(t, 'studentStatus', undefined)).toBe('—')
  })

  it('looks up the translation under enums.<category>.<value>', () => {
    const t = fakeT({ 'enums.studentStatus.Active': 'Активний' })
    expect(enumLabel(t, 'studentStatus', 'Active')).toBe('Активний')
  })

  it('falls back to the raw backend value when no translation exists', () => {
    const t = fakeT({})
    expect(enumLabel(t, 'courseStatus', 'SomeNewStatus')).toBe('SomeNewStatus')
  })
})
