import { useTranslation } from 'react-i18next'
import { PolarAngleAxis, RadialBar, RadialBarChart, ResponsiveContainer } from 'recharts'
import { cn, formatCurrencyParts, toNum } from '@/lib/utils'

/** Income vs. expenses as two concentric rings, each showing its share of the
 * period's turnover, with the net result in the middle. Green/red-family
 * series hues keep the sign readable without the heavier status tones. */
export function BalanceRings({
  income,
  expenses,
  net,
  lang,
}: {
  income: number | string
  expenses: number | string
  net: number | string
  lang: string
}) {
  const { t } = useTranslation()
  const rows = [
    {
      key: 'income',
      name: t('reports.billing.income'),
      value: toNum(income),
      fill: 'var(--chart-3)',
    },
    {
      key: 'expenses',
      name: t('reports.billing.expenses'),
      value: toNum(expenses),
      fill: 'var(--chart-2)',
    },
  ]
  const turnover = Math.max(1, rows[0].value + rows[1].value)
  const netValue = toNum(net)
  const netParts = formatCurrencyParts(netValue, lang)

  return (
    <div className="flex flex-col items-center gap-5">
      <div className="relative size-44">
        <ResponsiveContainer width="100%" height="100%">
          {/* Recharts draws the first row innermost; reversed so income is the outer ring. */}
          <RadialBarChart
            data={[...rows].reverse()}
            innerRadius="62%"
            outerRadius="100%"
            barSize={10}
            barCategoryGap={4}
            startAngle={90}
            endAngle={-270}
          >
            <PolarAngleAxis type="number" domain={[0, turnover]} tick={false} axisLine={false} />
            <RadialBar
              dataKey="value"
              cornerRadius={10}
              background={{ fill: 'var(--foreground)', fillOpacity: 0.06 }}
              animationDuration={800}
              animationEasing="ease-out"
            />
          </RadialBarChart>
        </ResponsiveContainer>
        <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center px-8 text-center">
          <span
            className={cn(
              'text-lg font-semibold tabular-nums',
              netValue >= 0 ? 'text-success' : 'text-destructive',
            )}
          >
            {netParts.value}
          </span>
          <span className="text-xs text-muted-foreground">{t('reports.billing.netResult')}</span>
        </div>
      </div>

      <ul className="flex w-full flex-col gap-2">
        {rows.map((row) => {
          const { value, unit } = formatCurrencyParts(row.value, lang)
          return (
            <li key={row.key} className="flex items-center justify-between gap-3 text-sm">
              <span className="flex items-center gap-2 text-muted-foreground">
                <span
                  className="size-2 rounded-full"
                  style={{ background: row.fill }}
                  aria-hidden
                />
                {row.name}
              </span>
              <span className="font-medium tabular-nums">
                {value}
                {unit && <span className="ml-0.5 font-normal text-muted-foreground">{unit}</span>}
              </span>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
