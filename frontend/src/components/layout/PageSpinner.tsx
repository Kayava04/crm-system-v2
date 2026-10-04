import { Spinner } from '@/components/ui/spinner'

/** A content-scoped loading spinner for a lazily-loaded route's Suspense
 * fallback — unlike FullPageSpinner, it doesn't cover the sidebar/header. */
export function PageSpinner() {
  return (
    <div className="flex min-h-64 items-center justify-center">
      <Spinner />
    </div>
  )
}
