import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function toNum(value: number | string): number {
  return typeof value === 'number' ? value : Number(value)
}

export function formatCurrency(amount: number | string, lang: string): string {
  return new Intl.NumberFormat(lang === 'en' ? 'en-US' : 'uk-UA', {
    style: 'currency',
    currency: 'UAH',
    maximumFractionDigits: 2,
  }).format(toNum(amount))
}

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

export function formatDate(value: string, lang: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat(lang === 'en' ? 'en-US' : 'uk-UA', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(date)
}

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
