import { useMemo, useState } from 'react'

function startOfWeek(date: Date): Date {
  const d = new Date(date)
  const day = d.getDay() // 0 = Sunday
  const diff = day === 0 ? -6 : 1 - day // shift to Monday
  d.setDate(d.getDate() + diff)
  d.setHours(0, 0, 0, 0)
  return d
}

export function toIsoDate(d: Date): string {
  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

/** Tracks a Monday-Sunday week window as ISO date strings, with prev/next/today navigation. */
export function useWeekRange() {
  const [anchor, setAnchor] = useState(() => startOfWeek(new Date()))

  const weekEnd = useMemo(() => {
    const d = new Date(anchor)
    d.setDate(d.getDate() + 6)
    return d
  }, [anchor])

  return {
    from: toIsoDate(anchor),
    to: toIsoDate(weekEnd),
    weekStart: anchor,
    weekEnd,
    goPrev: () =>
      setAnchor((prev) => {
        const d = new Date(prev)
        d.setDate(d.getDate() - 7)
        return d
      }),
    goNext: () =>
      setAnchor((prev) => {
        const d = new Date(prev)
        d.setDate(d.getDate() + 7)
        return d
      }),
    goToday: () => setAnchor(startOfWeek(new Date())),
  }
}
