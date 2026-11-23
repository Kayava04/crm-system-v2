import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import i18n from '@/i18n'
import { ConfirmDialog } from './ConfirmDialog'

function renderDialog(props: Partial<Parameters<typeof ConfirmDialog>[0]> = {}) {
  const onOpenChange = vi.fn()
  const onConfirm = vi.fn()
  render(
    <ConfirmDialog
      open
      onOpenChange={onOpenChange}
      title="Delete student?"
      description="This cannot be undone."
      onConfirm={onConfirm}
      {...props}
    />,
  )
  return { onOpenChange, onConfirm, ...props }
}

describe('ConfirmDialog', () => {
  it('shows the title, description and default button labels', () => {
    renderDialog()
    expect(screen.getByRole('dialog', { name: 'Delete student?' })).toBeInTheDocument()
    expect(screen.getByText('This cannot be undone.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: i18n.t('common.cancel') })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: i18n.t('common.confirm') })).toBeInTheDocument()
  })

  it('uses custom button labels', () => {
    renderDialog({ confirmLabel: 'Delete', cancelLabel: 'Keep' })
    expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Keep' })).toBeInTheDocument()
  })

  it('renders nothing while closed', () => {
    renderDialog({ open: false })
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('closes without confirming on cancel', async () => {
    const user = userEvent.setup()
    const { onOpenChange, onConfirm } = renderDialog()

    await user.click(screen.getByRole('button', { name: i18n.t('common.cancel') }))

    expect(onConfirm).not.toHaveBeenCalled()
    expect(onOpenChange).toHaveBeenCalledWith(false)
  })

  it('runs onConfirm and then closes', async () => {
    const user = userEvent.setup()
    const { onOpenChange, onConfirm } = renderDialog()

    await user.click(screen.getByRole('button', { name: i18n.t('common.confirm') }))

    expect(onConfirm).toHaveBeenCalledTimes(1)
    expect(onOpenChange).toHaveBeenCalledWith(false)
  })

  it('locks both buttons while an async confirm is pending', async () => {
    const user = userEvent.setup()
    let finish!: () => void
    const onConfirm = vi.fn(() => new Promise<void>((resolve) => (finish = resolve)))
    const { onOpenChange } = renderDialog({ onConfirm })

    await user.click(screen.getByRole('button', { name: i18n.t('common.confirm') }))

    expect(screen.getByRole('button', { name: i18n.t('common.confirm') })).toBeDisabled()
    expect(screen.getByRole('button', { name: i18n.t('common.cancel') })).toBeDisabled()
    expect(onOpenChange).not.toHaveBeenCalled()

    finish()
    await waitFor(() => expect(onOpenChange).toHaveBeenCalledWith(false))
  })
})
