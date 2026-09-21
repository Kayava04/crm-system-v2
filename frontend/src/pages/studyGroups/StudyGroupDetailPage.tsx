import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ArrowLeft, Pencil, Plus, Trash2 } from 'lucide-react'
import { getGroupById, updateGroup, removeGroupMember } from '@/features/studyGroups/api'
import { getCourseById } from '@/features/courses/api'
import { getTeacherById } from '@/features/teachers/api'
import { ApiError } from '@/api/errors'
import { formatDate } from '@/lib/utils'
import { useCan } from '@/features/auth/useCan'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Field } from '@/components/shared/Field'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { TeacherSearchInput } from '@/components/shared/TeacherSearchInput'
import { AddGroupMemberDialog } from './AddGroupMemberDialog'

export function StudyGroupDetailPage() {
  const { id = '' } = useParams()
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const queryClient = useQueryClient()
  const canManage = useCan('CanManageSchedule')

  const [editing, setEditing] = useState(false)
  const [nameValue, setNameValue] = useState('')
  const [teacherValue, setTeacherValue] = useState<{ id: string; fullName: string } | null>(null)
  const [saving, setSaving] = useState(false)
  const [addMemberOpen, setAddMemberOpen] = useState(false)
  const [removeTarget, setRemoveTarget] = useState<{
    enrollmentId: string
    studentName: string
  } | null>(null)

  const { data: group, isLoading } = useQuery({
    queryKey: ['study-groups', id],
    queryFn: () => getGroupById(id),
    enabled: !!id,
  })

  const { data: course } = useQuery({
    queryKey: ['courses', group?.courseId],
    queryFn: () => getCourseById(group!.courseId),
    enabled: !!group?.courseId,
  })

  const { data: teacher } = useQuery({
    queryKey: ['teachers', group?.teacherId],
    queryFn: () => getTeacherById(group!.teacherId),
    enabled: !!group?.teacherId,
  })

  function startEditing() {
    if (!group) return
    setNameValue(group.name)
    setTeacherValue(
      teacher ? { id: teacher.id, fullName: `${teacher.lastName} ${teacher.firstName}` } : null,
    )
    setEditing(true)
  }

  async function handleSave() {
    if (!teacherValue) return
    setSaving(true)
    try {
      await updateGroup(id, { name: nameValue, teacherId: teacherValue.id })
      await queryClient.invalidateQueries({ queryKey: ['study-groups', id] })
      toast.success(t('studyGroups.detail.profileSaved'))
      setEditing(false)
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setSaving(false)
    }
  }

  async function handleRemoveMember() {
    if (!removeTarget) return
    await removeGroupMember(id, removeTarget.enrollmentId)
    await queryClient.invalidateQueries({ queryKey: ['study-groups', id] })
    toast.success(t('studyGroups.detail.memberRemoved'))
  }

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-40 w-full" />
      </div>
    )
  }

  if (!group) return null

  return (
    <div className="flex max-w-2xl flex-col gap-4">
      <Link
        to="/study-groups"
        className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" />
        {t('studyGroups.detail.backToList')}
      </Link>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-lg font-medium">{group.name}</CardTitle>
          {canManage && !editing && (
            <Button variant="outline" size="sm" onClick={startEditing}>
              <Pencil />
              {t('studyGroups.detail.edit')}
            </Button>
          )}
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {editing ? (
            <>
              <Field label={t('studyGroups.create.nameLabel')}>
                <Input value={nameValue} onChange={(e) => setNameValue(e.target.value)} />
              </Field>
              <Field label={t('studyGroups.create.teacherLabel')}>
                <TeacherSearchInput value={teacherValue} onChange={setTeacherValue} />
              </Field>
              <div className="flex gap-2">
                <Button size="sm" loading={saving} onClick={handleSave}>
                  {t('studyGroups.detail.save')}
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  disabled={saving}
                  onClick={() => setEditing(false)}
                >
                  {t('studyGroups.detail.cancelEdit')}
                </Button>
              </div>
            </>
          ) : (
            <div className="flex flex-col gap-1 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('studyGroups.columns.course')}</span>
                <span>{course?.name ?? '—'}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('studyGroups.columns.teacher')}</span>
                <span>{teacher ? `${teacher.lastName} ${teacher.firstName}` : '—'}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('studyGroups.columns.members')}</span>
                <span>{group.members.length}</span>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-base font-medium">
            {t('studyGroups.detail.membersTitle')}
          </CardTitle>
          {canManage && (
            <Button size="sm" variant="outline" onClick={() => setAddMemberOpen(true)}>
              <Plus />
              {t('studyGroups.detail.addMember')}
            </Button>
          )}
        </CardHeader>
        <CardContent className="flex flex-col gap-2">
          {group.members.length === 0 && (
            <p className="text-sm text-muted-foreground">{t('studyGroups.detail.noMembers')}</p>
          )}
          {group.members.map((m) => (
            <div
              key={m.enrollmentId}
              className={`flex items-center justify-between rounded-md border border-border px-3 py-2 text-sm${m.isActive ? '' : ' opacity-50'}`}
            >
              <div className="flex flex-col">
                <span>{m.studentName}</span>
                <span className="text-xs text-muted-foreground">
                  {t('studyGroups.detail.joinedAt')} {formatDate(m.joinedAt, lang)}
                </span>
              </div>
              {canManage && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() =>
                    setRemoveTarget({ enrollmentId: m.enrollmentId, studentName: m.studentName })
                  }
                >
                  <Trash2 className="size-4" />
                </Button>
              )}
            </div>
          ))}
        </CardContent>
      </Card>

      <AddGroupMemberDialog
        open={addMemberOpen}
        onOpenChange={setAddMemberOpen}
        groupId={id}
        courseId={group.courseId}
        onAdded={() => queryClient.invalidateQueries({ queryKey: ['study-groups', id] })}
      />

      <ConfirmDialog
        open={!!removeTarget}
        onOpenChange={(open) => !open && setRemoveTarget(null)}
        title={t('studyGroups.detail.confirmRemoveMemberTitle')}
        description={t('studyGroups.detail.confirmRemoveMemberDesc')}
        destructive
        onConfirm={handleRemoveMember}
      />
    </div>
  )
}
