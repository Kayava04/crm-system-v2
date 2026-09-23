import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Pagination } from './Pagination'

describe('Pagination', () => {
  it('renders nothing when totalCount is 0', () => {
    const { container } = render(
      <Pagination page={1} totalPages={1} totalCount={0} onPageChange={vi.fn()} />,
    )
    expect(container).toBeEmptyDOMElement()
  })

  it('disables the previous button on the first page', () => {
    render(<Pagination page={1} totalPages={3} totalCount={30} onPageChange={vi.fn()} />)
    const [prev, next] = screen.getAllByRole('button')
    expect(prev).toBeDisabled()
    expect(next).not.toBeDisabled()
  })

  it('disables the next button on the last page', () => {
    render(<Pagination page={3} totalPages={3} totalCount={30} onPageChange={vi.fn()} />)
    const [prev, next] = screen.getAllByRole('button')
    expect(prev).not.toBeDisabled()
    expect(next).toBeDisabled()
  })

  it('calls onPageChange with page-1 / page+1 on click', async () => {
    const user = userEvent.setup()
    const onPageChange = vi.fn()
    render(<Pagination page={2} totalPages={3} totalCount={30} onPageChange={onPageChange} />)
    const [prev, next] = screen.getAllByRole('button')

    await user.click(prev)
    expect(onPageChange).toHaveBeenLastCalledWith(1)

    await user.click(next)
    expect(onPageChange).toHaveBeenLastCalledWith(3)
  })

  it('gives both icon-only buttons an accessible name', () => {
    render(<Pagination page={1} totalPages={3} totalCount={30} onPageChange={vi.fn()} />)
    const buttons = screen.getAllByRole('button')
    for (const button of buttons) {
      expect(button).toHaveAccessibleName()
    }
  })
})
