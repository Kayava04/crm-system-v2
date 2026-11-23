import { describe, expect, it, vi, beforeEach, type Mock } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { toast } from 'sonner'
import i18n from '@/i18n'
import { ApiError } from '@/api/errors'
import { useAuth } from './useAuth'
import { ChangePasswordForm } from './ChangePasswordForm'

vi.mock('./useAuth', () => ({
  useAuth: vi.fn(),
}))

vi.mock('sonner', () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}))

const changePassword = vi.fn()
const logout = vi.fn()

function renderForm(onSuccess = vi.fn()) {
  ;(useAuth as unknown as Mock).mockReturnValue({ changePassword, logout })
  render(<ChangePasswordForm onSuccess={onSuccess} />)
  return onSuccess
}

const field = (key: string) => screen.getByLabelText(i18n.t(key))

async function fill(oldPassword: string, newPassword: string, confirmNewPassword: string) {
  const user = userEvent.setup()
  if (oldPassword) await user.type(field('auth.oldPasswordLabel'), oldPassword)
  if (newPassword) await user.type(field('auth.newPasswordLabel'), newPassword)
  if (confirmNewPassword) await user.type(field('auth.confirmNewPasswordLabel'), confirmNewPassword)
  await user.click(screen.getByRole('button', { name: i18n.t('auth.changePasswordButton') }))
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('ChangePasswordForm', () => {
  it('requires a new password of at least 8 characters', async () => {
    renderForm()
    await fill('old-password', 'short', 'short')

    expect(await screen.findByText(i18n.t('auth.passwordMinLength'))).toBeInTheDocument()
    expect(changePassword).not.toHaveBeenCalled()
  })

  it('requires the confirmation to match', async () => {
    renderForm()
    await fill('old-password', 'new-password', 'other-password')

    expect(await screen.findByText(i18n.t('auth.passwordsDontMatch'))).toBeInTheDocument()
    expect(field('auth.confirmNewPasswordLabel')).toHaveAttribute('aria-invalid', 'true')
    expect(changePassword).not.toHaveBeenCalled()
  })

  it('requires the current password', async () => {
    renderForm()
    await fill('', 'new-password', 'new-password')

    expect(field('auth.oldPasswordLabel')).toHaveAttribute('aria-invalid', 'true')
    expect(changePassword).not.toHaveBeenCalled()
  })

  it('changes the password, confirms it and clears the form', async () => {
    changePassword.mockResolvedValue(undefined)
    const onSuccess = renderForm()
    await fill('old-password', 'new-password', 'new-password')

    expect(changePassword).toHaveBeenCalledWith({
      oldPassword: 'old-password',
      newPassword: 'new-password',
      confirmNewPassword: 'new-password',
    })
    expect(toast.success).toHaveBeenCalledWith(i18n.t('auth.passwordChanged'))
    expect(onSuccess).toHaveBeenCalledTimes(1)
    expect(field('auth.newPasswordLabel')).toHaveValue('')
  })

  it('puts server validation errors under the matching field', async () => {
    changePassword.mockRejectedValue(
      new ApiError(400, { errors: { OldPassword: ['Current password is wrong'] } }),
    )
    const onSuccess = renderForm()
    await fill('old-password', 'new-password', 'new-password')

    expect(await screen.findByText('Current password is wrong')).toBeInTheDocument()
    expect(onSuccess).not.toHaveBeenCalled()
  })

  it('signs the user out when the session has expired', async () => {
    changePassword.mockRejectedValue(new ApiError(401))
    renderForm()
    await fill('old-password', 'new-password', 'new-password')

    expect(await screen.findByRole('alert')).toHaveTextContent(i18n.t('auth.sessionExpired'))
    expect(logout).toHaveBeenCalledTimes(1)
  })

  it('shows the server detail for other API errors', async () => {
    changePassword.mockRejectedValue(new ApiError(409, { detail: 'Password was used before' }))
    renderForm()
    await fill('old-password', 'new-password', 'new-password')

    expect(await screen.findByRole('alert')).toHaveTextContent('Password was used before')
    expect(logout).not.toHaveBeenCalled()
  })

  it('shows the network error when the server cannot be reached', async () => {
    changePassword.mockRejectedValue(new TypeError('Failed to fetch'))
    renderForm()
    await fill('old-password', 'new-password', 'new-password')

    expect(await screen.findByRole('alert')).toHaveTextContent(i18n.t('common.networkError'))
  })
})
