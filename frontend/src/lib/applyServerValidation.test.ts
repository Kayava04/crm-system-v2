import { describe, expect, it, vi } from 'vitest'
import type { UseFormSetError } from 'react-hook-form'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from './applyServerValidation'

type Values = { email: string; firstName: string }

function fakeSetError() {
  return vi.fn<UseFormSetError<Values>>()
}

describe('applyServerValidation', () => {
  it('maps PascalCase backend fields onto camelCase form fields', () => {
    const setError = fakeSetError()
    applyServerValidation<Values>(
      setError,
      new ApiError(400, { errors: { Email: ['Invalid'], FirstName: ['Required'] } }),
    )

    expect(setError).toHaveBeenCalledWith('email', { type: 'server', message: 'Invalid' })
    expect(setError).toHaveBeenCalledWith('firstName', { type: 'server', message: 'Required' })
  })

  it('joins several messages for one field into a single message', () => {
    const setError = fakeSetError()
    applyServerValidation<Values>(
      setError,
      new ApiError(400, { errors: { Email: ['Required.', 'Must be an email.'] } }),
    )

    expect(setError).toHaveBeenCalledTimes(1)
    expect(setError).toHaveBeenCalledWith('email', {
      type: 'server',
      message: 'Required. Must be an email.',
    })
  })

  it('does nothing when the error carries no field errors', () => {
    const setError = fakeSetError()
    applyServerValidation<Values>(setError, new ApiError(409, { detail: 'Conflict' }))
    expect(setError).not.toHaveBeenCalled()
  })
})
