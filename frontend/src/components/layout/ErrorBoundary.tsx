import { Component, type ErrorInfo, type ReactNode } from 'react'
import { AlertTriangle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { StatusScreen } from '@/components/shared/StatusScreen'

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
      <StatusScreen
        icon={AlertTriangle}
        iconClassName="bg-destructive/10 text-destructive"
        title="Щось пішло не так"
        description="Сталася непередбачена помилка інтерфейсу. Спробуйте перезавантажити сторінку."
        action={<Button onClick={() => window.location.reload()}>Перезавантажити</Button>}
      />
    )
  }
}
