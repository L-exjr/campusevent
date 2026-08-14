import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import AppLayout from './components/layout/AppLayout'
import GuestRoute from './components/routing/GuestRoute'
import ProtectedRoute from './components/routing/ProtectedRoute'
import { ROUTE_ROLES } from './utils/permissions'
import SeoManager from './components/seo/SeoManager'
import Analytics from './components/seo/Analytics'
import StructuredBusinessData from './components/seo/StructuredBusinessData'

const AdminDashboardPage = lazy(() => import('./pages/admin/AdminDashboardPage'))
const AdminEventsPage = lazy(() => import('./pages/admin/AdminEventsPage'))
const AdminOrganizerApplicationsPage = lazy(() => import('./pages/admin/AdminOrganizerApplicationsPage'))
const AdminUsersPage = lazy(() => import('./pages/admin/AdminUsersPage'))
const AdminEmailOutboxPage = lazy(() => import('./pages/admin/AdminEmailOutboxPage'))
const AdminAuditLogsPage = lazy(() => import('./pages/admin/AdminAuditLogsPage'))
const AdminImageCleanupPage = lazy(() => import('./pages/admin/AdminImageCleanupPage'))
const AdminBookingQueue = lazy(() => import('./pages/admin/AdminBookingQueue'))
const LoginPage = lazy(() => import('./pages/auth/LoginPage'))
const RegisterPage = lazy(() => import('./pages/auth/RegisterPage'))
const ForgotPassword = lazy(() => import('./pages/auth/ForgotPassword'))
const ResetPassword = lazy(() => import('./pages/auth/ResetPassword'))
const BookingRequestForm = lazy(() => import('./pages/BookingRequestForm'))
const BookingRequestThankYouPage = lazy(() => import('./pages/BookingRequestThankYouPage'))
const NotFoundPage = lazy(() => import('./pages/errors/NotFoundPage'))
const UnauthorizedPage = lazy(() => import('./pages/errors/UnauthorizedPage'))
const AttendancePage = lazy(() => import('./pages/organizer/AttendancePage'))
const ManageEventsPage = lazy(() => import('./pages/organizer/ManageEventsPage'))
const OrganizerDashboardPage = lazy(() => import('./pages/organizer/OrganizerDashboardPage'))
const RegistrantsPage = lazy(() => import('./pages/organizer/RegistrantsPage'))
const OrganizerBookingRequestsPage = lazy(() => import('./pages/organizer/OrganizerBookingRequestsPage'))
const ManageVotingPage = lazy(() => import('./pages/organizer/ManageVotingPage'))
const EventDetailsPage = lazy(() => import('./pages/student/EventDetailsPage'))
const EventsPage = lazy(() => import('./pages/student/EventsPage'))
const MyRegistrationsPage = lazy(() => import('./pages/student/MyRegistrationsPage'))
const OrganizerApplicationPage = lazy(() => import('./pages/student/OrganizerApplicationPage'))
const StudentDashboardPage = lazy(() => import('./pages/student/StudentDashboardPage'))
const PaymentCallbackPage = lazy(() => import('./pages/student/PaymentCallbackPage'))
const VotingPaymentCallbackPage = lazy(() => import('./pages/student/VotingPaymentCallbackPage'))
const VotingPage = lazy(() => import('./pages/student/VotingPage'))
const ProfilePage = lazy(() => import('./pages/profile/ProfilePage'))
const LandingPage = lazy(() => import('./pages/LandingPage'))
const AboutPage = lazy(() => import('./pages/AboutPage'))
const PrivacyPage = lazy(() => import('./pages/PrivacyPage'))

export default function App() {
  return (
    <>
      <SeoManager />
      <Analytics />
      <StructuredBusinessData />
      <Suspense fallback={<div className="container py-5" role="status">Loading…</div>}>
      <Routes>
      <Route element={<AppLayout />}>
        <Route path="/" element={<LandingPage />} />
        <Route path="/events" element={<EventsPage />} />
        <Route path="/events/:id" element={<EventDetailsPage />} />
        <Route path="/events/:id/voting" element={<VotingPage />} />
        <Route path="/request-organizer" element={<BookingRequestForm />} />
        <Route path="/request-organizer/thank-you" element={<BookingRequestThankYouPage />} />
        <Route path="/about" element={<AboutPage />} />
        <Route path="/privacy" element={<PrivacyPage />} />
      </Route>

      <Route element={<GuestRoute />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/forgot-password" element={<ForgotPassword />} />
        <Route path="/reset-password" element={<ResetPassword />} />
      </Route>

      <Route element={<ProtectedRoute allowedRoles={ROUTE_ROLES.authenticated} />}>
        <Route element={<AppLayout />}>
          <Route path="/profile" element={<ProfilePage />} />
          <Route element={<ProtectedRoute allowedRoles={ROUTE_ROLES.student} />}>
            <Route path="/student" element={<StudentDashboardPage />} />
            <Route path="/student/events" element={<Navigate to="/events" replace />} />
            <Route path="/student/events/:id" element={<EventDetailsPage />} />
            <Route path="/student/registrations" element={<MyRegistrationsPage />} />
            <Route path="/student/organizer-application" element={<OrganizerApplicationPage />} />
            <Route path="/payment/callback" element={<PaymentCallbackPage />} />
            <Route path="/voting/payment/callback" element={<VotingPaymentCallbackPage />} />
          </Route>

          <Route element={<ProtectedRoute allowedRoles={ROUTE_ROLES.organizer} />}>
            <Route path="/organizer" element={<OrganizerDashboardPage />} />
            <Route path="/organizer/events" element={<ManageEventsPage />} />
            <Route path="/organizer/booking-requests" element={<OrganizerBookingRequestsPage />} />
            <Route path="/organizer/events/:id/registrants" element={<RegistrantsPage />} />
            <Route path="/organizer/events/:id/attendance" element={<AttendancePage />} />
            <Route path="/organizer/events/:id/voting" element={<ManageVotingPage />} />
          </Route>

          <Route element={<ProtectedRoute allowedRoles={ROUTE_ROLES.admin} />}>
            <Route path="/admin" element={<AdminDashboardPage />} />
            <Route path="/admin/users" element={<AdminUsersPage />} />
            <Route path="/admin/organizer-applications" element={<AdminOrganizerApplicationsPage />} />
            <Route path="/admin/events" element={<AdminEventsPage />} />
            <Route path="/admin/booking-requests" element={<AdminBookingQueue />} />
            <Route path="/admin/email-outbox" element={<AdminEmailOutboxPage />} />
            <Route path="/admin/audit-logs" element={<AdminAuditLogsPage />} />
            <Route path="/admin/image-cleanup" element={<AdminImageCleanupPage />} />
            <Route path="/admin/events/:id/registrants" element={<RegistrantsPage />} />
            <Route path="/admin/events/:id/voting" element={<ManageVotingPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="/unauthorized" element={<UnauthorizedPage />} />
      <Route path="*" element={<NotFoundPage />} />
      </Routes>
      </Suspense>
    </>
  )
}
