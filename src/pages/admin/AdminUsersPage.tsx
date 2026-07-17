import { useCallback, useMemo, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import { api } from '../../api'
import AdminUserTable from '../../components/admin/AdminUserTable'
import ConfirmModal from '../../components/shared/ConfirmModal'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import PageHeader from '../../components/shared/PageHeader'
import { useApiResource } from '../../hooks/useApiResource'
import type { Role, User } from '../../types'

export default function AdminUsersPage() {
  const [search, setSearch] = useState('')
  const [role, setRole] = useState('')
  const [busyUserId, setBusyUserId] = useState<string | null>(null)
  const [statusTarget, setStatusTarget] = useState<User | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const loadUsers = useCallback(() => api.getUsers(), [])
  const { data: users, loading, error, reload } = useApiResource(loadUsers)

  const filteredUsers = useMemo(() => {
    const query = search.trim().toLowerCase()
    return (users ?? []).filter(
      (user) =>
        (!query || user.name.toLowerCase().includes(query) || user.email.toLowerCase().includes(query)) &&
        (!role || user.role === role),
    )
  }, [role, search, users])

  const changeRole = async (user: User, nextRole: Exclude<Role, 'admin'>) => {
    setBusyUserId(user.id)
    setActionError(null)
    try {
      await api.updateUserRole(user.id, nextRole)
      setNotice(`${user.name} is now ${nextRole === 'organizer' ? 'an Organizer' : 'a Student'}.`)
      await reload()
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'The role could not be updated.')
    } finally {
      setBusyUserId(null)
    }
  }

  const changeStatus = async () => {
    if (!statusTarget) return
    setBusyUserId(statusTarget.id)
    setActionError(null)
    try {
      await api.updateUserStatus(statusTarget.id, false)
      setNotice(`${statusTarget.name} was deactivated.`)
      setStatusTarget(null)
      await reload()
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'The account could not be updated.')
    } finally {
      setBusyUserId(null)
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="Manage users"
        description="Review accounts, assign Organizer access, and control account status."
      />
      {notice && <Alert variant="success" dismissible onClose={() => setNotice(null)}>{notice}</Alert>}
      {actionError && <Alert variant="danger" dismissible onClose={() => setActionError(null)}>{actionError}</Alert>}
      <Card className="filter-card border-0 mb-4">
        <Card.Body>
          <Row className="g-3 align-items-end">
            <Col md={8}>
              <Form.Group controlId="user-search">
                <Form.Label>Search accounts</Form.Label>
                <Form.Control value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Name or email address" />
              </Form.Group>
            </Col>
            <Col md={3}>
              <Form.Group controlId="user-role-filter">
                <Form.Label>Role</Form.Label>
                <Form.Select value={role} onChange={(event) => setRole(event.target.value)}>
                  <option value="">All roles</option>
                  <option value="student">Students</option>
                  <option value="organizer">Organizers</option>
                  <option value="admin">Admins</option>
                </Form.Select>
              </Form.Group>
            </Col>
            <Col md={1}>
              <Button variant="light" className="w-100" onClick={() => { setSearch(''); setRole('') }}>Reset</Button>
            </Col>
          </Row>
        </Card.Body>
      </Card>
      {loading ? (
        <LoadingState label="Loading user accounts" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : filteredUsers.length ? (
        <AdminUserTable
          users={filteredUsers}
          busyUserId={busyUserId}
          onRoleChange={(user, nextRole) => void changeRole(user, nextRole)}
          onStatusChange={setStatusTarget}
        />
      ) : (
        <EmptyState title="No matching accounts" message="Try a different name, email, or role filter." />
      )}
      <ConfirmModal
        show={Boolean(statusTarget)}
        title="Deactivate this account?"
        message={`${statusTarget?.name ?? 'This user'} will no longer be able to sign in.`}
        confirmLabel="Deactivate"
        busy={Boolean(busyUserId)}
        onConfirm={() => void changeStatus()}
        onHide={() => setStatusTarget(null)}
      />
    </>
  )
}
