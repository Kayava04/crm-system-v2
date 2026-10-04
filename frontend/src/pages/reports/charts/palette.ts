// Categorical series map onto the --chart-* tokens so the dashboard never
// drifts from the rest of the app's colors.
export const CHART_COLORS = [
  'var(--chart-1)',
  'var(--chart-2)',
  'var(--chart-3)',
  'var(--chart-4)',
  'var(--chart-5)',
]

// A status keeps the same color across renders and filters ("color follows the
// entity, never its rank"), instead of being colored by its position among only
// the statuses that happen to have a nonzero count this time.
export function colorForStatus(status: string, order: readonly string[]): string {
  const idx = order.indexOf(status)
  return CHART_COLORS[(idx >= 0 ? idx : 0) % CHART_COLORS.length]
}
