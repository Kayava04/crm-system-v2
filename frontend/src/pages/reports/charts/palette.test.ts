import { describe, expect, it } from 'vitest'
import { CHART_COLORS, colorForStatus } from './palette'

const order = ['Active', 'Paused', 'Completed', 'Cancelled', 'Draft', 'Archived']

describe('colorForStatus', () => {
  it('gives each status the colour at its position in the order', () => {
    expect(colorForStatus('Active', order)).toBe(CHART_COLORS[0])
    expect(colorForStatus('Completed', order)).toBe(CHART_COLORS[2])
  })

  it('wraps around once the palette runs out', () => {
    expect(colorForStatus('Archived', order)).toBe(CHART_COLORS[0])
  })

  it('falls back to the first colour for an unknown status', () => {
    expect(colorForStatus('SomethingNew', order)).toBe(CHART_COLORS[0])
  })
})
