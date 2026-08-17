import { http, HttpResponse } from 'msw'
import { apiEvent, event, paginated, users } from './fixtures'

const apiUrl = 'http://localhost:5080/api'

export const handlers = [
  http.get(`${apiUrl}/auth/csrf`, () => HttpResponse.json({ token: 'test-csrf-token' })),
  http.get(`${apiUrl}/auth/session`, () => new HttpResponse(null, { status: 401 })),
  http.get(`${apiUrl}/events`, () => HttpResponse.json(paginated([apiEvent()]))),
  http.get(`${apiUrl}/events/mine`, () => HttpResponse.json(paginated([apiEvent()]))),
  http.get(`${apiUrl}/events/analytics/mine`, () => HttpResponse.json({
    registrationCount: 24,
    ticketRevenueMinor: 360000,
    currency: 'GHS',
    attendedCount: 18,
    attendanceRate: 75,
    registrationsOverTime: [{ date: '2026-08-15', registrations: 6 }],
  })),
  http.get(`${apiUrl}/coupons`, () => HttpResponse.json([])),
  http.get(`${apiUrl}/events/all`, () => HttpResponse.json(paginated([apiEvent()]))),
  http.get(`${apiUrl}/events/:id`, () => HttpResponse.json(apiEvent())),
  http.get(`${apiUrl}/events/:id/management`, () => HttpResponse.json(apiEvent())),
  http.get(`${apiUrl}/events/:id/registration-status`, () =>
    HttpResponse.json({ isRegistered: false })),
  http.get(`${apiUrl}/students/:id/registrations`, () => HttpResponse.json(paginated([]))),
  http.get(`${apiUrl}/organizer-applications/mine`, () => HttpResponse.json(null)),
  http.get(`${apiUrl}/events/:id/registrants`, () => HttpResponse.json(paginated([
    {
      registrationId: 'registration-1',
      studentId: users.student.id,
      studentName: users.student.name,
      studentEmail: users.student.email,
      registeredAt: '2026-07-15T12:00:00Z',
      attended: false,
    },
  ]))),
  http.put(`${apiUrl}/events/:id/attendance`, () => new HttpResponse(null, { status: 204 })),
  http.get(`${apiUrl}/reports/summary`, () => HttpResponse.json({
    totalEvents: 1,
    totalRegistrations: 1,
    totalUsers: 3,
    overallAttendanceRate: 0,
  })),
  http.get(`${apiUrl}/reports/organizers`, () => HttpResponse.json(paginated([
    {
      organizerId: users.organizer.id,
      organizerName: users.organizer.name,
      eventCount: 1,
      registrationCount: 1,
    },
  ]))),
  http.get(`${apiUrl}/reports/events/${event.id}`, () => HttpResponse.json({
    eventId: event.id,
    eventTitle: event.title,
    registrationCount: 1,
    attendanceCount: 0,
    attendanceRate: 0,
  })),
  http.get(`${apiUrl}/reports/events`, () => HttpResponse.json(paginated([{
    eventId: event.id,
    eventTitle: event.title,
    organizerId: users.organizer.id,
    organizerName: users.organizer.name,
    registrationCount: 1,
    attendanceCount: 0,
    attendanceRate: 0,
  }]))),
  http.get(`${apiUrl}/users`, () => HttpResponse.json(paginated([
    {
      id: users.student.id,
      name: users.student.name,
      email: users.student.email,
      role: 'Student',
      isActive: true,
      createdAt: users.student.joinedAt,
      imageUrl: users.student.imageUrl,
    },
  ]))),
]
