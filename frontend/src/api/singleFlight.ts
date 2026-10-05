export function createSingleFlight<T>() {
  let inFlight: Promise<T> | null = null

  return function run(factory: () => Promise<T>): Promise<T> {
    if (!inFlight) {
      inFlight = factory().finally(() => {
        inFlight = null
      })
    }
    return inFlight
  }
}
