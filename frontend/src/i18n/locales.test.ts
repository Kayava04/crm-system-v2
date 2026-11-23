import { describe, expect, it } from 'vitest'
import uk from './locales/uk.json'
import en from './locales/en.json'
import { navSections } from '@/routes/navConfig'
import { PERMISSIONS, ROLES } from '@/lib/permissions'

type Tree = { [key: string]: string | Tree }

function flatten(tree: Tree, prefix = ''): Record<string, string> {
  return Object.entries(tree).reduce<Record<string, string>>((acc, [key, value]) => {
    const path = prefix ? `${prefix}.${key}` : key
    return typeof value === 'string'
      ? { ...acc, [path]: value }
      : { ...acc, ...flatten(value, path) }
  }, {})
}

const ukKeys = flatten(uk as Tree)
const enKeys = flatten(en as Tree)

function placeholders(text: string) {
  return (text.match(/{{\s*\w+\s*}}/g) ?? []).map((p) => p.replace(/\s/g, '')).sort()
}

describe('locales', () => {
  it('have the same keys in uk and en', () => {
    expect(Object.keys(enKeys).sort()).toEqual(Object.keys(ukKeys).sort())
  })

  it('have no empty translations', () => {
    const empty = [...Object.entries(ukKeys), ...Object.entries(enKeys)]
      .filter(([, value]) => value.trim() === '')
      .map(([key]) => key)
    expect(empty).toEqual([])
  })

  it('use the same interpolation placeholders in both languages', () => {
    const mismatched = Object.keys(ukKeys).filter(
      (key) =>
        key in enKeys && placeholders(ukKeys[key]).join() !== placeholders(enKeys[key]).join(),
    )
    expect(mismatched).toEqual([])
  })

  it('translate every sidebar label', () => {
    const missing = navSections
      .flatMap((s) => [s.titleKey, ...s.items.map((i) => i.labelKey)])
      .filter((key): key is string => !!key)
      .filter((key) => !(key in ukKeys) || !(key in enKeys))
    expect(missing).toEqual([])
  })

  it('translate every permission and role', () => {
    const keys = [...PERMISSIONS.map((p) => `permissions.${p}`), ...ROLES.map((r) => `roles.${r}`)]
    const missing = keys.filter((key) => !(key in ukKeys) || !(key in enKeys))
    expect(missing).toEqual([])
  })
})
