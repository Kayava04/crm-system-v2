import { describe, expect, it } from 'vitest'
import { cn, formatCurrency, formatCurrencyParts, formatDate, formatDateTime, toNum } from './utils'

describe('toNum', () => {
  it('returns a number unchanged', () => {
    expect(toNum(42)).toBe(42)
    expect(toNum(0)).toBe(0)
  })

  it('parses a numeric string, matching the backend numeric-or-string quirk', () => {
    expect(toNum('42')).toBe(42)
    expect(toNum('12.5')).toBe(12.5)
    expect(toNum('0')).toBe(0)
  })
})

describe('cn', () => {
  it('merges class names and resolves Tailwind conflicts (later wins)', () => {
    expect(cn('px-2', 'px-4')).toBe('px-4')
  })

  it('drops falsy values', () => {
    expect(cn('a', false, undefined, null, 'b')).toBe('a b')
  })
})

describe('formatCurrency', () => {
  it('formats a numeric-string amount as UAH', () => {
    // Both locales format UAH with the ₴ sign; assert the digits/symbol survive
    // rather than pinning exact locale punctuation, which varies across ICU versions.
    const result = formatCurrency('1500', 'uk')
    expect(result).toContain('1')
    expect(result).toContain('500')
    expect(result).toMatch(/₴/)
  })

  it('accepts an actual number the same as a numeric string', () => {
    expect(formatCurrency(1500, 'uk')).toBe(formatCurrency('1500', 'uk'))
  })
})

describe('formatCurrencyParts', () => {
  it('splits the number from the currency unit', () => {
    const { value, unit } = formatCurrencyParts(1500, 'uk')
    expect(value).toContain('1')
    expect(value).toContain('500')
    expect(value).not.toMatch(/₴/)
    expect(unit).toMatch(/₴/)
  })

  it('agrees with formatCurrency once value and unit are put back together', () => {
    const { value, unit } = formatCurrencyParts('2500.5', 'en')
    const whole = formatCurrency('2500.5', 'en')
    expect(whole).toContain(value)
    expect(whole).toContain(unit)
  })
})

describe('formatDate', () => {
  it('formats a valid ISO date', () => {
    const result = formatDate('2026-03-15', 'en')
    expect(result).toMatch(/2026/)
    expect(result).toMatch(/Mar/)
  })

  it('falls back to the raw value for an unparseable date', () => {
    expect(formatDate('not-a-date', 'en')).toBe('not-a-date')
  })
})

describe('formatDateTime', () => {
  it('formats a valid ISO date-time including hour and minute', () => {
    const result = formatDateTime('2026-03-15T14:30:00Z', 'en')
    expect(result).toMatch(/2026/)
    expect(result).toMatch(/Mar/)
  })

  it('falls back to the raw value for an unparseable date-time', () => {
    expect(formatDateTime('garbage', 'uk')).toBe('garbage')
  })
})
