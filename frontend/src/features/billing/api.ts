import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type InvoiceListItem = Schemas['InvoiceListResponse']
export type InvoiceDetail = Schemas['InvoiceDetailResponse']
export type InvoiceStatus = Schemas['InvoiceStatus']
export type PayrollListItem = Schemas['PayrollListResponse']
export type PayrollDetail = Schemas['PayrollDetailResponse']
export type PayrollStatus = Schemas['PayrollStatus']

export interface InvoiceListFilters {
  studentId?: string
  enrollmentId?: string
  period?: string
  status?: InvoiceStatus
  page?: number
  pageSize?: number
}

export function getInvoices(filters: InvoiceListFilters) {
  return unwrap(
    api.GET('/api/billing/invoices', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}

export function getInvoiceById(id: string) {
  return unwrap(api.GET('/api/billing/invoices/{id}', { params: { path: { id } } }))
}

export function createInvoice(input: Schemas['CreateInvoiceRequest']) {
  return unwrap(api.POST('/api/billing/invoices', { body: input }))
}

export function markInvoicePaid(id: string) {
  return unwrap(api.PUT('/api/billing/invoices/{id}/paid', { params: { path: { id } } }))
}

export function markInvoicesOverdue() {
  return unwrap(api.PUT('/api/billing/invoices/mark-overdue', {}))
}

export interface MyInvoiceFilters {
  status?: InvoiceStatus
  page?: number
  pageSize?: number
}

export function getMyInvoices(filters: MyInvoiceFilters = {}) {
  return unwrap(
    api.GET('/api/billing/invoices/my', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}

export interface PayrollListFilters {
  teacherId?: string
  period?: string
  status?: PayrollStatus
  page?: number
  pageSize?: number
}

export function getPayrolls(filters: PayrollListFilters) {
  return unwrap(
    api.GET('/api/billing/payrolls', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}

export function getPayrollById(id: string) {
  return unwrap(api.GET('/api/billing/payrolls/{id}', { params: { path: { id } } }))
}

export function createPayroll(input: Schemas['CreatePayrollRequest']) {
  return unwrap(api.POST('/api/billing/payrolls', { body: input }))
}

export function markPayrollPaid(id: string) {
  return unwrap(api.PUT('/api/billing/payrolls/{id}/paid', { params: { path: { id } } }))
}

export interface MyPayrollFilters {
  period?: string
  status?: PayrollStatus
  page?: number
  pageSize?: number
}

export function getMyPayrolls(filters: MyPayrollFilters = {}) {
  return unwrap(
    api.GET('/api/billing/payrolls/my', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}
