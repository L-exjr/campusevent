import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Form from 'react-bootstrap/Form'
import Table from 'react-bootstrap/Table'
import type { Role, User } from '../../types'
import { formatDate } from '../../utils/formatters'
import { canManageUserAccount, ROLE_LABELS } from '../../utils/permissions'

interface AdminUserTableProps {
  users: User[]
  busyUserId?: string | null
  onRoleChange: (user: User, role: Exclude<Role, 'admin'>) => void
  onStatusChange: (user: User) => void
}

export default function AdminUserTable({
  users,
  busyUserId,
  onRoleChange,
  onStatusChange,
}: AdminUserTableProps) {
  return (
    <div className="table-shell">
      <Table responsive hover className="align-middle mb-0">
        <thead>
          <tr>
            <th>User</th>
            <th>Role</th>
            <th>Joined</th>
            <th>Status</th>
            <th className="text-end">Account</th>
          </tr>
        </thead>
        <tbody>
          {users.map((user) => {
            const manageable = canManageUserAccount(user.role)
            return (
              <tr key={user.id} className={!user.active ? 'table-muted' : ''}>
                <td>
                  <div className="fw-semibold">{user.name}</div>
                  <small className="text-secondary">{user.email}</small>
                </td>
                <td>
                  {manageable ? (
                    <Form.Select
                      size="sm"
                      aria-label={`Role for ${user.name}`}
                      value={user.role}
                      disabled={busyUserId === user.id}
                      onChange={(event) =>
                        onRoleChange(user, event.target.value as Exclude<Role, 'admin'>)
                      }
                    >
                      <option value="student">Student</option>
                      <option value="organizer">Organizer</option>
                    </Form.Select>
                  ) : (
                    <Badge bg="dark">{ROLE_LABELS[user.role]}</Badge>
                  )}
                </td>
                <td>{formatDate(user.joinedAt)}</td>
                <td>
                  <Badge bg={user.active ? 'success' : 'secondary'}>
                    {user.active ? 'Active' : 'Inactive'}
                  </Badge>
                </td>
                <td className="text-end">
                  <Button
                    size="sm"
                    variant={user.active ? 'outline-danger' : 'light'}
                    disabled={!manageable || !user.active || busyUserId === user.id}
                    onClick={() => onStatusChange(user)}
                  >
                    {user.active ? 'Deactivate' : 'Deactivated'}
                  </Button>
                </td>
              </tr>
            )
          })}
        </tbody>
      </Table>
    </div>
  )
}
