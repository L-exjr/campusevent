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
  EventInput,
  EventItem,
  EventAccess,
  EventTeamMember,
  EventTeamRole,
  EventRegistrant,
  EventPaymentStatus,
  OrganizerApplication,
  OrganizerDetail,
  OrganizerDirectorySettings,
  OrganizerSummary,
  Page,
  PaymentInitialization,
  OrganizerAnalytics,
  Coupon,
  CouponInput,
  ReportsData,
  Role,
  StudentRegistration,
  Ticket,
  User,
  VotingCampaign,
  VotingCampaignInput,
  VotingPaymentInitialization,
  VotingPaymentStatus,
  VerificationStatus,
} from '../types'

export interface EventManagementApi {
  login(email: string, password: string): Promise<AuthSession>
  register(name: string, email: string, password: string): Promise<AuthSession>
  googleLogin(idToken: string): Promise<AuthSession>
  forgotPassword(email: string): Promise<string>
  resetPassword(token: string, newPassword: string): Promise<string>
  restoreSession(): Promise<AuthSession | null>
  logout(): Promise<void>
  getEvents(filters?: EventFilters, page?: number, pageSize?: number, signal?: AbortSignal): Promise<Page<EventItem>>
  getEvent(id: string): Promise<EventItem>
  getManagementEvent(id: string): Promise<EventItem>
  getEventAccess(id: string): Promise<EventAccess>
  getEventTeam(id: string): Promise<EventTeamMember[]>
  inviteEventTeamMember(id: string, email: string, role: EventTeamRole): Promise<EventTeamMember>
  updateEventTeamMember(id: string, userId: string, role: EventTeamRole): Promise<EventTeamMember>
  removeEventTeamMember(id: string, userId: string): Promise<void>
  registerForEvent(eventId: string, studentId: string): Promise<void>
  initializeEventPayment(eventId: string, ticketTierId?: string, couponCode?: string): Promise<PaymentInitialization>
  getPaymentStatus(reference: string): Promise<EventPaymentStatus>
  getTicket(registrationId: string): Promise<Ticket>
  checkInTicket(eventId: string, token: string): Promise<CheckInResult>
  checkInTicketByCode(eventId: string, ticketCode: string): Promise<CheckInResult>
  getCertificate(registrationId: string): Promise<CertificateDownload>
  getVotingCampaign(eventId: string): Promise<VotingCampaign>
  saveVotingCampaign(eventId: string, input: VotingCampaignInput): Promise<VotingCampaign>
  castFreeVote(categoryId: string, nomineeId: string): Promise<void>
  initializeVotingPayment(
    categoryId: string,
    nomineeId: string,
    quantity: number,
  ): Promise<VotingPaymentInitialization>
  getVotingPaymentStatus(reference: string): Promise<VotingPaymentStatus>
  isRegisteredForEvent(eventId: string): Promise<boolean>
  getStudentRegistrations(studentId: string, page?: number, pageSize?: number): Promise<Page<StudentRegistration>>
  getMyOrganizerApplication(): Promise<OrganizerApplication | null>
  submitOrganizerApplication(reason: string): Promise<OrganizerApplication>
  getPendingOrganizerApplications(page?: number, pageSize?: number, search?: string, signal?: AbortSignal): Promise<Page<OrganizerApplication>>
  approveOrganizerApplication(id: string): Promise<OrganizerApplication>
  rejectOrganizerApplication(id: string, reason?: string): Promise<OrganizerApplication>
  getOrganizerEvents(organizerId: string, upcomingOnly?: boolean, page?: number, pageSize?: number): Promise<Page<EventItem>>
  createEvent(input: EventInput): Promise<EventItem>
  updateEvent(id: string, input: EventInput): Promise<EventItem>
  transferEventOwnership(id: string, organizerId: string, version: number): Promise<EventItem>
  deleteEvent(id: string): Promise<void>
  getEventRegistrants(
    eventId: string,
    page?: number,
    pageSize?: number,
    search?: string,
    attended?: boolean,
    signal?: AbortSignal,
  ): Promise<Page<EventRegistrant>>
  updateAttendance(eventId: string, attendance: Record<string, boolean>): Promise<void>
  exportEventRegistrants(eventId: string): Promise<Blob>
  getOrganizerAnalytics(): Promise<OrganizerAnalytics>
  getCoupons(): Promise<Coupon[]>
  createCoupon(input: CouponInput): Promise<Coupon>
  updateCoupon(id: string, input: CouponInput): Promise<Coupon>
  getUsers(page?: number, pageSize?: number, search?: string, role?: Role, verificationStatus?: VerificationStatus, isActive?: boolean, signal?: AbortSignal): Promise<Page<User>>
  searchOrganizers(search?: string, pageSize?: number, signal?: AbortSignal): Promise<Page<User>>
  updateUserRole(id: string, role: Exclude<Role, 'admin'>): Promise<void>
  updateUserStatus(id: string, active: boolean): Promise<void>
  updateProfile(id: string, imageUrl: string | null): Promise<User>
  getAllEvents(page?: number, pageSize?: number, filters?: EventFilters, signal?: AbortSignal): Promise<Page<EventItem>>
  getReports(page?: number, pageSize?: number): Promise<ReportsData>
  getFailedEmails(page?: number, pageSize?: number): Promise<Page<EmailDeadLetter>>
  retryFailedEmail(id: string): Promise<void>
  getFailedImageCleanups(page?: number, pageSize?: number): Promise<Page<FailedImageCleanup>>
  retryFailedImageCleanup(id: string): Promise<void>
  getAdminAuditLogs(
    search?: string,
    page?: number,
    pageSize?: number,
    signal?: AbortSignal,
  ): Promise<Page<AdminAuditLog>>
  exportAdminAuditLogs(from?: string, to?: string): Promise<Blob>
  submitBookingRequest(input: BookingRequestInput): Promise<BookingSubmission>
  trackBookingRequest(id: string, token: string): Promise<TrackedBookingRequest>
  getOrganizers(search?: string, category?: string, page?: number, pageSize?: number, signal?: AbortSignal): Promise<Page<OrganizerSummary>>
  getOrganizer(id: string): Promise<OrganizerDetail>
  getOrganizerDirectorySettings(): Promise<OrganizerDirectorySettings>
  updateOrganizerDirectorySettings(settings: OrganizerDirectorySettings): Promise<OrganizerDirectorySettings>
  getBookingRequests(page?: number, pageSize?: number): Promise<Page<BookingRequest>>
  getAssignedBookingRequests(page?: number, pageSize?: number): Promise<Page<BookingRequest>>
  assignBookingRequest(id: string, organizerId: string): Promise<BookingRequest>
  updateBookingRequestStatus(
    id: string,
    status: Extract<BookingRequest['status'], 'underReview' | 'converted' | 'closed'>,
  ): Promise<BookingRequest>
  respondToBookingRequest(id: string, accept: boolean, note?: string): Promise<BookingRequest>
  submitBookingRequestQuote(id: string, proposedFeeMinor: number, proposedTimeline: string, message: string): Promise<BookingRequest>
}
