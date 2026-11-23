import { describe, expect, it, vi } from 'vitest'
import { createSingleFlight } from './singleFlight'

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

describe('createSingleFlight', () => {
  it('shares one in-flight call between concurrent callers', async () => {
    const run = createSingleFlight<string>()
    const pending = deferred<string>()
    const factory = vi.fn(() => pending.promise)

    const first = run(factory)
    const second = run(factory)
    pending.resolve('token')

    expect(factory).toHaveBeenCalledTimes(1)
    await expect(first).resolves.toBe('token')
    await expect(second).resolves.toBe('token')
  })

  it('starts a fresh call once the previous one has settled', async () => {
    const run = createSingleFlight<number>()
    const factory = vi.fn().mockResolvedValueOnce(1).mockResolvedValueOnce(2)

    await expect(run(factory)).resolves.toBe(1)
    await expect(run(factory)).resolves.toBe(2)
    expect(factory).toHaveBeenCalledTimes(2)
  })

  it('lets the next caller retry after a failure', async () => {
    const run = createSingleFlight<string>()
    const factory = vi.fn().mockRejectedValueOnce(new Error('offline')).mockResolvedValueOnce('ok')

    await expect(run(factory)).rejects.toThrow('offline')
    await expect(run(factory)).resolves.toBe('ok')
  })

  it('keeps separate instances independent', () => {
    const a = createSingleFlight<string>()
    const b = createSingleFlight<string>()
    const factory = vi.fn(() => new Promise<string>(() => {}))

    void a(factory)
    void b(factory)

    expect(factory).toHaveBeenCalledTimes(2)
  })
})
