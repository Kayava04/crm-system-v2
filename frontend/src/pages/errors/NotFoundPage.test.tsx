import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import '@/i18n'
import { NotFoundPage } from './NotFoundPage'

describe('NotFoundPage', () => {
  it('renders the home link through Button asChild', () => {
    render(
      <MemoryRouter>
        <NotFoundPage />
      </MemoryRouter>,
    )
    expect(screen.getByRole('link')).toHaveAttribute('href', '/')
  })
})
