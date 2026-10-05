export const CHART_COLORS = [
  'var(--chart-1)',
  'var(--chart-2)',
  'var(--chart-3)',
  'var(--chart-4)',
  'var(--chart-5)',
]

export function colorForStatus(status: string, order: readonly string[]): string {
  const idx = order.indexOf(status)
  return CHART_COLORS[(idx >= 0 ? idx : 0) % CHART_COLORS.length]
}
