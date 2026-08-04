import type { EventManagementApi } from './EventManagementApi'
import {
  apiRequest,
  clearStoredSession,
  fetchAllPages,
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

  async getEvents(filters = {}) {
    return (await fetchAllPages<ApiEvent>(buildEventQuery(filters, true))).map(mapEvent)
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

  async getStudentRegistrations(studentId) {
    const registrations = await fetchAllPages<ApiStudentRegistration>(
      `/students/${studentId}/registrations`,
    )
    return registrations.map<StudentRegistration>((registration) => ({
      registration: {
        id: registration.registrationId,
        eventId: registration.event.id,
        studentId,
        registeredAt: registration.registeredAt,
        attended: registration.attended,
      },
      event: mapEvent(registration.event),
    }))
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

  async getPendingOrganizerApplications() {
    const applications = await fetchAllPages<ApiOrganizerApplication>(
      '/organizer-applications?status=Pending',
    )
    return applications.map(mapOrganizerApplication)
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

  async getOrganizerEvents(_organizerId, upcomingOnly = false) {
    return (await fetchAllPages<ApiEvent>(
      upcomingOnly ? '/events/mine?upcoming=true' : '/events/mine',
    )).map(mapEvent)
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

  async getEventRegistrants(eventId) {
    const registrants = await fetchAllPages<ApiRegistrant>(`/events/${eventId}/registrants`)
    return registrants.map<EventRegistrant>((registrant) => ({
      registrationId: registrant.registrationId,
      userId: registrant.studentId,
      name: registrant.studentName,
      email: registrant.studentEmail,
      registeredAt: registrant.registeredAt,
      attended: registrant.attended,
    }))
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

  async getUsers() {
    return (await fetchAllPages<ApiUser>('/users')).map(mapUser)
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

  async getAllEvents() {
    return (await fetchAllPages<ApiEvent>('/events/all')).map(mapEvent)
  },

  async getReports(page = 1, pageSize = 20) {
    const [summary, eventPage, apiOrganizers] = await Promise.all([
      apiRequest<ApiSummaryReport>('/reports/summary'),
      apiRequest<PaginatedResponse<ApiEventReport>>(
        `/reports/events?page=${page}&pageSize=${pageSize}`,
      ),
      fetchAllPages<ApiOrganizerReport>('/reports/organizers'),
    ])
    const events = eventPage.items.map<EventReport>((report) => ({
      eventId: report.eventId,
      title: report.eventTitle,
      organizerName: report.organizerName,
      registrations: report.registrationCount,
      attended: report.attendanceCount,
      attendanceRate: report.attendanceRate,
    }))
    const organizers = apiOrganizers.map<OrganizerReport>((organizer) => ({
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

  async getBookingRequests() {
    const requests = await fetchAllPages<Omit<BookingRequest, 'status'> & { status: string }>(
      '/booking-requests',
    )
    return requests.map(mapBookingRequest)
  },

  async getAssignedBookingRequests() {
    const requests = await fetchAllPages<Omit<BookingRequest, 'status'> & { status: string }>(
      '/booking-requests/assigned',
    )
    return requests.map(mapBookingRequest)
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
