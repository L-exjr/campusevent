import { Navigate, Route, Routes } from 'react-router-dom'
import AppLayout from './components/layout/AppLayout'
import GuestRoute from './components/routing/GuestRoute'
import ProtectedRoute from './components/routing/ProtectedRoute'
import AdminDashboardPage from './pages/admin/AdminDashboardPage'
import AdminEventsPage from './pages/admin/AdminEventsPage'
import AdminOrganizerApplicationsPage from './pages/admin/AdminOrganizerApplicationsPage'
import AdminUsersPage from './pages/admin/AdminUsersPage'
import AdminEmailOutboxPage from './pages/admin/AdminEmailOutboxPage'
import AdminAuditLogsPage from './pages/admin/AdminAuditLogsPage'
import AdminImageCleanupPage from './pages/admin/AdminImageCleanupPage'
import LoginPage from './pages/auth/LoginPage'
import RegisterPage from './pages/auth/RegisterPage'
import ForgotPassword from './pages/auth/ForgotPassword'
import ResetPassword from './pages/auth/ResetPassword'
import BookingRequestForm from './pages/BookingRequestForm'
import AdminBookingQueue from './pages/admin/AdminBookingQueue'
import NotFoundPage from './pages/errors/NotFoundPage'
import UnauthorizedPage from './pages/errors/UnauthorizedPage'
import AttendancePage from './pages/organizer/AttendancePage'
import ManageEventsPage from './pages/organizer/ManageEventsPage'
import OrganizerDashboardPage from './pages/organizer/OrganizerDashboardPage'
import RegistrantsPage from './pages/organizer/RegistrantsPage'
import OrganizerBookingRequestsPage from './pages/organizer/OrganizerBookingRequestsPage'
import EventDetailsPage from './pages/student/EventDetailsPage'
import EventsPage from './pages/student/EventsPage'
import MyRegistrationsPage from './pages/student/MyRegistrationsPage'
import OrganizerApplicationPage from './pages/student/OrganizerApplicationPage'
import StudentDashboardPage from './pages/student/StudentDashboardPage'
import PaymentCallbackPage from './pages/student/PaymentCallbackPage'
import VotingPaymentCallbackPage from './pages/student/VotingPaymentCallbackPage'
import VotingPage from './pages/student/VotingPage'
import ManageVotingPage from './pages/organizer/ManageVotingPage'
import ProfilePage from './pages/profile/ProfilePage'
import LandingPage from './pages/LandingPage'
import { ROUTE_ROLES } from './utils/permissions'
import AboutPage from './pages/AboutPage'
import BookingRequestThankYouPage from './pages/BookingRequestThankYouPage'
import PrivacyPage from './pages/PrivacyPage'
import SeoManager from './components/seo/SeoManager'
import Analytics from './components/seo/Analytics'
import StructuredBusinessData from './components/seo/StructuredBusinessData'

export default function App() {
  return (
    <>
      <SeoManager />
      <Analytics />
      <StructuredBusinessData />
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
    </>
  )
}
