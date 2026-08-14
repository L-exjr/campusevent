import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Table from 'react-bootstrap/Table'
import type { User } from '../../types'
import { formatDate } from '../../utils/formatters'
import { canManageUserAccount, ROLE_LABELS } from '../../utils/permissions'

interface AdminUserTableProps {
  users: User[]
  busyUserId?: string | null
  onStatusChange: (user: User) => void
}

export default function AdminUserTable({
  users,
  busyUserId,
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
                  <Badge bg={user.role === 'admin' ? 'dark' : 'light'} text={user.role === 'admin' ? undefined : 'dark'}>{ROLE_LABELS[user.role]}</Badge>
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
