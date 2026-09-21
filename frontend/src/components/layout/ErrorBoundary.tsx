import { Component, type ErrorInfo, type ReactNode } from 'react'
import { AlertTriangle } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface Props {
  children: ReactNode
}
interface State {
  error: Error | null
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: Error): State {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Unhandled UI error', error, info.componentStack)
  }

  render() {
    if (!this.state.error) return this.props.children
    return (
      <div className="flex min-h-svh flex-col items-center justify-center gap-3 px-4 text-center">
        <AlertTriangle className="size-10 text-destructive" />
        <h1 className="text-xl font-semibold">Щось пішло не так</h1>
        <p className="max-w-sm text-sm text-muted-foreground">
          Сталася непередбачена помилка інтерфейсу. Спробуйте перезавантажити сторінку.
        </p>
        <Button className="mt-2" onClick={() => window.location.reload()}>
          Перезавантажити
        </Button>
      </div>
    )
  }
}
