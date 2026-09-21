import { useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import { X } from 'lucide-react'
import {
  broadcastNotification,
  sendNotification,
  sendInvoiceReminders,
  sendLessonReminders,
  getAllNotifications,
} from '@/features/notifications/api'
import type { NotificationType, BroadcastAudience } from '@/features/notifications/api'
import { getStudents } from '@/features/students/api'
import { getTeachers } from '@/features/teachers/api'
import { ApiError } from '@/api/errors'
import { enumLabel } from '@/lib/enumLabels'
import { toNum, formatDateTime } from '@/lib/utils'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Field } from '@/components/shared/Field'
import { DataTable } from '@/components/shared/DataTable'
import { Pagination } from '@/components/shared/Pagination'

const AUDIENCES: BroadcastAudience[] = ['Students', 'Teachers', 'Admins', 'Everyone']
const TYPES: NotificationType[] = [
  'PasswordChangeRequired',
  'InvoiceReminder',
  'LessonReminder',
  'General',
]

export function AdminNotificationsPage() {
  const { t } = useTranslation()
  const [tab, setTab] = useState('tools')

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold tracking-tight">{t('notifications.admin.title')}</h1>

      <Tabs value={tab} onValueChange={setTab}>
        <TabsList>
          <TabsTrigger value="tools">{t('notifications.admin.tabs.tools')}</TabsTrigger>
          <TabsTrigger value="log">{t('notifications.admin.tabs.log')}</TabsTrigger>
        </TabsList>

        <TabsContent value="tools">
          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <BroadcastCard />
            <SendToUsersCard />
            <InvoiceRemindersCard />
            <LessonRemindersCard />
          </div>
        </TabsContent>

        <TabsContent value="log">
          <LogCard />
        </TabsContent>
      </Tabs>
    </div>
  )
}

function BroadcastCard() {
  const { t } = useTranslation()
  const schema = z.object({
    audience: z.enum(['Students', 'Teachers', 'Admins', 'Everyone']),
    subject: z.string().min(1),
    body: z.string().min(1),
  })
  type FormValues = z.infer<typeof schema>

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { audience: 'Everyone' },
  })

  async function onSubmit(values: FormValues) {
    try {
      const result = await broadcastNotification(values)
      toast.success(t('notifications.admin.broadcast.success', { count: Number(result.created) }))
      reset({ audience: values.audience, subject: '', body: '' })
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">
          {t('notifications.admin.broadcast.title')}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <form className="flex flex-col gap-3" onSubmit={handleSubmit(onSubmit)} noValidate>
          <Field label={t('notifications.admin.broadcast.audienceLabel')}>
            <Controller
              control={control}
              name="audience"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {AUDIENCES.map((a) => (
                      <SelectItem key={a} value={a}>
                        {enumLabel(t, 'broadcastAudience', a)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </Field>
          <Field
            label={t('notifications.admin.broadcast.subjectLabel')}
            error={errors.subject?.message}
          >
            <Input {...register('subject')} aria-invalid={!!errors.subject} />
          </Field>
          <Field label={t('notifications.admin.broadcast.bodyLabel')} error={errors.body?.message}>
            <Textarea rows={3} {...register('body')} aria-invalid={!!errors.body} />
          </Field>
          <Button type="submit" loading={isSubmitting} className="w-fit">
            {t('notifications.admin.broadcast.submit')}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}

interface Recipient {
  id: string
  label: string
}

function SendToUsersCard() {
  const { t } = useTranslation()
  const [source, setSource] = useState<'student' | 'teacher'>('student')
  const [query, setQuery] = useState('')
  const debouncedQuery = useDebouncedValue(query)
  const [recipients, setRecipients] = useState<Recipient[]>([])
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: studentResults } = useQuery({
    queryKey: ['students', 'notify-search', debouncedQuery],
    queryFn: () => getStudents({ search: debouncedQuery, page: 1, pageSize: 8 }),
    enabled: source === 'student' && debouncedQuery.length >= 2,
  })
  const { data: teacherResults } = useQuery({
    queryKey: ['teachers', 'notify-search', debouncedQuery],
    queryFn: () => getTeachers({ search: debouncedQuery, page: 1, pageSize: 8 }),
    enabled: source === 'teacher' && debouncedQuery.length >= 2,
  })

  function addRecipient(r: Recipient) {
    setRecipients((prev) => (prev.some((p) => p.id === r.id) ? prev : [...prev, r]))
    setQuery('')
  }

  async function handleSubmit() {
    setError(null)
    if (recipients.length === 0) {
      setError(t('notifications.admin.send.noRecipients'))
      return
    }
    if (!subject.trim() || !body.trim()) return
    setSubmitting(true)
    try {
      const result = await sendNotification({
        recipientUserIds: recipients.map((r) => r.id),
        subject,
        body,
      })
      toast.success(t('notifications.admin.send.success', { count: Number(result.created) }))
      setRecipients([])
      setSubject('')
      setBody('')
    } catch (err) {
      setError(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setSubmitting(false)
    }
  }

  const results = source === 'student' ? studentResults?.items : teacherResults?.items

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">
          {t('notifications.admin.send.title')}
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        {error && <p className="text-sm text-destructive">{error}</p>}

        <Field label={t('notifications.admin.send.recipientsLabel')}>
          <div className="flex flex-col gap-2">
            {recipients.length > 0 && (
              <div className="flex flex-wrap gap-1.5">
                {recipients.map((r) => (
                  <Badge key={r.id} variant="secondary" className="gap-1">
                    {r.label}
                    <button
                      type="button"
                      onClick={() => setRecipients((prev) => prev.filter((p) => p.id !== r.id))}
                    >
                      <X className="size-3" />
                    </button>
                  </Badge>
                ))}
              </div>
            )}
            <div className="flex gap-2">
              <Button
                type="button"
                size="sm"
                variant={source === 'student' ? 'default' : 'outline'}
                onClick={() => {
                  setSource('student')
                  setQuery('')
                }}
              >
                {t('notifications.admin.send.sourceStudent')}
              </Button>
              <Button
                type="button"
                size="sm"
                variant={source === 'teacher' ? 'default' : 'outline'}
                onClick={() => {
                  setSource('teacher')
                  setQuery('')
                }}
              >
                {t('notifications.admin.send.sourceTeacher')}
              </Button>
            </div>
            <Input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder={
                source === 'student'
                  ? t('notifications.admin.send.searchStudentPlaceholder')
                  : t('notifications.admin.send.searchTeacherPlaceholder')
              }
            />
            {results && results.length > 0 && (
              <div className="flex flex-col rounded-md border border-border">
                {results.map((p) => (
                  <button
                    key={p.id}
                    type="button"
                    className="px-3 py-1.5 text-left text-sm hover:bg-muted"
                    onClick={() => addRecipient({ id: p.id, label: p.fullName })}
                  >
                    {p.fullName} <span className="text-xs text-muted-foreground">{p.email}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        </Field>

        <Field label={t('notifications.admin.send.subjectLabel')}>
          <Input value={subject} onChange={(e) => setSubject(e.target.value)} />
        </Field>
        <Field label={t('notifications.admin.send.bodyLabel')}>
          <Textarea rows={3} value={body} onChange={(e) => setBody(e.target.value)} />
        </Field>

        <Button type="button" loading={submitting} className="w-fit" onClick={handleSubmit}>
          {t('notifications.admin.send.submit')}
        </Button>
      </CardContent>
    </Card>
  )
}

function InvoiceRemindersCard() {
  const { t } = useTranslation()
  const [daysBeforeDue, setDaysBeforeDue] = useState('3')
  const [includeOverdue, setIncludeOverdue] = useState(true)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit() {
    setSubmitting(true)
    try {
      const result = await sendInvoiceReminders({
        daysBeforeDue: Number(daysBeforeDue) || 0,
        includeOverdue,
      })
      toast.success(
        t('notifications.admin.invoiceReminders.result', {
          found: Number(result.invoicesFound),
          created: Number(result.created),
          skippedSent: Number(result.skippedAlreadySent),
          skippedNoAccount: Number(result.skippedNoAccount),
        }),
      )
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">
          {t('notifications.admin.invoiceReminders.title')}
        </CardTitle>
        <CardDescription>{t('notifications.admin.invoiceReminders.description')}</CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <Field label={t('notifications.admin.invoiceReminders.daysBeforeDueLabel')}>
          <Input
            type="number"
            min={0}
            value={daysBeforeDue}
            onChange={(e) => setDaysBeforeDue(e.target.value)}
          />
        </Field>
        <div className="flex items-center gap-2">
          <Checkbox
            id="includeOverdue"
            checked={includeOverdue}
            onCheckedChange={(v) => setIncludeOverdue(v === true)}
          />
          <Label htmlFor="includeOverdue" className="font-normal">
            {t('notifications.admin.invoiceReminders.includeOverdueLabel')}
          </Label>
        </div>
        <Button type="button" loading={submitting} className="w-fit" onClick={handleSubmit}>
          {t('notifications.admin.invoiceReminders.submit')}
        </Button>
      </CardContent>
    </Card>
  )
}

function LessonRemindersCard() {
  const { t } = useTranslation()
  const [hoursAhead, setHoursAhead] = useState('24')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit() {
    setSubmitting(true)
    try {
      const result = await sendLessonReminders({ hoursAhead: Number(hoursAhead) || 0 })
      toast.success(
        t('notifications.admin.lessonReminders.result', {
          found: Number(result.lessonsFound),
          created: Number(result.created),
          skippedSent: Number(result.skippedAlreadySent),
        }),
      )
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">
          {t('notifications.admin.lessonReminders.title')}
        </CardTitle>
        <CardDescription>{t('notifications.admin.lessonReminders.description')}</CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <Field label={t('notifications.admin.lessonReminders.hoursAheadLabel')}>
          <Input
            type="number"
            min={0}
            value={hoursAhead}
            onChange={(e) => setHoursAhead(e.target.value)}
          />
        </Field>
        <Button type="button" loading={submitting} className="w-fit" onClick={handleSubmit}>
          {t('notifications.admin.lessonReminders.submit')}
        </Button>
      </CardContent>
    </Card>
  )
}

function LogCard() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const [type, setType] = useState('')
  const [unreadOnly, setUnreadOnly] = useState(false)
  const [page, setPage] = useState(1)

  const filters = {
    type: (type || undefined) as NotificationType | undefined,
    unreadOnly: unreadOnly || undefined,
    page,
    pageSize: 20,
  }

  const { data, isPending, isPlaceholderData } = useQuery({
    queryKey: ['notifications', 'all', filters],
    queryFn: () => getAllNotifications(filters),
    placeholderData: keepPreviousData,
  })

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-end gap-3">
        <Select
          value={type}
          onValueChange={(v) => {
            setType(v === 'any' ? '' : v)
            setPage(1)
          }}
        >
          <SelectTrigger className="w-56">
            <SelectValue placeholder={t('notifications.admin.log.filters.type')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('notifications.admin.log.filters.any')}</SelectItem>
            {TYPES.map((tp) => (
              <SelectItem key={tp} value={tp}>
                {enumLabel(t, 'notificationType', tp)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <div className="flex items-center gap-2">
          <Checkbox
            id="logUnreadOnly"
            checked={unreadOnly}
            onCheckedChange={(v) => {
              setUnreadOnly(v === true)
              setPage(1)
            }}
          />
          <Label htmlFor="logUnreadOnly" className="font-normal">
            {t('notifications.admin.log.filters.unreadOnly')}
          </Label>
        </div>
        {(type || unreadOnly) && (
          <Button
            variant="ghost"
            onClick={() => {
              setType('')
              setUnreadOnly(false)
              setPage(1)
            }}
          >
            {t('notifications.admin.log.filters.clear')}
          </Button>
        )}
      </div>

      <div className={isPlaceholderData ? 'opacity-60 transition-opacity' : undefined}>
        <DataTable
          columns={[
            {
              key: 'recipient',
              header: t('notifications.admin.log.columns.recipient'),
              cell: (row) => (
                <span className="font-mono text-xs">{row.recipientUserId.slice(0, 8)}…</span>
              ),
            },
            {
              key: 'type',
              header: t('notifications.admin.log.columns.type'),
              cell: (row) => enumLabel(t, 'notificationType', row.type),
            },
            {
              key: 'subject',
              header: t('notifications.admin.log.columns.subject'),
              cell: (row) => row.subject,
            },
            {
              key: 'status',
              header: t('notifications.admin.log.columns.status'),
              cell: (row) => (
                <Badge variant={row.isRead ? 'secondary' : 'default'}>
                  {row.isRead
                    ? t('notifications.admin.log.read')
                    : t('notifications.admin.log.unread')}
                </Badge>
              ),
            },
            {
              key: 'createdAt',
              header: t('notifications.admin.log.columns.createdAt'),
              cell: (row) => formatDateTime(row.createdAt, lang),
            },
          ]}
          rows={data?.items ?? []}
          rowKey={(row) => row.id}
          isLoading={isPending}
          emptyMessage={t('notifications.admin.log.empty')}
        />
      </div>

      {data && (
        <Pagination
          page={toNum(data.page)}
          totalPages={toNum(data.totalPages)}
          totalCount={toNum(data.totalCount)}
          onPageChange={setPage}
        />
      )}
    </div>
  )
}
