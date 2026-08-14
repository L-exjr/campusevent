export type Role = 'student' | 'organizer' | 'admin'
export type OrganizerApplicationStatus = 'pending' | 'approved' | 'rejected'
export type BookingRequestStatus =
  | 'submitted' | 'underReview' | 'sentToOrganizer' | 'accepted'
  | 'declined' | 'converted' | 'closed'

export type EventCategory =
  | 'Art & Exhibition'
  | 'Awards Event'
  | 'Comedy Shows'
  | 'Concerts & Music'
  | 'Conferences'
  | 'Cultural Events'
  | 'Education & Learning'
  | 'Fashion & Beauty'
  | 'Festivals'
  | 'Food & Drink'
  | 'Gaming & Esports'
  | 'Hackathons'
  | 'Health & Wellness'
  | 'Movies & Film'
  | 'Other'
  | 'Pageant'
  | 'Parties & Nightlife'
  | 'Startup & Tech'
  | 'Workshops & Training'

export type EventFormat = 'physical' | 'virtual' | 'hybrid'
export type VirtualPlatform = 'zoom' | 'googleMeet' | 'microsoftTeams' | 'youtubeLive' | 'custom'

export interface User {
  id: string
  name: string
  email: string
  role: Role
  active: boolean
  joinedAt: string
  imageUrl: string | null
}

export interface Page<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface EventItem {
  id: string
  title: string
  description: string
  date: string
  endDate?: string | null
  capacity: number
  category: EventCategory
  location: string
  format: EventFormat
  meetingUrl: string | null
  virtualPlatform?: VirtualPlatform | null
  latitude?: number | null
  longitude?: number | null
  instagramUrl?: string | null
  twitterUrl?: string | null
  facebookUrl?: string | null
  websiteUrl?: string | null
  ticketingEnabled?: boolean
  registrationsEnabled?: boolean
  votingEnabled?: boolean
  salesStartsAt: string | null
  salesEndsAt: string | null
  organizerId: string
  organizerName: string
  createdAt: string
  registeredCount: number
  imageUrl: string | null
  isPublished: boolean
  version: number
  priceMinor: number
  currency: 'GHS'
  ticketTiers?: TicketTier[]
}

export interface TicketTier {
  id: string
  name: string
  priceMinor: number
  capacity: number
  sold: number
  isActive: boolean
}

export interface EventInput {
  title: string
  description: string
  date: string
  endDate?: string | null
  capacity: number
  category: EventCategory
  location: string
  format: EventFormat
  meetingUrl: string | null
  virtualPlatform?: VirtualPlatform | null
  latitude?: number | null
  longitude?: number | null
  instagramUrl?: string | null
  twitterUrl?: string | null
  facebookUrl?: string | null
  websiteUrl?: string | null
  ticketingEnabled?: boolean
  registrationsEnabled?: boolean
  votingEnabled?: boolean
  salesStartsAt: string | null
  salesEndsAt: string | null
  imageUrl: string | null
  isPublished?: boolean
  version?: number
  priceMinor: number
  currency: 'GHS'
  ticketTiers?: Array<{ id?: string | null; name: string; priceMinor: number; capacity: number }>
}

export type PaymentStatus =
  | 'pending'
  | 'verified'
  | 'failed'
  | 'expired'
  | 'refundPending'
  | 'refunded'
  | 'refundFailed'

export interface PaymentInitialization {
  reference: string
  authorizationUrl: string
  amountMinor: number
  originalAmountMinor?: number
  discountAmountMinor?: number
  ticketTierId?: string | null
  ticketTierName?: string | null
  couponCode?: string | null
  currency: string
  expiresAt: string
}

export interface EventPaymentStatus {
  reference: string
  status: PaymentStatus
  amountMinor: number
  currency: string
  registrationId: string | null
  expiresAt: string
}

export interface Ticket {
  registrationId: string
  eventId: string
  eventTitle: string
  studentName: string
  ticketCode?: string
  token: string
  expiresAt: string
}

export interface CheckInResult {
  registrationId: string
  eventId: string
  studentName: string
  checkedInAt: string
}

export interface CertificateDownload {
  registrationId: string
  downloadUrl: string
  expiresAt: string
  generatedAt: string
}

export type VotingMode = 'free' | 'paid'

export interface VotingNominee {
  id: string
  name: string
  description: string | null
  voteCount: number | null
}

export interface VotingCategory {
  id: string
  name: string
  description: string | null
  mode: VotingMode
  pricePerVoteMinor: number
  currency: 'GHS'
  hasVoted: boolean
  nominees: VotingNominee[]
}

export interface VotingCampaign {
  id: string
  eventId: string
  eventTitle: string
  opensAt: string
  closesAt: string
  isPublished: boolean
  showLiveResults?: boolean
  status: 'Draft' | 'Scheduled' | 'Open' | 'Closed'
  canManage: boolean
  resultsVisible: boolean
  categories: VotingCategory[]
}

export interface VotingCampaignInput {
  opensAt: string
  closesAt: string
  isPublished: boolean
  showLiveResults?: boolean
  categories: Array<{
    name: string
    description?: string
    mode: VotingMode
    pricePerVoteMinor: number
    nominees: Array<{ name: string; description?: string }>
  }>
}

export interface VotingPaymentInitialization {
  reference: string
  authorizationUrl: string
  categoryId: string
  nomineeId: string
  quantity: number
  amountMinor: number
  currency: 'GHS'
  expiresAt: string
}

export interface VotingPaymentStatus {
  reference: string
  status: PaymentStatus
  categoryId: string
  nomineeId: string
  quantity: number
  amountMinor: number
  currency: 'GHS'
  voteRecorded: boolean
  expiresAt: string
}

export interface OrganizerAnalytics {
  registrationCount: number
  ticketRevenueMinor: number
  currency: 'GHS'
  attendedCount: number
  attendanceRate: number
  registrationsOverTime: Array<{ date: string; registrations: number }>
}

export interface Coupon {
  id: string
  code: string
  percentageDiscount: number
  usageLimit: number | null
  used: number
  eventId: string | null
  eventTitle: string | null
  expiresAt: string | null
  isActive: boolean
}

export type CouponInput = Omit<Coupon, 'id' | 'used' | 'eventTitle'>

export interface BookingRequestInput {
  organizationName: string
  contactName: string
  email: string
  phone: string
  eventType: string
  proposedDate: string
  alternativeDates?: string
  flexibilityNote?: string
  estimatedAttendance: number
  preferredOrganizer?: string
  requestedOrganizerId?: string | null
  description: string
  website: string
}

export interface BookingRequest extends Omit<BookingRequestInput, 'website'> {
  id: string
  status: BookingRequestStatus
  assignedOrganizerId: string | null
  requestedOrganizerId: string | null
  requestedOrganizerName: string | null
  assignedOrganizerName: string | null
  organizerResponseNote: string | null
  draftEventId: string | null
  submittedAt: string
  updatedAt: string
  personalDataAnonymizedAt?: string | null
}

export interface OrganizerSummary {
  id: string
  name: string
  imageUrl: string | null
  bannerUrl: string | null
  bio: string | null
  specialties: EventCategory[]
}

export interface OrganizerDetail extends OrganizerSummary {
  instagramUrl: string | null
  twitterUrl: string | null
  facebookUrl: string | null
  websiteUrl: string | null
  events: EventItem[]
}

export interface OrganizerDirectorySettings {
  isVisible: boolean
  bio: string | null
  bannerUrl: string | null
  instagramUrl: string | null
  twitterUrl: string | null
  facebookUrl: string | null
  websiteUrl: string | null
  specialties: EventCategory[]
}

export interface EventFilters {
  search?: string
  category?: string
  date?: string
}

export interface Registration {
  id: string
  eventId: string
  studentId: string
  registeredAt: string
  attended: boolean
}

export interface StudentRegistration {
  registration: Registration
  event: EventItem
}

export interface EventRegistrant {
  registrationId: string
  userId: string
  name: string
  email: string
  registeredAt: string
  attended: boolean
}

export interface AuthSession {
  expiresAt: string
  user: User
}

export interface OrganizerApplication {
  id: string
  userId: string
  userName: string
  userEmail: string
  reason: string
  status: OrganizerApplicationStatus
  rejectionReason: string | null
  submittedAt: string
  reviewedAt: string | null
  reviewedByAdminId: string | null
  reviewedByAdminName: string | null
}

export interface EventReport {
  eventId: string
  title: string
  organizerName: string
  registrations: number
  attended: number
  attendanceRate: number
}

export interface OrganizerReport {
  organizerId: string
  organizerName: string
  events: number
  registrations: number
}

export interface ReportsData {
  totalEvents: number
  totalRegistrations: number
  totalUsers: number
  attendanceRate: number
  events: EventReport[]
  organizers: OrganizerReport[]
  eventPage: number
  eventPageSize: number
  eventTotalCount: number
  eventTotalPages: number
}

export interface EmailDeadLetter {
  id: string
  kind: string
  aggregateId: string
  attemptCount: number
  lifetimeAttemptCount: number
  manualRetryCount: number
  lastRetriedAt: string | null
  lastError: string | null
  createdAt: string
  canRetry: boolean
}

export interface FailedImageCleanup {
  id: string
  bucket: string
  objectKey: string
  kind: string
  deleteAttemptCount: number
  lifetimeDeleteAttemptCount: number
  manualRetryCount: number
  lastRetriedAt: string | null
  lastError: string | null
  createdAt: string
}

export interface AdminAuditLog {
  id: string
  actorUserId: string
  actorName: string
  action: string
  targetType: string
  targetId: string
  detailsJson: string
  createdAt: string
}

export const EVENT_CATEGORIES: EventCategory[] = [
  'Art & Exhibition',
  'Awards Event',
  'Comedy Shows',
  'Concerts & Music',
  'Conferences',
  'Cultural Events',
  'Education & Learning',
  'Fashion & Beauty',
  'Festivals',
  'Food & Drink',
  'Gaming & Esports',
  'Hackathons',
  'Health & Wellness',
  'Movies & Film',
  'Other',
  'Pageant',
  'Parties & Nightlife',
  'Startup & Tech',
  'Workshops & Training',
]
