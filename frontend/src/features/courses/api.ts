import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type CourseListItem = Schemas['CourseListResponse']
export type CourseDetail = Schemas['CourseDetailResponse']
export type CreateCourseInput = Schemas['CreateCourseRequest']
export type UpdateCourseInput = Schemas['UpdateCourseRequest']
export type CourseStatus = Schemas['CourseStatus']

export interface CourseListFilters {
  search?: string
  language?: Schemas['Language']
  level?: Schemas['Level']
  format?: Schemas['Format']
  lessonType?: Schemas['LessonType']
  status?: CourseStatus
  page?: number
  pageSize?: number
}

export function getCourses(filters: CourseListFilters) {
  return unwrap(
    api.GET('/api/courses', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}

export function getCourseById(id: string) {
  return unwrap(api.GET('/api/courses/{id}', { params: { path: { id } } }))
}

export function createCourse(input: CreateCourseInput) {
  return unwrap(api.POST('/api/courses', { body: input }))
}

export function updateCourse(id: string, input: UpdateCourseInput) {
  return unwrap(api.PUT('/api/courses/{id}', { params: { path: { id } }, body: input }))
}

export function activateCourse(id: string) {
  return unwrap(api.PUT('/api/courses/{id}/activate', { params: { path: { id } } }))
}

export function archiveCourse(id: string) {
  return unwrap(api.PUT('/api/courses/{id}/archive', { params: { path: { id } } }))
}

export function deleteCourse(id: string) {
  return unwrap(api.DELETE('/api/courses/{id}', { params: { path: { id } } }))
}

/** Fetches every course (small, bounded list for a school) for use in
 * selects and for resolving a courseId to a display name client-side. */
export async function getAllCoursesForLookup() {
  const result = await getCourses({ page: 1, pageSize: 200 })
  return result.items
}
