import { describe, expect, it, afterEach, vi } from 'vitest'
import { act, renderHook } from '@testing-library/react'
import { useWeekRange } from './useWeekRange'

describe('useWeekRange', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('anchors the initial window to the Monday-Sunday week containing today', () => {
    // Wednesday, 2026-03-18
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-03-18T12:00:00Z'))

    const { result } = renderHook(() => useWeekRange())

    expect(result.current.from).toBe('2026-03-16') // Monday
    expect(result.current.to).toBe('2026-03-22') // Sunday
  })

  it('treats Sunday as the last day of its own week, not the start of the next', () => {
    // Sunday, 2026-03-22
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-03-22T12:00:00Z'))

    const { result } = renderHook(() => useWeekRange())

    expect(result.current.from).toBe('2026-03-16')
    expect(result.current.to).toBe('2026-03-22')
  })

  it('goNext and goPrev shift the window by exactly 7 days', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-03-18T12:00:00Z'))

    const { result } = renderHook(() => useWeekRange())

    act(() => result.current.goNext())
    expect(result.current.from).toBe('2026-03-23')
    expect(result.current.to).toBe('2026-03-29')

    act(() => result.current.goPrev())
    act(() => result.current.goPrev())
    expect(result.current.from).toBe('2026-03-09')
    expect(result.current.to).toBe('2026-03-15')
  })

  it('goToday resets the window back to the current week', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-03-18T12:00:00Z'))

    const { result } = renderHook(() => useWeekRange())
    act(() => result.current.goNext())
    act(() => result.current.goNext())
    act(() => result.current.goToday())

    expect(result.current.from).toBe('2026-03-16')
    expect(result.current.to).toBe('2026-03-22')
  })
})
