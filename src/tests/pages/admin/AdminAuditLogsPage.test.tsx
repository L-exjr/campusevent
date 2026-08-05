import { screen } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import App from '../../../App'
import { paginated, users } from '../../mocks/fixtures'
import { server } from '../../mocks/server'
import { renderWithAuth } from '../../testUtils'

const apiUrl = 'http://localhost:5080/api'

describe('AdminAuditLogsPage', () => {
  it('shows immutable administrative action records', async () => {
    server.use(
      http.get(`${apiUrl}/admin-audit-logs`, () => HttpResponse.json(paginated([{
        id: 'audit-1',
        actorUserId: users.admin.id,
        actorName: users.admin.name,
        action: 'EventOwnershipTransferred',
        targetType: 'Event',
        targetId: 'event-1',
        detailsJson: '{"previousOrganizerId":"one","newOrganizerId":"two"}',
        createdAt: '2026-08-05T10:00:00Z',
      }]))),
    )

    renderWithAuth(<App />, { user: users.admin, initialEntries: ['/admin/audit-logs'] })

    expect(await screen.findByText('EventOwnershipTransferred')).toBeVisible()
    expect(screen.getAllByText(users.admin.name)).toHaveLength(2)
    expect(screen.getByText('Event · event-1')).toBeVisible()
  })
})
