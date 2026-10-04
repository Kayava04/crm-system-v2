import { Spinner } from '@/components/ui/spinner'

export function FullPageSpinner() {
  return (
    <div className="flex min-h-svh items-center justify-center bg-background">
      <Spinner />
    </div>
  )
}
