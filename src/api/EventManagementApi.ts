import type {
  AuthSession,
  BookingRequest,
  BookingRequestInput,
  EventFilters,
  EventInput,
  EventItem,
  EventRegistrant,
  OrganizerApplication,
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
  getEvents(filters?: EventFilters): Promise<EventItem[]>
  getEvent(id: string): Promise<EventItem>
  registerForEvent(eventId: string, studentId: string): Promise<void>
  getStudentRegistrations(studentId: string): Promise<StudentRegistration[]>
  getMyOrganizerApplication(): Promise<OrganizerApplication | null>
  submitOrganizerApplication(reason: string): Promise<OrganizerApplication>
  getPendingOrganizerApplications(): Promise<OrganizerApplication[]>
  approveOrganizerApplication(id: string): Promise<OrganizerApplication>
  rejectOrganizerApplication(id: string, reason?: string): Promise<OrganizerApplication>
  getOrganizerEvents(organizerId: string): Promise<EventItem[]>
  createEvent(input: EventInput): Promise<EventItem>
  updateEvent(id: string, input: EventInput): Promise<EventItem>
  deleteEvent(id: string): Promise<void>
  getEventRegistrants(eventId: string): Promise<EventRegistrant[]>
  updateAttendance(eventId: string, attendance: Record<string, boolean>): Promise<void>
  getUsers(): Promise<User[]>
  updateUserRole(id: string, role: Exclude<Role, 'admin'>): Promise<void>
  updateUserStatus(id: string, active: boolean): Promise<void>
  updateProfile(id: string, imageUrl: string | null): Promise<User>
  getAllEvents(): Promise<EventItem[]>
  getReports(): Promise<ReportsData>
  submitBookingRequest(input: BookingRequestInput): Promise<string>
  getBookingRequests(): Promise<BookingRequest[]>
  assignBookingRequest(id: string, organizerId: string): Promise<BookingRequest>
}
