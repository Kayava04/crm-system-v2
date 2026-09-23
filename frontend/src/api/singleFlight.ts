/**
 * Ensures only one call to `factory` is in flight at a time; concurrent callers
 * await the same promise instead of starting their own. Used to serialize token
 * refresh calls (a used refresh token stops working, so two parallel refreshes
 * would make the second one fail).
 */
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
