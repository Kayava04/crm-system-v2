import { describe, expect, it, vi, beforeEach, type Mock } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import i18n from '@/i18n'
import { useAuth } from '@/features/auth/useAuth'
import { ApiError } from '@/api/errors'
import { LoginPage } from './LoginPage'

vi.mock('@/features/auth/useAuth', () => ({
  useAuth: vi.fn(),
}))

const login = vi.fn()

function renderLogin(
  status: 'loading' | 'authenticated' | 'unauthenticated' = 'unauthenticated',
  from?: string,
) {
  ;(useAuth as unknown as Mock).mockReturnValue({ status, login })
  return render(
    <MemoryRouter
      initialEntries={[{ pathname: '/login', state: from ? { from: { pathname: from } } : null }]}
    >
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<>{'HOME PAGE'}</>} />
        <Route path="/students" element={<>{'STUDENTS PAGE'}</>} />
      </Routes>
    </MemoryRouter>,
  )
}

async function submit(email: string, password: string) {
  const user = userEvent.setup()
  if (email) await user.type(screen.getByLabelText(i18n.t('auth.emailLabel')), email)
  if (password) await user.type(screen.getByLabelText(i18n.t('auth.passwordLabel')), password)
  await user.click(screen.getByRole('button', { name: i18n.t('auth.loginButton') }))
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('LoginPage', () => {
  it('redirects an already signed-in user home', () => {
    renderLogin('authenticated')
    expect(screen.getByText('HOME PAGE')).toBeInTheDocument()
  })

  it('redirects an already signed-in user back to where they came from', () => {
    renderLogin('authenticated', '/students')
    expect(screen.getByText('STUDENTS PAGE')).toBeInTheDocument()
  })

  it('does not submit an empty form', async () => {
    renderLogin()
    await submit('', '')

    expect(login).not.toHaveBeenCalled()
    expect(screen.getByLabelText(i18n.t('auth.emailLabel'))).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByLabelText(i18n.t('auth.passwordLabel'))).toHaveAttribute(
      'aria-invalid',
      'true',
    )
  })

  it('does not submit a malformed email', async () => {
    renderLogin()
    await submit('not-an-email', 'secret')

    expect(login).not.toHaveBeenCalled()
    expect(screen.getByLabelText(i18n.t('auth.emailLabel'))).toHaveAttribute('aria-invalid', 'true')
  })

  it('signs in and goes home', async () => {
    login.mockResolvedValue({})
    renderLogin()
    await submit('admin@example.com', 'secret')

    expect(login).toHaveBeenCalledWith('admin@example.com', 'secret')
    expect(await screen.findByText('HOME PAGE')).toBeInTheDocument()
  })

  it('signs in and returns to the page that required it', async () => {
    login.mockResolvedValue({})
    renderLogin('unauthenticated', '/students')
    await submit('admin@example.com', 'secret')

    expect(await screen.findByText('STUDENTS PAGE')).toBeInTheDocument()
  })

  it('shows the invalid credentials message on a rejected login', async () => {
    login.mockRejectedValue(new ApiError(401))
    renderLogin()
    await submit('admin@example.com', 'wrong')

    expect(await screen.findByRole('alert')).toHaveTextContent(i18n.t('auth.invalidCredentials'))
  })

  it('shows the server detail when there is one', async () => {
    login.mockRejectedValue(new ApiError(423, { detail: 'Account is locked' }))
    renderLogin()
    await submit('admin@example.com', 'wrong')

    expect(await screen.findByRole('alert')).toHaveTextContent('Account is locked')
  })

  it('tells the user how long to wait when rate limited', async () => {
    login.mockRejectedValue(new ApiError(429, { title: 'Too many requests' }, 30))
    renderLogin()
    await submit('admin@example.com', 'secret')

    expect(await screen.findByRole('alert')).toHaveTextContent(
      i18n.t('auth.tooManyAttempts', { seconds: 30 }),
    )
  })

  it('puts server validation errors under the matching field', async () => {
    login.mockRejectedValue(new ApiError(400, { errors: { Email: ['Email is not allowed'] } }))
    renderLogin()
    await submit('admin@example.com', 'secret')

    expect(await screen.findByText('Email is not allowed')).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('shows the network error when the server cannot be reached', async () => {
    login.mockRejectedValue(new TypeError('Failed to fetch'))
    renderLogin()
    await submit('admin@example.com', 'secret')

    expect(await screen.findByRole('alert')).toHaveTextContent(i18n.t('common.networkError'))
  })
})
