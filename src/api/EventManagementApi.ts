import type {
  AuthSession,
  BookingRequest,
  BookingRequestInput,
  EventFilters,
  EventInput,
  EventItem,
  EventRegistrant,
  OrganizerApplication,
  Page,
  ReportsData,
  Role,
  StudentRegistration,
  User,
} from '../types'

export interface EventManagementApi {
  login(email: string, password: string): Promise<AuthSession>
  register(name: string, email: string, password: string): Promise<AuthSession>
  googleLogin(idToken: string): Promise<AuthSession>
  forgotPassword(email: string): Promise<string>
  resetPassword(token: string, newPassword: string): Promise<string>
  restoreSession(): Promise<AuthSession | null>
  logout(): Promise<void>
  getEvents(filters?: EventFilters, page?: number, pageSize?: number): Promise<Page<EventItem>>
  getEvent(id: string): Promise<EventItem>
  getManagementEvent(id: string): Promise<EventItem>
  registerForEvent(eventId: string, studentId: string): Promise<void>
  isRegisteredForEvent(eventId: string): Promise<boolean>
  getStudentRegistrations(studentId: string, page?: number, pageSize?: number): Promise<Page<StudentRegistration>>
  getMyOrganizerApplication(): Promise<OrganizerApplication | null>
  submitOrganizerApplication(reason: string): Promise<OrganizerApplication>
  getPendingOrganizerApplications(page?: number, pageSize?: number, search?: string): Promise<Page<OrganizerApplication>>
  approveOrganizerApplication(id: string): Promise<OrganizerApplication>
  rejectOrganizerApplication(id: string, reason?: string): Promise<OrganizerApplication>
  getOrganizerEvents(organizerId: string, upcomingOnly?: boolean, page?: number, pageSize?: number): Promise<Page<EventItem>>
  createEvent(input: EventInput): Promise<EventItem>
  updateEvent(id: string, input: EventInput): Promise<EventItem>
  deleteEvent(id: string): Promise<void>
  getEventRegistrants(
    eventId: string,
    page?: number,
    pageSize?: number,
    search?: string,
    attended?: boolean,
  ): Promise<Page<EventRegistrant>>
  updateAttendance(eventId: string, attendance: Record<string, boolean>): Promise<void>
  getUsers(page?: number, pageSize?: number, search?: string, role?: Role): Promise<Page<User>>
  updateUserRole(id: string, role: Exclude<Role, 'admin'>): Promise<void>
  updateUserStatus(id: string, active: boolean): Promise<void>
  updateProfile(id: string, imageUrl: string | null): Promise<User>
  getAllEvents(page?: number, pageSize?: number, filters?: EventFilters): Promise<Page<EventItem>>
  getReports(page?: number, pageSize?: number): Promise<ReportsData>
  submitBookingRequest(input: BookingRequestInput): Promise<string>
  getBookingRequests(page?: number, pageSize?: number): Promise<Page<BookingRequest>>
  getAssignedBookingRequests(page?: number, pageSize?: number): Promise<Page<BookingRequest>>
  assignBookingRequest(id: string, organizerId: string): Promise<BookingRequest>
  updateBookingRequestStatus(
    id: string,
    status: Extract<BookingRequest['status'], 'underReview' | 'converted' | 'closed'>,
  ): Promise<BookingRequest>
  respondToBookingRequest(id: string, accept: boolean, note?: string): Promise<BookingRequest>
}
