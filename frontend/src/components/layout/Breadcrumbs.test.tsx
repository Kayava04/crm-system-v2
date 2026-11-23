import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import i18n from '@/i18n'
import { Breadcrumbs } from './Breadcrumbs'

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Breadcrumbs />
    </MemoryRouter>,
  )
}

describe('Breadcrumbs', () => {
  it('renders nothing on the home page', () => {
    const { container } = renderAt('/')
    expect(container).toBeEmptyDOMElement()
  })

  it('labels a top-level page with its sidebar name', () => {
    renderAt('/students')
    expect(screen.getByText(i18n.t('nav.students'))).toHaveAttribute('aria-current', 'page')
  })

  it('uses the section name for nested pages', () => {
    renderAt('/students/123')
    expect(screen.getByText(i18n.t('nav.students'))).toBeInTheDocument()
    expect(screen.queryByText('123')).not.toBeInTheDocument()
  })

  it('knows pages that are not in the sidebar', () => {
    renderAt('/profile')
    expect(screen.getByText(i18n.t('nav.profile'))).toBeInTheDocument()
  })

  it('falls back to the raw segment for an unknown page', () => {
    renderAt('/whatever')
    expect(screen.getByText('whatever')).toBeInTheDocument()
  })

  it('links back home', () => {
    renderAt('/students')
    expect(screen.getByRole('link', { name: i18n.t('common.home') })).toHaveAttribute('href', '/')
  })
})
