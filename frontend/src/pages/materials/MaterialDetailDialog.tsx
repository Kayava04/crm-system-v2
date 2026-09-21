import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ExternalLink, Pencil, Trash2 } from 'lucide-react'
import { getMaterialById, updateMaterial, deleteMaterial } from '@/features/materials/api'
import { getTeacherById } from '@/features/teachers/api'
import type { MaterialListItem } from '@/features/materials/api'
import { ApiError } from '@/api/errors'
import { enumLabel } from '@/lib/enumLabels'
import { formatDateTime } from '@/lib/utils'
import { useAuth } from '@/features/auth/AuthProvider'
import { useCan, useHasRole } from '@/features/auth/useCan'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { Field } from '@/components/shared/Field'

const TYPES = ['Video', 'Article', 'Link'] as const

interface MaterialDetailDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  courseName?: string
  row: MaterialListItem | null
  onChanged: () => void
}

export function MaterialDetailDialog({
  open,
  onOpenChange,
  courseName,
  row,
  onChanged,
}: MaterialDetailDialogProps) {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const { user } = useAuth()
  const canManageAny = useCan('CanManageMaterials')
  const isTeacher = useHasRole('Teacher')

  const [editing, setEditing] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [saving, setSaving] = useState(false)
  const [type, setType] = useState<(typeof TYPES)[number]>('Link')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [url, setUrl] = useState('')
  const [body, setBody] = useState('')

  const { data: detail } = useQuery({
    queryKey: ['materials', row?.id, 'detail'],
    queryFn: () => getMaterialById(row!.id),
    enabled: open && !!row,
  })

  // Best-effort: the author is usually the teacher who created the material, but
  // could also be an admin (no unified user lookup exists to resolve that case).
  const { data: authorTeacher } = useQuery({
    queryKey: ['teachers', detail?.authorUserId, 'lookup-name'],
    queryFn: () => getTeacherById(detail!.authorUserId),
    enabled: !!detail?.authorUserId,
    retry: false,
  })

  if (!row) return null

  const canEdit = canManageAny || (isTeacher && detail?.authorUserId === user?.userId)

  function startEditing() {
    if (!detail) return
    setType(detail.type)
    setTitle(detail.title)
    setDescription(detail.description ?? '')
    setUrl(detail.url ?? '')
    setBody(detail.body ?? '')
    setEditing(true)
  }

  async function handleSave() {
    setSaving(true)
    try {
      await updateMaterial(row!.id, {
        type,
        title,
        description: description || null,
        body: body || null,
        url: url || null,
      })
      toast.success(t('materials.detail.saved'))
      onChanged()
      setEditing(false)
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete() {
    await deleteMaterial(row!.id)
    toast.success(t('materials.detail.deleted'))
    onChanged()
    onOpenChange(false)
  }

  function handleClose(next: boolean) {
    if (!next) setEditing(false)
    onOpenChange(next)
  }

  return (
    <>
      <Dialog open={open} onOpenChange={handleClose}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              {editing ? t('materials.detail.edit') : row.title}
              {!editing && (
                <Badge variant="outline">{enumLabel(t, 'materialType', row.type)}</Badge>
              )}
            </DialogTitle>
          </DialogHeader>

          {editing ? (
            <div className="flex flex-col gap-4">
              <Field label={t('materials.createDialog.typeLabel')}>
                <Select value={type} onValueChange={(v) => setType(v as (typeof TYPES)[number])}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {TYPES.map((tp) => (
                      <SelectItem key={tp} value={tp}>
                        {enumLabel(t, 'materialType', tp)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </Field>
              <Field label={t('materials.createDialog.titleLabel')}>
                <Input value={title} onChange={(e) => setTitle(e.target.value)} />
              </Field>
              <Field label={t('materials.createDialog.descriptionLabel')}>
                <Textarea
                  rows={2}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                />
              </Field>
              {(type === 'Video' || type === 'Link') && (
                <Field label={t('materials.createDialog.urlLabel')}>
                  <Input value={url} onChange={(e) => setUrl(e.target.value)} />
                </Field>
              )}
              {type === 'Article' && (
                <Field label={t('materials.createDialog.bodyLabel')}>
                  <Textarea rows={6} value={body} onChange={(e) => setBody(e.target.value)} />
                </Field>
              )}
              <div className="flex gap-2">
                <Button size="sm" loading={saving} onClick={handleSave}>
                  {t('materials.detail.save')}
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  disabled={saving}
                  onClick={() => setEditing(false)}
                >
                  {t('materials.detail.cancelEdit')}
                </Button>
              </div>
            </div>
          ) : (
            <div className="flex flex-col gap-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('materials.detail.course')}</span>
                <span>{courseName ?? '—'}</span>
              </div>
              {authorTeacher && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('materials.detail.author')}</span>
                  <span>
                    {authorTeacher.lastName} {authorTeacher.firstName}
                  </span>
                </div>
              )}
              {detail?.description && (
                <div className="flex flex-col gap-1">
                  <span className="text-muted-foreground">{t('materials.detail.description')}</span>
                  <span>{detail.description}</span>
                </div>
              )}
              {detail?.embedUrl && (
                <div className="aspect-video w-full overflow-hidden rounded-md border border-border">
                  <iframe
                    src={detail.embedUrl}
                    className="size-full"
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                    allowFullScreen
                    title={row.title}
                  />
                </div>
              )}
              {detail?.body && (
                <div className="flex flex-col gap-1">
                  <span className="text-muted-foreground">{t('materials.detail.body')}</span>
                  <p className="whitespace-pre-wrap">{detail.body}</p>
                </div>
              )}
              {detail?.url && detail.type !== 'Video' && (
                <a
                  href={detail.url}
                  target="_blank"
                  rel="noreferrer"
                  className="flex w-fit items-center gap-1 text-sm font-medium text-primary hover:underline"
                >
                  <ExternalLink className="size-4" />
                  {t('materials.detail.openLink')}
                </a>
              )}
              <div className="flex justify-between text-xs text-muted-foreground">
                <span>{t('materials.detail.createdAt')}</span>
                <span>{formatDateTime(row.createdAt, lang)}</span>
              </div>
            </div>
          )}

          {!editing && canEdit && (
            <DialogFooter className="flex-wrap gap-2">
              <Button variant="outline" onClick={startEditing}>
                <Pencil />
                {t('materials.detail.edit')}
              </Button>
              <Button variant="destructive" onClick={() => setDeleteOpen(true)}>
                <Trash2 />
                {t('materials.detail.delete')}
              </Button>
            </DialogFooter>
          )}
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title={t('materials.detail.deleteConfirmTitle')}
        description={t('materials.detail.deleteConfirmDesc')}
        destructive
        onConfirm={handleDelete}
      />
    </>
  )
}
