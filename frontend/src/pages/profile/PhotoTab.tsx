import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ImagePlus, Trash2 } from 'lucide-react'
import { useAuth } from '@/features/auth/AuthProvider'
import { useAuthenticatedBlobUrl } from '@/lib/useAuthenticatedBlobUrl'
import { uploadMyPhoto, deleteMyPhoto } from '@/features/profile/api'
import { ApiError } from '@/api/errors'
import { Button } from '@/components/ui/button'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'

const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const MAX_SIZE = 5 * 1024 * 1024

export function PhotoTab() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [pendingFile, setPendingFile] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [validationError, setValidationError] = useState<string | null>(null)
  const [removeOpen, setRemoveOpen] = useState(false)

  const currentPhotoUrl = useAuthenticatedBlobUrl(user?.hasPhoto ? '/api/auth/me/photo' : null)

  const uploadMutation = useMutation({
    mutationFn: uploadMyPhoto,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['auth', 'me'] })
      toast.success(t('profile.photo.uploaded'))
      resetSelection()
    },
    onError: (err) => {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    },
  })

  const deleteMutation = useMutation({
    mutationFn: deleteMyPhoto,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['auth', 'me'] })
      toast.success(t('profile.photo.removed'))
    },
    onError: (err) => {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    },
  })

  function resetSelection() {
    setPendingFile(null)
    if (previewUrl) URL.revokeObjectURL(previewUrl)
    setPreviewUrl(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    setValidationError(null)
    if (!file) return
    if (!ALLOWED_TYPES.includes(file.type)) {
      setValidationError(t('profile.photo.wrongType'))
      return
    }
    if (file.size > MAX_SIZE) {
      setValidationError(t('profile.photo.tooLarge'))
      return
    }
    setPendingFile(file)
    setPreviewUrl(URL.createObjectURL(file))
  }

  const displayUrl = previewUrl ?? currentPhotoUrl
  const initials = (user?.profile?.fullName ?? user?.contact?.fullName ?? user?.email ?? '?')
    .slice(0, 2)
    .toUpperCase()

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">{t('profile.tabs.photo')}</CardTitle>
        <CardDescription>{t('profile.photo.description')}</CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {validationError && (
          <Alert variant="destructive">
            <AlertDescription>{validationError}</AlertDescription>
          </Alert>
        )}

        <Avatar className="size-24">
          {displayUrl && <AvatarImage src={displayUrl} alt="" />}
          <AvatarFallback className="text-lg">{initials}</AvatarFallback>
        </Avatar>

        <input
          ref={fileInputRef}
          type="file"
          accept={ALLOWED_TYPES.join(',')}
          className="hidden"
          onChange={handleFileChange}
        />

        {pendingFile ? (
          <div className="flex gap-2">
            <Button
              loading={uploadMutation.isPending}
              onClick={() => uploadMutation.mutate(pendingFile)}
            >
              {t('common.save')}
            </Button>
            <Button variant="outline" onClick={resetSelection} disabled={uploadMutation.isPending}>
              {t('common.cancel')}
            </Button>
          </div>
        ) : (
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => fileInputRef.current?.click()}>
              <ImagePlus />
              {user?.hasPhoto ? t('profile.photo.replace') : t('profile.photo.upload')}
            </Button>
            {user?.hasPhoto && (
              <Button variant="outline" onClick={() => setRemoveOpen(true)}>
                <Trash2 />
                {t('profile.photo.remove')}
              </Button>
            )}
          </div>
        )}
      </CardContent>

      <ConfirmDialog
        open={removeOpen}
        onOpenChange={setRemoveOpen}
        title={t('profile.photo.confirmRemoveTitle')}
        description={t('profile.photo.confirmRemoveDesc')}
        destructive
        onConfirm={() => deleteMutation.mutateAsync()}
      />
    </Card>
  )
}
