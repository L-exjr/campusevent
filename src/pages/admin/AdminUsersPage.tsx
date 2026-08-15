import { useCallback, useState } from 'react'
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
import NotificationToast from '../../components/shared/NotificationToast'
import PageHeader from '../../components/shared/PageHeader'
import PaginationControls from '../../components/shared/PaginationControls'
import { useApiResource } from '../../hooks/useApiResource'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import type { Role, User, VerificationStatus } from '../../types'

export default function AdminUsersPage() {
  const [search, setSearch] = useState('')
  const [role, setRole] = useState('')
  const [verificationStatus, setVerificationStatus] = useState('')
  const [accountStatus, setAccountStatus] = useState('')
  const [busyUserId, setBusyUserId] = useState<string | null>(null)
  const [statusTarget, setStatusTarget] = useState<User | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const debouncedSearch = useDebouncedValue(search)
  const loadUsers = useCallback(
    (signal: AbortSignal) => api.getUsers(
      page, 20, debouncedSearch,
      role ? role as Role : undefined,
      verificationStatus ? verificationStatus as VerificationStatus : undefined,
      accountStatus ? accountStatus === 'active' : undefined,
      signal,
    ),
    [accountStatus, debouncedSearch, page, role, verificationStatus],
  )
  const { data: userPage, loading, error, reload } = useApiResource(loadUsers)

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
        description="Review accounts and control account status. Ordinary users can both attend and organize events."
      />
      <NotificationToast message={notice} onClose={() => setNotice(null)} />
      {actionError && <Alert variant="danger" dismissible onClose={() => setActionError(null)}>{actionError}</Alert>}
      <Card className="filter-card border-0 mb-4">
        <Card.Body>
          <Row className="g-3 align-items-end">
            <Col lg={4} md={6}>
              <Form.Group controlId="user-search">
                <Form.Label>Search accounts</Form.Label>
                <Form.Control value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder="Name or email address" />
              </Form.Group>
            </Col>
            <Col lg={2} md={3}>
              <Form.Group controlId="user-role-filter">
                <Form.Label>Role</Form.Label>
                <Form.Select value={role} onChange={(event) => { setRole(event.target.value); setPage(1) }}>
                  <option value="">All roles</option>
                  <option value="student">Ordinary users</option>
                  <option value="admin">Admins</option>
                </Form.Select>
              </Form.Group>
            </Col>
            <Col lg={2} md={3}>
              <Form.Group controlId="user-verification-filter">
                <Form.Label>Verification</Form.Label>
                <Form.Select value={verificationStatus} onChange={(event) => { setVerificationStatus(event.target.value); setPage(1) }}>
                  <option value="">All statuses</option>
                  <option value="unverified">Unverified</option>
                  <option value="pending">Pending</option>
                  <option value="verified">Verified</option>
                </Form.Select>
              </Form.Group>
            </Col>
            <Col lg={2} md={3}>
              <Form.Group controlId="user-account-status-filter">
                <Form.Label>Account</Form.Label>
                <Form.Select value={accountStatus} onChange={(event) => { setAccountStatus(event.target.value); setPage(1) }}>
                  <option value="">All accounts</option>
                  <option value="active">Active</option>
                  <option value="deactivated">Deactivated</option>
                </Form.Select>
              </Form.Group>
            </Col>
            <Col lg={2} md={3}>
              <Button variant="light" className="w-100 text-nowrap" onClick={() => { setSearch(''); setRole(''); setVerificationStatus(''); setAccountStatus(''); setPage(1) }}>Reset</Button>
            </Col>
          </Row>
        </Card.Body>
      </Card>
      {loading ? (
        <LoadingState label="Loading user accounts" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : userPage?.items.length ? (
        <>
        <AdminUserTable
          users={userPage.items}
          busyUserId={busyUserId}
          onStatusChange={setStatusTarget}
        />
        <PaginationControls {...userPage} label="accounts" onPageChange={setPage} />
        </>
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
