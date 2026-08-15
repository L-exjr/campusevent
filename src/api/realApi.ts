import type { EventManagementApi } from './EventManagementApi'
import { apiRequest, apiDownload } from './httpClient'
import type { PaginatedResponse } from './httpClient'
import type {
  AuthSession,
  AdminAuditLog,
  BookingRequest,
  BookingRequestInput,
  BookingSubmission,
  TrackedBookingRequest,
  CheckInResult,
  CertificateDownload,
  EmailDeadLetter,
  FailedImageCleanup,
  EventFilters,
  EventTeamMember,
  EventInput,
  EventItem,
  EventRegistrant,
  EventPaymentStatus,
  EventReport,
  OrganizerApplication,
  OrganizerApplicationStatus,
  OrganizerDetail,
  OrganizerDirectorySettings,
  OrganizerSummary,
  OrganizerReport,
  Page,
  PaymentInitialization,
  PaymentStatus,
  Role,
  StudentRegistration,
  Ticket,
  User,
  VotingCampaign,
  VotingCampaignInput,
  VotingPaymentInitialization,
  VotingPaymentStatus,
  Coupon,
  CouponInput,
  OrganizerAnalytics,
} from '../types'

interface ApiUser {
  id: string
  name: string
  email: string
  role: string
  verificationStatus?: string
  isActive: boolean
  createdAt: string
  imageUrl: string | null
}

interface ApiAuthResponse {
  expiresAt: string
  user: ApiUser
}

interface ApiEvent {
  id: string
  title: string
  description: string
  date: string
  endDate: string | null
  location: string
  format: 'Physical' | 'Virtual' | 'Hybrid'
  meetingUrl: string | null
  virtualPlatform: EventInput['virtualPlatform']
  latitude: number | null
  longitude: number | null
  instagramUrl: string | null
  twitterUrl: string | null
  facebookUrl: string | null
  websiteUrl: string | null
  ticketingEnabled: boolean
  registrationsEnabled: boolean
  votingEnabled: boolean
  salesStartsAt: string | null
  salesEndsAt: string | null
  capacity: number
  category: string
  organizerId: string
  organizerName: string
  registrationCount: number
  createdAt: string
  imageUrl: string | null
  isPublished: boolean
  version: number
  priceMinor: number
  currency: 'GHS'
  ticketTiers?: EventItem['ticketTiers']
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
  userVerificationStatus?: string
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
    verificationStatus: (user.verificationStatus?.toLowerCase() ?? 'unverified') as User['verificationStatus'],
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
    userVerificationStatus: (application.userVerificationStatus?.toLowerCase() ?? (application.status.toLowerCase() === 'approved' ? 'verified' : application.status.toLowerCase() === 'pending' ? 'pending' : 'unverified')) as OrganizerApplication['userVerificationStatus'],
  }
}

function mapEvent(event: ApiEvent): EventItem {
  return {
    id: event.id,
    title: event.title,
    description: event.description,
    date: event.date,
    endDate: event.endDate,
    capacity: event.capacity,
    category: event.category as EventItem['category'],
    location: event.location,
    format: event.format.toLowerCase() as EventItem['format'],
    meetingUrl: event.meetingUrl,
    virtualPlatform: event.virtualPlatform,
    latitude: event.latitude,
    longitude: event.longitude,
    instagramUrl: event.instagramUrl,
    twitterUrl: event.twitterUrl,
    facebookUrl: event.facebookUrl,
    websiteUrl: event.websiteUrl,
    ticketingEnabled: event.ticketingEnabled,
    registrationsEnabled: event.registrationsEnabled,
    votingEnabled: event.votingEnabled,
    salesStartsAt: event.salesStartsAt,
    salesEndsAt: event.salesEndsAt,
    organizerId: event.organizerId,
    organizerName: event.organizerName,
    createdAt: event.createdAt,
    registeredCount: event.registrationCount,
    imageUrl: event.imageUrl,
    isPublished: event.isPublished ?? true,
    version: event.version,
    priceMinor: event.priceMinor ?? 0,
    currency: event.currency ?? 'GHS',
    ticketTiers: event.ticketTiers ?? [],
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
    endDate: input.endDate ? new Date(input.endDate).toISOString() : null,
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
  if (!Number.isFinite(new Date(response.expiresAt).getTime())) {
    throw new Error('The server returned an invalid session expiry.')
  }
  return { expiresAt: response.expiresAt, user }
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
    try {
      const user = mapUser(await apiRequest<ApiUser>('/auth/session'))
      return { expiresAt: '', user }
    } catch {
      return null
    }
  },

  async logout() {
    await apiRequest('/auth/logout', { method: 'POST' })
  },

  async getEvents(filters = {}, page = 1, pageSize = 20, signal) {
    return mapPage(
      await apiRequest<PaginatedResponse<ApiEvent>>(
        pageQuery(buildEventQuery(filters, true), page, pageSize),
        { signal },
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
  async getEventAccess(id) { return apiRequest(`/events/${id}/access`) },
  async getEventTeam(id) {
    const members = await apiRequest<Array<Omit<EventTeamMember, 'role'> & { role: string | null }>>(`/events/${id}/team`)
    return members.map(member => ({ ...member, role: member.role ? (member.role[0].toLowerCase() + member.role.slice(1)) as EventTeamMember['role'] : null }))
  },
  async inviteEventTeamMember(id, email, role) {
    const apiRole = role === 'checkInStaff' ? 'CheckInStaff' : `${role[0].toUpperCase()}${role.slice(1)}`
    return apiRequest(`/events/${id}/team`, { method: 'POST', body: JSON.stringify({ email, role: apiRole }) })
  },
  async updateEventTeamMember(id, userId, role) {
    const apiRole = role === 'checkInStaff' ? 'CheckInStaff' : `${role[0].toUpperCase()}${role.slice(1)}`
    return apiRequest(`/events/${id}/team/${userId}`, { method: 'PUT', body: JSON.stringify({ role: apiRole }) })
  },
  async removeEventTeamMember(id, userId) { await apiRequest(`/events/${id}/team/${userId}`, { method: 'DELETE' }) },

  async registerForEvent(eventId) {
    await apiRequest(`/events/${eventId}/register`, { method: 'POST' })
  },

  async initializeEventPayment(eventId, ticketTierId, couponCode) {
    const query = new URLSearchParams()
    if (ticketTierId) query.set('ticketTierId', ticketTierId)
    if (couponCode?.trim()) query.set('couponCode', couponCode.trim())
    const suffix = query.size ? `?${query}` : ''
    return apiRequest<PaymentInitialization>(`/payments/events/${eventId}/initialize${suffix}`, {
      method: 'POST',
    })
  },

  async getPaymentStatus(reference) {
    const response = await apiRequest<Omit<EventPaymentStatus, 'status'> & { status: string }>(
      `/payments/${encodeURIComponent(reference)}`,
    )
    const normalized = `${response.status[0].toLowerCase()}${response.status.slice(1)}` as PaymentStatus
    return { ...response, status: normalized }
  },

  async getTicket(registrationId) {
    return apiRequest<Ticket>(`/tickets/${registrationId}`)
  },

  async checkInTicket(eventId, token) {
    return apiRequest<CheckInResult>(`/events/${eventId}/check-in`, {
      method: 'POST',
      body: JSON.stringify({ token }),
    })
  },

  async checkInTicketByCode(eventId, ticketCode) {
    return apiRequest<CheckInResult>(`/events/${eventId}/check-in/manual`, {
      method: 'POST', body: JSON.stringify({ ticketCode }),
    })
  },

  async getCertificate(registrationId) {
    return apiRequest<CertificateDownload>(`/certificates/registrations/${registrationId}`, {
      method: 'POST',
    })
  },

  async getVotingCampaign(eventId) {
    const campaign = await apiRequest<VotingCampaign & {
      categories: Array<VotingCampaign['categories'][number] & { mode: string }>
    }>(`/events/${eventId}/voting`)
    return {
      ...campaign,
      categories: campaign.categories.map((category) => ({
        ...category,
        mode: category.mode.toLowerCase() as VotingCampaign['categories'][number]['mode'],
      })),
    }
  },

  async saveVotingCampaign(eventId, input: VotingCampaignInput) {
    const response = await apiRequest<VotingCampaign & {
      categories: Array<VotingCampaign['categories'][number] & { mode: string }>
    }>(`/events/${eventId}/voting`, {
      method: 'PUT',
      body: JSON.stringify({
        ...input,
        categories: input.categories.map((category) => ({
          ...category,
          mode: category.mode === 'paid' ? 'Paid' : 'Free',
        })),
      }),
    })
    return {
      ...response,
      categories: response.categories.map((category) => ({
        ...category,
        mode: category.mode.toLowerCase() as VotingCampaign['categories'][number]['mode'],
      })),
    }
  },

  async castFreeVote(categoryId, nomineeId) {
    await apiRequest(`/voting/categories/${categoryId}/votes`, {
      method: 'POST',
      body: JSON.stringify({ nomineeId }),
    })
  },

  async initializeVotingPayment(categoryId, nomineeId, quantity) {
    return apiRequest<VotingPaymentInitialization>(
      `/voting/categories/${categoryId}/payments/initialize`,
      { method: 'POST', body: JSON.stringify({ nomineeId, quantity }) },
    )
  },

  async getVotingPaymentStatus(reference) {
    const response = await apiRequest<Omit<VotingPaymentStatus, 'status'> & { status: string }>(
      `/voting/payments/${encodeURIComponent(reference)}`,
    )
    return {
      ...response,
      status: `${response.status[0].toLowerCase()}${response.status.slice(1)}` as PaymentStatus,
    }
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

  async getPendingOrganizerApplications(page = 1, pageSize = 20, search = '', signal) {
    const query = new URLSearchParams({
      status: 'Pending',
      page: String(page),
      pageSize: String(pageSize),
    })
    if (search.trim()) query.set('search', search.trim())
    return mapPage(
      await apiRequest<PaginatedResponse<ApiOrganizerApplication>>(
        `/organizer-applications?${query}`,
        { signal },
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

  async transferEventOwnership(id, organizerId, version) {
    return mapEvent(await apiRequest<ApiEvent>(`/events/${id}/organizer`, {
      method: 'PUT',
      body: JSON.stringify({ organizerId, version }),
    }))
  },

  async deleteEvent(id) {
    await apiRequest(`/events/${id}`, { method: 'DELETE' })
  },

  async getEventRegistrants(eventId, page = 1, pageSize = 20, search = '', attended, signal) {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (search.trim()) query.set('search', search.trim())
    if (attended !== undefined) query.set('attended', String(attended))
    return mapPage(
      await apiRequest<PaginatedResponse<ApiRegistrant>>(
        `/events/${eventId}/registrants?${query}`,
        { signal },
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

  async exportEventRegistrants(eventId) {
    return apiDownload(`/events/${eventId}/registrants/export`)
  },

  async getOrganizerAnalytics() {
    return apiRequest<OrganizerAnalytics>('/events/analytics/mine')
  },

  async getCoupons() { return apiRequest<Coupon[]>('/coupons') },
  async createCoupon(input: CouponInput) {
    return apiRequest<Coupon>('/coupons', { method: 'POST', body: JSON.stringify(input) })
  },
  async updateCoupon(id, input: CouponInput) {
    return apiRequest<Coupon>(`/coupons/${id}`, { method: 'PUT', body: JSON.stringify(input) })
  },

  async getUsers(page = 1, pageSize = 20, search = '', role, verificationStatus, isActive, signal) {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (search.trim()) query.set('search', search.trim())
    if (role) query.set('role', `${role[0].toUpperCase()}${role.slice(1)}`)
    if (verificationStatus) query.set('verificationStatus', `${verificationStatus[0].toUpperCase()}${verificationStatus.slice(1)}`)
    if (isActive !== undefined) query.set('isActive', String(isActive))
    return mapPage(
      await apiRequest<PaginatedResponse<ApiUser>>(`/users?${query}`, { signal }),
      mapUser,
    )
  },

  async searchOrganizers(search = '', pageSize = 20, signal) {
    const query = new URLSearchParams({
      role: 'Organizer',
      isActive: 'true',
      page: '1',
      pageSize: String(pageSize),
    })
    if (search.trim()) query.set('search', search.trim())
    return mapPage(
      await apiRequest<PaginatedResponse<ApiUser>>(`/users?${query}`, { signal }),
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
    return mapUser(apiUser)
  },

  async getAllEvents(page = 1, pageSize = 20, filters = {}, signal) {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (filters.search?.trim()) query.set('search', filters.search.trim())
    if (filters.category) query.set('category', filters.category)
    return mapPage(
      await apiRequest<PaginatedResponse<ApiEvent>>(
        `/events/all?${query}`,
        { signal },
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

  async getFailedEmails(page = 1, pageSize = 20) {
    return apiRequest<PaginatedResponse<EmailDeadLetter>>(
      `/email-outbox/failed?page=${page}&pageSize=${pageSize}`,
    )
  },

  async retryFailedEmail(id) {
    await apiRequest(`/email-outbox/${id}/retry`, { method: 'PUT' })
  },

  async getFailedImageCleanups(page = 1, pageSize = 20) {
    return apiRequest<PaginatedResponse<FailedImageCleanup>>(
      `/image-cleanup/failed?page=${page}&pageSize=${pageSize}`,
    )
  },

  async retryFailedImageCleanup(id) {
    await apiRequest(`/image-cleanup/${id}/retry`, { method: 'PUT' })
  },

  async getAdminAuditLogs(search = '', page = 1, pageSize = 20, signal) {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (search.trim()) query.set('search', search.trim())
    return apiRequest<PaginatedResponse<AdminAuditLog>>(
      `/admin-audit-logs?${query}`,
      { signal },
    )
  },

  async exportAdminAuditLogs(from, to) {
    const query = new URLSearchParams()
    if (from) query.set('from', from)
    if (to) query.set('to', to)
    return apiDownload(`/admin-audit-logs/export${query.size ? `?${query}` : ''}`)
  },

  async submitBookingRequest(input: BookingRequestInput) {
    return apiRequest<BookingSubmission>('/booking-requests', {
      method: 'POST',
      body: JSON.stringify({
        ...input,
        proposedDate: new Date(input.proposedDate).toISOString(),
        expectedEndDate: input.expectedEndDate ? new Date(input.expectedEndDate).toISOString() : null,
      }),
    })
  },

  async trackBookingRequest(id, token) {
    return apiRequest<TrackedBookingRequest>(`/booking-requests/${id}/track?token=${encodeURIComponent(token)}`)
  },

  async getOrganizers(search = '', category = '', page = 1, pageSize = 12, signal) {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (search.trim()) query.set('search', search.trim())
    if (category) query.set('category', category)
    return apiRequest<PaginatedResponse<OrganizerSummary>>(`/organizers?${query}`, { signal })
  },

  async getOrganizer(id) {
    const organizer = await apiRequest<Omit<OrganizerDetail, 'events'> & { events: ApiEvent[] }>(`/organizers/${id}`)
    return { ...organizer, events: organizer.events.map(mapEvent) }
  },

  async getOrganizerDirectorySettings() {
    return apiRequest<OrganizerDirectorySettings>('/organizers/me/settings')
  },

  async updateOrganizerDirectorySettings(settings) {
    return apiRequest<OrganizerDirectorySettings>('/organizers/me/settings', { method: 'PUT', body: JSON.stringify(settings) })
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

  async submitBookingRequestQuote(id, proposedFeeMinor, proposedTimeline, message) {
    const request = await apiRequest<Omit<BookingRequest, 'status'> & { status: string }>(
      `/booking-requests/${id}/quote`,
      { method: 'POST', body: JSON.stringify({ proposedFeeMinor, proposedTimeline, message }) },
    )
    return mapBookingRequest(request)
  },
}
