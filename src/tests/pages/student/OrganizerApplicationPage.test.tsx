import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
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

  it('reviews the statement before submitting the unchanged API payload', async () => {
    const user = userEvent.setup()
    let submittedReason = ''
    server.use(
      http.get('http://localhost:5080/api/organizer-applications/mine', () =>
        HttpResponse.json(null)),
      http.post('http://localhost:5080/api/organizer-applications', async ({ request }) => {
        const body = await request.json() as { reason: string }
        submittedReason = body.reason
        return HttpResponse.json({
          id: 'application-new',
          userId: users.student.id,
          userName: users.student.name,
          userEmail: users.student.email,
          reason: body.reason,
          status: 'Pending',
          rejectionReason: null,
          submittedAt: '2026-08-11T12:00:00Z',
          reviewedAt: null,
          reviewedByAdminId: null,
          reviewedByAdminName: null,
        }, { status: 201 })
      }),
    )

    renderWithAuth(<OrganizerApplicationPage />, { user: users.student })

    const statement = 'I want to run practical career workshops for final-year students.'
    await user.type(await screen.findByLabelText('Your plan'), `  ${statement}  `)
    await user.click(screen.getByRole('button', { name: 'Review application' }))

    expect(screen.getByRole('heading', { name: 'Review your application' })).toBeVisible()
    expect(screen.getByText(statement)).toBeVisible()
    expect(submittedReason).toBe('')

    await user.click(screen.getByRole('button', { name: 'Submit application' }))
    expect(await screen.findByText(/awaiting review/i)).toBeVisible()
    expect(submittedReason).toBe(statement)
  })
})
