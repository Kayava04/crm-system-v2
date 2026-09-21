import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

/** Merge Tailwind class names, resolving conflicts (later wins). */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/**
 * The backend's OpenAPI document types every numeric response field (int32/double) as
 * `number | string` (it declares e.g. `"type": ["integer", "string"]"` with a numeric
 * pattern on almost every count/price field across the whole schema) even though the API
 * always serialises actual JSON numbers at runtime. Use this to normalise such a field
 * wherever it needs arithmetic or comparison, instead of scattering `as number` casts.
 */
export function toNum(value: number | string): number {
  return typeof value === 'number' ? value : Number(value)
}

/**
 * Formats a money amount using the given i18n language. Defaults to UAH (the school is
 * Ukraine-based) — the Billing slice may need to make the currency configurable if the
 * backend turns out to support more than one.
 */
export function formatCurrency(amount: number | string, lang: string): string {
  return new Intl.NumberFormat(lang === 'en' ? 'en-US' : 'uk-UA', {
    style: 'currency',
    currency: 'UAH',
    maximumFractionDigits: 2,
  }).format(toNum(amount))
}

/**
 * Same as formatCurrency, but with the currency unit split out so callers can
 * render it smaller/muted next to the number (e.g. a dashboard stat tile),
 * instead of one same-sized string. Locale-order-safe: uk-UA puts the unit
 * after the number ("3 200,00 грн"), en-US puts it before ("₴3,200.00").
 */
export function formatCurrencyParts(
  amount: number | string,
  lang: string,
): { value: string; unit: string } {
  const parts = new Intl.NumberFormat(lang === 'en' ? 'en-US' : 'uk-UA', {
    style: 'currency',
    currency: 'UAH',
    maximumFractionDigits: 2,
  }).formatToParts(toNum(amount))
  const unit = parts.find((p) => p.type === 'currency')?.value ?? ''
  const value = parts
    .filter((p) => p.type !== 'currency')
    .map((p) => p.value)
    .join('')
    .trim()
  return { value, unit }
}

/** Formats an ISO date (or date-time) string using the given i18n language. */
export function formatDate(value: string, lang: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat(lang === 'en' ? 'en-US' : 'uk-UA', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(date)
}

/** Formats an ISO date-time string (date + time) using the given i18n language. */
export function formatDateTime(value: string, lang: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat(lang === 'en' ? 'en-US' : 'uk-UA', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}
