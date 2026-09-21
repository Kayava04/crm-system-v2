/** A content-scoped loading spinner for a lazily-loaded route's Suspense
 * fallback — unlike FullPageSpinner, it doesn't cover the sidebar/header. */
export function PageSpinner() {
  return (
    <div className="flex min-h-64 items-center justify-center">
      <div
        className="size-8 animate-spin rounded-full border-2 border-muted border-t-primary"
        role="status"
        aria-label="Loading"
      />
    </div>
  )
}
