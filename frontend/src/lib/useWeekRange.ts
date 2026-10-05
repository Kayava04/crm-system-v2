import { useMemo, useState } from 'react'

function startOfWeek(date: Date): Date {
  const d = new Date(date)
  const day = d.getDay()
  const diff = day === 0 ? -6 : 1 - day
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
