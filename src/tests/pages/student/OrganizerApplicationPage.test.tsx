import { screen } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import OrganizerApplicationPage from '../../../pages/student/OrganizerApplicationPage'
import { users } from '../../mocks/fixtures'
import { server } from '../../mocks/server'
import { renderWithAuth } from '../../testUtils'

describe('OrganizerApplicationPage', () => {
  it('prevents another submission while an application is pending', async () => {
    let submissionCount = 0
    server.use(
      http.get('http://localhost:5080/api/organizer-applications/mine', () =>
        HttpResponse.json({
          id: 'application-1',
          userId: users.student.id,
          userName: users.student.name,
          userEmail: users.student.email,
          reason: 'I want to create practical workshops for students.',
          status: 'Pending',
          rejectionReason: null,
          submittedAt: '2026-07-20T12:00:00Z',
          reviewedAt: null,
          reviewedByAdminId: null,
          reviewedByAdminName: null,
        })),
      http.post('http://localhost:5080/api/organizer-applications', () => {
        submissionCount += 1
        return HttpResponse.json({}, { status: 201 })
      }),
    )

    renderWithAuth(<OrganizerApplicationPage />, { user: users.student })

    expect(await screen.findByText(/cannot submit another application while this one is pending/i))
      .toBeVisible()
    expect(screen.queryByLabelText('Application reason')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Submit application' })).not.toBeInTheDocument()
    expect(submissionCount).toBe(0)
  })
})
