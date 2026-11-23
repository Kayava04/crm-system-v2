import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ErrorBoundary } from './ErrorBoundary'

function Boom(): never {
  throw new Error('render failed')
}

const swallow = (event: ErrorEvent) => event.preventDefault()

beforeEach(() => {
  window.addEventListener('error', swallow)
})

afterEach(() => {
  window.removeEventListener('error', swallow)
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
})

describe('ErrorBoundary', () => {
  it('renders its children when nothing throws', () => {
    render(<ErrorBoundary>{'PAGE CONTENT'}</ErrorBoundary>)
    expect(screen.getByText('PAGE CONTENT')).toBeInTheDocument()
  })

  it('replaces a crashed tree with the fallback screen and logs the error', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    render(
      <ErrorBoundary>
        <Boom />
      </ErrorBoundary>,
    )

    expect(screen.getByRole('heading')).toBeInTheDocument()
    expect(screen.getByRole('button')).toBeInTheDocument()
    expect(consoleError).toHaveBeenCalledWith(
      'Unhandled UI error',
      expect.objectContaining({ message: 'render failed' }),
      expect.anything(),
    )
  })

  it('reloads the page from the fallback button', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    const reload = vi.fn()
    vi.stubGlobal('location', { ...window.location, reload })
    const user = userEvent.setup()
    render(
      <ErrorBoundary>
        <Boom />
      </ErrorBoundary>,
    )

    await user.click(screen.getByRole('button'))

    expect(reload).toHaveBeenCalledTimes(1)
  })
})
