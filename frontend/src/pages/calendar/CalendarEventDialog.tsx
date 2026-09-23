import { useEffect } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  createCalendarEvent,
  updateCalendarEvent,
  type CalendarEvent,
  type CalendarEventVisibility,
} from '@/features/scheduling/calendarEvents'
import { useCan } from '@/features/auth/useCan'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Field } from '@/components/shared/Field'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

function toLocalInput(iso: string): string {
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function defaultStart(): string {
  const d = new Date()
  d.setMinutes(0, 0, 0)
  d.setHours(d.getHours() + 1)
  return toLocalInput(d.toISOString())
}

function defaultEnd(): string {
  const d = new Date()
  d.setMinutes(0, 0, 0)
  d.setHours(d.getHours() + 2)
  return toLocalInput(d.toISOString())
}

const schema = z
  .object({
    visibility: z.enum(['Personal', 'Everyone']),
    title: z.string().min(1),
    description: z.string().optional(),
    startsAt: z.string().min(1),
    endsAt: z.string().min(1),
    isAllDay: z.boolean(),
  })
  .refine((v) => new Date(v.endsAt) >= new Date(v.startsAt), {
    path: ['endsAt'],
    message: 'endBeforeStart',
  })
type FormValues = z.infer<typeof schema>

interface CalendarEventDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  event: CalendarEvent | null
  defaultVisibility: CalendarEventVisibility
  onSaved: () => void
}

/** Creates a new event (event=null) or edits an existing one. Everyone-visibility
 * is only offered to a CanManageSchedule holder; the server enforces the same
 * rule regardless, this just avoids offering an option that would just 403. */
export function CalendarEventDialog({
  open,
  onOpenChange,
  event,
  defaultVisibility,
  onSaved,
}: CalendarEventDialogProps) {
  const { t } = useTranslation()
  const canManageSchedule = useCan('CanManageSchedule')

  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (!open) return
    if (event) {
      reset({
        visibility: event.visibility,
        title: event.title,
        description: event.description ?? '',
        startsAt: toLocalInput(event.startsAt),
        endsAt: toLocalInput(event.endsAt),
        isAllDay: event.isAllDay,
      })
    } else {
      reset({
        visibility: defaultVisibility,
        title: '',
        description: '',
        startsAt: defaultStart(),
        endsAt: defaultEnd(),
        isAllDay: false,
      })
    }
  }, [open, event, defaultVisibility, reset])

  async function onSubmit(values: FormValues) {
    try {
      const input = {
        visibility: values.visibility,
        title: values.title,
        description: values.description || null,
        startsAt: new Date(values.startsAt).toISOString(),
        endsAt: new Date(values.endsAt).toISOString(),
        isAllDay: values.isAllDay,
      }
      if (event) {
        await updateCalendarEvent(event.id, input)
        toast.success(t('calendarEvents.updated'))
      } else {
        await createCalendarEvent(input)
        toast.success(t('calendarEvents.created'))
      }
      onOpenChange(false)
      onSaved()
    } catch (err) {
      if (err instanceof ApiError && err.isValidation) {
        applyServerValidation(setError, err)
        return
      }
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {event ? t('calendarEvents.edit') : t('calendarEvents.add')}
          </DialogTitle>
        </DialogHeader>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
          {canManageSchedule && (
            <Field label={t('calendarEvents.visibilityLabel')}>
              <Controller
                control={control}
                name="visibility"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Personal">{t('calendarEvents.visibilityPersonal')}</SelectItem>
                      <SelectItem value="Everyone">{t('calendarEvents.visibilityEveryone')}</SelectItem>
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
          )}

          <Field label={t('calendarEvents.titleLabel')} error={errors.title?.message && t('calendarEvents.titleRequired')}>
            <Input aria-invalid={!!errors.title} {...register('title')} />
          </Field>

          <Field label={t('calendarEvents.descriptionLabel')} hint={t('calendarEvents.descriptionOptional')}>
            <Textarea rows={3} {...register('description')} />
          </Field>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label={t('calendarEvents.startLabel')} error={errors.startsAt?.message ? ' ' : undefined}>
              <Input type="datetime-local" aria-invalid={!!errors.startsAt} {...register('startsAt')} />
            </Field>
            <Field
              label={t('calendarEvents.endLabel')}
              error={errors.endsAt?.message ? t('calendarEvents.endBeforeStart') : undefined}
            >
              <Input type="datetime-local" aria-invalid={!!errors.endsAt} {...register('endsAt')} />
            </Field>
          </div>

          <div className="flex items-center gap-2">
            <Controller
              control={control}
              name="isAllDay"
              render={({ field }) => (
                <Checkbox
                  id="isAllDay"
                  checked={field.value}
                  onCheckedChange={(v) => field.onChange(v === true)}
                />
              )}
            />
            <Label htmlFor="isAllDay" className="font-normal">
              {t('calendarEvents.allDayLabel')}
            </Label>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isSubmitting}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" loading={isSubmitting}>
              {t('calendarEvents.save')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
