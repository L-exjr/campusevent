import type { EventManagementApi } from './EventManagementApi'
import {
  apiRequest,
  clearStoredSession,
  readStoredSession,
  writeStoredSession,
} from './httpClient'
import type { PaginatedResponse } from './httpClient'
import { readJwtSessionClaims } from './jwtSession'
import type {
  AuthSession,
  BookingRequest,
  BookingRequestInput,
  EventFilters,
  EventInput,
  EventItem,
  EventRegistrant,
  EventReport,
  OrganizerApplication,
  OrganizerApplicationStatus,
  OrganizerReport,
  Page,
  Role,
  StudentRegistration,
  User,
} from '../types'

interface ApiUser {
  id: string
  name: string
  email: string
  role: string
  isActive: boolean
  createdAt: string
  imageUrl: string | null
}

interface ApiAuthResponse {
  token: string
  expiresAt: string
  user: ApiUser
}

interface ApiEvent {
  id: string
  title: string
  description: string
  date: string
  location: string
  capacity: number
  category: string
  organizerId: string
  organizerName: string
  registrationCount: number
  createdAt: string
  imageUrl: string | null
  isPublished: boolean
  version: number
}

interface ApiStudentRegistration {
  registrationId: string
  registeredAt: string
  attended: boolean
  event: ApiEvent
}

interface ApiRegistrant {
  registrationId: string
  studentId: string
  studentName: string
  studentEmail: string
  registeredAt: string
  attended: boolean
}

interface ApiOrganizerApplication {
  id: string
  userId: string
  userName: string
  userEmail: string
  reason: string
  status: string
  rejectionReason: string | null
  submittedAt: string
  reviewedAt: string | null
  reviewedByAdminId: string | null
  reviewedByAdminName: string | null
}

interface ApiSummaryReport {
  totalEvents: number
  totalRegistrations: number
  totalUsers: number
  overallAttendanceRate: number
}

interface ApiEventReport {
  eventId: string
  eventTitle: string
  registrationCount: number
  attendanceCount: number
  attendanceRate: number
  organizerId: string
  organizerName: string
}

interface ApiOrganizerReport {
  organizerId: string
  organizerName: string
  eventCount: number
  registrationCount: number
}

function mapRole(role: string): Role {
  const normalized = role.toLowerCase()
  if (normalized === 'student' || normalized === 'organizer' || normalized === 'admin') {
    return normalized
  }
  throw new Error(`Unsupported user role: ${role}`)
}

function toApiRole(role: Exclude<Role, 'admin'>) {
  return role === 'organizer' ? 'Organizer' : 'Student'
}

function mapUser(user: ApiUser): User {
  return {
    id: user.id,
    name: user.name,
    email: user.email,
    role: mapRole(user.role),
    active: user.isActive,
    joinedAt: user.createdAt,
    imageUrl: user.imageUrl,
  }
}

function mapApplicationStatus(status: string): OrganizerApplicationStatus {
  const normalized = status.toLowerCase()
  if (normalized === 'pending' || normalized === 'approved' || normalized === 'rejected') {
    return normalized
  }
  throw new Error(`Unsupported organizer application status: ${status}`)
}

function mapOrganizerApplication(application: ApiOrganizerApplication): OrganizerApplication {
  return {
    ...application,
    status: mapApplicationStatus(application.status),
  }
}

function mapEvent(event: ApiEvent): EventItem {
  return {
    id: event.id,
    title: event.title,
    description: event.description,
    date: event.date,
    capacity: event.capacity,
    category: event.category as EventItem['category'],
    location: event.location,
    organizerId: event.organizerId,
    organizerName: event.organizerName,
    createdAt: event.createdAt,
    registeredCount: event.registrationCount,
    imageUrl: event.imageUrl,
    isPublished: event.isPublished ?? true,
    version: event.version,
  }
}

function mapBookingRequest(
  request: Omit<BookingRequest, 'status'> & { status: string },
): BookingRequest {
  return {
    ...request,
    status: `${request.status[0].toLowerCase()}${request.status.slice(1)}` as BookingRequest['status'],
  }
}

function mapPage<TSource, TTarget>(
  source: PaginatedResponse<TSource>,
  mapper: (item: TSource) => TTarget,
): Page<TTarget> {
  return { ...source, items: source.items.map(mapper) }
}

function pageQuery(path: string, page: number, pageSize: number) {
  const separator = path.includes('?') ? '&' : '?'
  return `${path}${separator}page=${page}&pageSize=${pageSize}`
}

function eventPayload(input: EventInput) {
  return {
    ...input,
    date: new Date(input.date).toISOString(),
  }
}

function dateRange(date: string) {
  const start = new Date(`${date}T00:00:00`)
  const end = new Date(`${date}T23:59:59.999`)
  return { from: start.toISOString(), to: end.toISOString() }
}

function buildEventQuery(filters: EventFilters, upcomingOnly: boolean) {
  const query = new URLSearchParams()
  if (filters.search?.trim()) query.set('search', filters.search.trim())
  if (filters.category) query.set('category', filters.category)
  if (filters.date) {
    const range = dateRange(filters.date)
    query.set('from', range.from)
    query.set('to', range.to)
  } else if (upcomingOnly) {
    query.set('from', new Date().toISOString())
  }
  return query.size ? `/events?${query}` : '/events'
}

function saveApiSession(response: ApiAuthResponse): AuthSession {
  const user = mapUser(response.user)
  const claims = readJwtSessionClaims(response.token)
  if (claims.userId !== user.id || claims.role !== user.role) {
    throw new Error('The session token does not match the signed-in user.')
  }
  if (!Number.isFinite(new Date(response.expiresAt).getTime())) {
    throw new Error('The server returned an invalid session expiry.')
  }
  const session = { token: response.token, expiresAt: claims.expiresAt, user }
  writeStoredSession({
    token: response.token,
    expiresAt: claims.expiresAt,
    user: response.user,
  })
  return session
}

export const realApi: EventManagementApi = {
  async login(email, password) {
    const response = await apiRequest<ApiAuthResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    })
    return saveApiSession(response)
  },

  async register(name, email, password) {
    const response = await apiRequest<ApiAuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ name, email, password }),
    })
    return saveApiSession(response)
  },

  async googleLogin(idToken) {
    return saveApiSession(await apiRequest<ApiAuthResponse>('/auth/google', {
      method: 'POST', body: JSON.stringify({ idToken }),
    }))
  },

  async forgotPassword(email) {
    const response = await apiRequest<{ message: string }>('/auth/forgot-password', {
      method: 'POST', body: JSON.stringify({ email }),
    })
    return response.message
  },

  async resetPassword(token, newPassword) {
    const response = await apiRequest<{ message: string }>('/auth/reset-password', {
      method: 'POST', body: JSON.stringify({ token, newPassword }),
    })
    return response.message
  },

  async restoreSession() {
    const stored = readStoredSession()
    if (!stored) return null
    try {
      const claims = readJwtSessionClaims(stored.token)
      const user = mapUser(stored.user as ApiUser)
      if (claims.userId !== user.id || claims.role !== user.role) throw new Error()
      return { token: stored.token, expiresAt: claims.expiresAt, user }
    } catch {
      clearStoredSession()
      return null
    }
  },

  async logout() {
    clearStoredSession()
  },

  async getEvents(filters = {}, page = 1, pageSize = 20) {
    return mapPage(
      await apiRequest<PaginatedResponse<ApiEvent>>(
        pageQuery(buildEventQuery(filters, true), page, pageSize),
      ),
      mapEvent,
    )
  },

  async getEvent(id) {
    return mapEvent(await apiRequest<ApiEvent>(`/events/${id}`))
  },

  async getManagementEvent(id) {
    return mapEvent(await apiRequest<ApiEvent>(`/events/${id}/management`))
  },

  async registerForEvent(eventId) {
    await apiRequest(`/events/${eventId}/register`, { method: 'POST' })
  },

  async isRegisteredForEvent(eventId) {
    const response = await apiRequest<{ isRegistered: boolean }>(
      `/events/${eventId}/registration-status`,
    )
    return response.isRegistered
  },

  async getStudentRegistrations(studentId, page = 1, pageSize = 20) {
    return mapPage(
      await apiRequest<PaginatedResponse<ApiStudentRegistration>>(
        `/students/${studentId}/registrations?page=${page}&pageSize=${pageSize}`,
      ),
      (registration): StudentRegistration => ({
      registration: {
        id: registration.registrationId,
        eventId: registration.event.id,
        studentId,
        registeredAt: registration.registeredAt,
        attended: registration.attended,
      },
      event: mapEvent(registration.event),
      }),
    )
  },

  async getMyOrganizerApplication() {
    const application = await apiRequest<ApiOrganizerApplication | null>(
      '/organizer-applications/mine',
    )
    return application ? mapOrganizerApplication(application) : null
  },

  async submitOrganizerApplication(reason) {
    const application = await apiRequest<ApiOrganizerApplication>('/organizer-applications', {
      method: 'POST',
      body: JSON.stringify({ reason }),
    })
    return mapOrganizerApplication(application)
  },

  async getPendingOrganizerApplications(page = 1, pageSize = 20, search = '') {
    const query = new URLSearchParams({
      status: 'Pending',
      page: String(page),
      pageSize: String(pageSize),
    })
    if (search.trim()) query.set('search', search.trim())
    return mapPage(
      await apiRequest<PaginatedResponse<ApiOrganizerApplication>>(
        `/organizer-applications?${query}`,
      ),
      mapOrganizerApplication,
    )
  },

  async approveOrganizerApplication(id) {
    const application = await apiRequest<ApiOrganizerApplication>(
      `/organizer-applications/${id}/approve`,
      { method: 'PUT' },
    )
    return mapOrganizerApplication(application)
  },

  async rejectOrganizerApplication(id, reason) {
    const application = await apiRequest<ApiOrganizerApplication>(
      `/organizer-applications/${id}/reject`,
      {
        method: 'PUT',
        body: JSON.stringify({ reason: reason?.trim() || null }),
      },
    )
    return mapOrganizerApplication(application)
  },

  async getOrganizerEvents(_organizerId, upcomingOnly = false, page = 1, pageSize = 20) {
    const path = upcomingOnly ? '/events/mine?upcoming=true' : '/events/mine'
    return mapPage(
      await apiRequest<PaginatedResponse<ApiEvent>>(pageQuery(path, page, pageSize)),
      mapEvent,
    )
  },

  async createEvent(input) {
    return mapEvent(await apiRequest<ApiEvent>('/events', {
      method: 'POST',
      body: JSON.stringify(eventPayload(input)),
    }))
  },

  async updateEvent(id, input) {
    return mapEvent(await apiRequest<ApiEvent>(`/events/${id}`, {
      method: 'PUT',
      body: JSON.stringify(eventPayload(input)),
    }))
  },

  async deleteEvent(id) {
    await apiRequest(`/events/${id}`, { method: 'DELETE' })
  },

  async getEventRegistrants(eventId, page = 1, pageSize = 20, search = '', attended) {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (search.trim()) query.set('search', search.trim())
    if (attended !== undefined) query.set('attended', String(attended))
    return mapPage(
      await apiRequest<PaginatedResponse<ApiRegistrant>>(
        `/events/${eventId}/registrants?${query}`,
      ),
      (registrant): EventRegistrant => ({
      registrationId: registrant.registrationId,
      userId: registrant.studentId,
      name: registrant.studentName,
      email: registrant.studentEmail,
      registeredAt: registrant.registeredAt,
      attended: registrant.attended,
      }),
    )
  },

  async updateAttendance(eventId, attendance) {
    await apiRequest(`/events/${eventId}/attendance`, {
      method: 'PUT',
      body: JSON.stringify({
        registrations: Object.entries(attendance).map(([registrationId, attended]) => ({
          registrationId,
          attended,
        })),
      }),
    })
  },

  async getUsers(page = 1, pageSize = 20, search = '', role) {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (search.trim()) query.set('search', search.trim())
    if (role) query.set('role', `${role[0].toUpperCase()}${role.slice(1)}`)
    return mapPage(
      await apiRequest<PaginatedResponse<ApiUser>>(`/users?${query}`),
      mapUser,
    )
  },

  async updateUserRole(id, role) {
    await apiRequest(`/users/${id}/role`, {
      method: 'PUT',
      body: JSON.stringify({ role: toApiRole(role) }),
    })
  },

  async updateUserStatus(id, active) {
    if (active) throw new Error('Reactivating accounts is not supported by the current API.')
    await apiRequest(`/users/${id}/deactivate`, { method: 'PUT' })
  },

  async updateProfile(id, imageUrl) {
    const apiUser = await apiRequest<ApiUser>(`/users/${id}/profile`, {
      method: 'PUT',
      body: JSON.stringify({ imageUrl }),
    })
    const stored = readStoredSession()
    if (stored) writeStoredSession({ ...stored, user: apiUser })
    return mapUser(apiUser)
  },

  async getAllEvents(page = 1, pageSize = 20, filters = {}) {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (filters.search?.trim()) query.set('search', filters.search.trim())
    if (filters.category) query.set('category', filters.category)
    return mapPage(
      await apiRequest<PaginatedResponse<ApiEvent>>(
        `/events/all?${query}`,
      ),
      mapEvent,
    )
  },

  async getReports(page = 1, pageSize = 20) {
    const [summary, eventPage, apiOrganizers] = await Promise.all([
      apiRequest<ApiSummaryReport>('/reports/summary'),
      apiRequest<PaginatedResponse<ApiEventReport>>(
        `/reports/events?page=${page}&pageSize=${pageSize}`,
      ),
      apiRequest<PaginatedResponse<ApiOrganizerReport>>('/reports/organizers?page=1&pageSize=20'),
    ])
    const events = eventPage.items.map<EventReport>((report) => ({
      eventId: report.eventId,
      title: report.eventTitle,
      organizerName: report.organizerName,
      registrations: report.registrationCount,
      attended: report.attendanceCount,
      attendanceRate: report.attendanceRate,
    }))
    const organizers = apiOrganizers.items.map<OrganizerReport>((organizer) => ({
      organizerId: organizer.organizerId,
      organizerName: organizer.organizerName,
      events: organizer.eventCount,
      registrations: organizer.registrationCount,
    }))
    return {
      totalEvents: summary.totalEvents,
      totalRegistrations: summary.totalRegistrations,
      totalUsers: summary.totalUsers,
      attendanceRate: summary.overallAttendanceRate,
      events,
      organizers,
      eventPage: eventPage.page,
      eventPageSize: eventPage.pageSize,
      eventTotalCount: eventPage.totalCount,
      eventTotalPages: eventPage.totalPages,
    }
  },

  async submitBookingRequest(input: BookingRequestInput) {
    const response = await apiRequest<{ message: string }>('/booking-requests', {
      method: 'POST',
      body: JSON.stringify({ ...input, proposedDate: new Date(input.proposedDate).toISOString() }),
    })
    return response.message
  },

  async getBookingRequests(page = 1, pageSize = 20) {
    return mapPage(
      await apiRequest<PaginatedResponse<Omit<BookingRequest, 'status'> & { status: string }>>(
        `/booking-requests?page=${page}&pageSize=${pageSize}`,
      ),
      mapBookingRequest,
    )
  },

  async getAssignedBookingRequests(page = 1, pageSize = 20) {
    return mapPage(
      await apiRequest<PaginatedResponse<Omit<BookingRequest, 'status'> & { status: string }>>(
        `/booking-requests/assigned?page=${page}&pageSize=${pageSize}`,
      ),
      mapBookingRequest,
    )
  },

  async assignBookingRequest(id, organizerId) {
    const request = await apiRequest<Omit<BookingRequest, 'status'> & { status: string }>(
      `/booking-requests/${id}/assign`,
      { method: 'PUT', body: JSON.stringify({ organizerId }) },
    )
    return mapBookingRequest(request)
  },

  async updateBookingRequestStatus(id, status) {
    const request = await apiRequest<Omit<BookingRequest, 'status'> & { status: string }>(
      `/booking-requests/${id}/status`,
      {
        method: 'PUT',
        body: JSON.stringify({ status: `${status[0].toUpperCase()}${status.slice(1)}` }),
      },
    )
    return mapBookingRequest(request)
  },

  async respondToBookingRequest(id, accept, note) {
    const request = await apiRequest<Omit<BookingRequest, 'status'> & { status: string }>(
      `/booking-requests/${id}/respond`,
      {
        method: 'PUT',
        body: JSON.stringify({ accept, note: note?.trim() || null }),
      },
    )
    return mapBookingRequest(request)
  },
}
