import { Navigate, Route, Routes } from 'react-router-dom'
import AppLayout from './components/layout/AppLayout'
import GuestRoute from './components/routing/GuestRoute'
import ProtectedRoute from './components/routing/ProtectedRoute'
import AdminDashboardPage from './pages/admin/AdminDashboardPage'
import AdminEventsPage from './pages/admin/AdminEventsPage'
import AdminOrganizerApplicationsPage from './pages/admin/AdminOrganizerApplicationsPage'
import AdminUsersPage from './pages/admin/AdminUsersPage'
import LoginPage from './pages/auth/LoginPage'
import RegisterPage from './pages/auth/RegisterPage'
import NotFoundPage from './pages/errors/NotFoundPage'
import UnauthorizedPage from './pages/errors/UnauthorizedPage'
import AttendancePage from './pages/organizer/AttendancePage'
import ManageEventsPage from './pages/organizer/ManageEventsPage'
import OrganizerDashboardPage from './pages/organizer/OrganizerDashboardPage'
import RegistrantsPage from './pages/organizer/RegistrantsPage'
import EventDetailsPage from './pages/student/EventDetailsPage'
import EventsPage from './pages/student/EventsPage'
import MyRegistrationsPage from './pages/student/MyRegistrationsPage'
import OrganizerApplicationPage from './pages/student/OrganizerApplicationPage'
import StudentDashboardPage from './pages/student/StudentDashboardPage'
import { ROUTE_ROLES } from './utils/permissions'

export default function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route path="/events" element={<EventsPage />} />
        <Route path="/events/:id" element={<EventDetailsPage />} />
      </Route>

      <Route element={<GuestRoute />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>

      <Route element={<ProtectedRoute allowedRoles={ROUTE_ROLES.authenticated} />}>
        <Route element={<AppLayout />}>
          <Route element={<ProtectedRoute allowedRoles={ROUTE_ROLES.student} />}>
            <Route path="/student" element={<StudentDashboardPage />} />
            <Route path="/student/events" element={<Navigate to="/events" replace />} />
            <Route path="/student/events/:id" element={<EventDetailsPage />} />
            <Route path="/student/registrations" element={<MyRegistrationsPage />} />
            <Route path="/student/organizer-application" element={<OrganizerApplicationPage />} />
          </Route>

          <Route element={<ProtectedRoute allowedRoles={ROUTE_ROLES.organizer} />}>
            <Route path="/organizer" element={<OrganizerDashboardPage />} />
            <Route path="/organizer/events" element={<ManageEventsPage />} />
            <Route path="/organizer/events/:id/registrants" element={<RegistrantsPage />} />
            <Route path="/organizer/events/:id/attendance" element={<AttendancePage />} />
          </Route>

          <Route element={<ProtectedRoute allowedRoles={ROUTE_ROLES.admin} />}>
            <Route path="/admin" element={<AdminDashboardPage />} />
            <Route path="/admin/users" element={<AdminUsersPage />} />
            <Route path="/admin/organizer-applications" element={<AdminOrganizerApplicationsPage />} />
            <Route path="/admin/events" element={<AdminEventsPage />} />
            <Route path="/admin/events/:id/registrants" element={<RegistrantsPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="/" element={<Navigate to="/events" replace />} />
      <Route path="/unauthorized" element={<UnauthorizedPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
