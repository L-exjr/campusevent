import { useCallback, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Modal from 'react-bootstrap/Modal'
import Row from 'react-bootstrap/Row'
import { api } from '../../api'
import ConfirmModal from '../../components/shared/ConfirmModal'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import NotificationToast from '../../components/shared/NotificationToast'
import PageHeader from '../../components/shared/PageHeader'
import PaginationControls from '../../components/shared/PaginationControls'
import { useApiResource } from '../../hooks/useApiResource'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { formatDateTime, getInitials } from '../../utils/formatters'
import type { OrganizerApplication } from '../../types'

const MAXIMUM_REJECTION_REASON_LENGTH = 1000

export default function AdminOrganizerApplicationsPage() {
  const [search, setSearch] = useState('')
  const [approvalTarget, setApprovalTarget] = useState<OrganizerApplication | null>(null)
  const [rejectionTarget, setRejectionTarget] = useState<OrganizerApplication | null>(null)
  const [rejectionReason, setRejectionReason] = useState('')
  const [busyApplicationId, setBusyApplicationId] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const debouncedSearch = useDebouncedValue(search)
  const loadApplications = useCallback(
    (signal: AbortSignal) => api.getPendingOrganizerApplications(page, 20, debouncedSearch, signal),
    [debouncedSearch, page],
  )
  const { data: applicationPage, loading, error, reload, setData } = useApiResource(loadApplications)
  const applications = applicationPage?.items

  const removeFromQueue = (applicationId: string) => {
    setData((current) => current ? {
      ...current,
      items: current.items.filter((application) => application.id !== applicationId),
      totalCount: Math.max(current.totalCount - 1, 0),
    } : current)
  }

  const approveApplication = async () => {
    if (!approvalTarget) return
    const target = approvalTarget
    setBusyApplicationId(target.id)
    setActionError(null)
    setNotice(null)
    try {
      await api.approveOrganizerApplication(target.id)
      removeFromQueue(target.id)
      setNotice(`${target.userName} is now verified.`)
      setApprovalTarget(null)
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'The application could not be approved.')
      setApprovalTarget(null)
    } finally {
      setBusyApplicationId(null)
    }
  }

  const rejectApplication = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!rejectionTarget) return
    const target = rejectionTarget
    const normalizedReason = rejectionReason.trim()
    if (normalizedReason.length > MAXIMUM_REJECTION_REASON_LENGTH) {
      setActionError(`Feedback cannot exceed ${MAXIMUM_REJECTION_REASON_LENGTH} characters.`)
      return
    }

    setBusyApplicationId(target.id)
    setActionError(null)
    setNotice(null)
    try {
      await api.rejectOrganizerApplication(target.id, normalizedReason)
      removeFromQueue(target.id)
      setNotice(`${target.userName}'s application was rejected.`)
      setRejectionTarget(null)
      setRejectionReason('')
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'The application could not be rejected.')
      setRejectionTarget(null)
    } finally {
      setBusyApplicationId(null)
    }
  }

  const openRejection = (application: OrganizerApplication) => {
    setActionError(null)
    setRejectionReason('')
    setRejectionTarget(application)
  }

  return (
    <>
      <PageHeader
        eyebrow="Organizer trust"
        title="Pending verification requests"
        description="Review organizer identity requests. Decisions only change verification status, never roles or event permissions."
        action={
          applications ? (
            <Badge bg="warning" text="dark" pill className="pending-count-badge">
              {applicationPage?.totalCount ?? applications.length} pending
            </Badge>
          ) : undefined
        }
      />

      <NotificationToast message={notice} onClose={() => setNotice(null)} />
      {actionError && <Alert variant="danger" dismissible onClose={() => setActionError(null)}>{actionError}</Alert>}

      {loading ? (
        <LoadingState label="Loading pending applications" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : applications?.length ? (
        <>
          <Card className="filter-card border-0 mb-4">
            <Card.Body>
              <Row className="g-3 align-items-end">
                <Col md={10}>
                  <Form.Group controlId="application-search">
                    <Form.Label>Search pending applications</Form.Label>
                    <Form.Control
                      value={search}
                      placeholder="Applicant name, email, or application reason"
                      onChange={(event) => { setSearch(event.target.value); setPage(1) }}
                    />
                  </Form.Group>
                </Col>
                <Col md={2}>
                  <Button
                    variant="light"
                    className="w-100 text-nowrap"
                    disabled={!search}
                    onClick={() => { setSearch(''); setPage(1) }}
                  >
                    Reset
                  </Button>
                </Col>
              </Row>
            </Card.Body>
          </Card>

          {applications.length ? (
            <>
            <div className="d-grid gap-3">
              {applications.map((application) => (
                <Card key={application.id} className="admin-application-card border-0">
                  <Card.Body className="p-4">
                    <div className="d-flex flex-column flex-md-row justify-content-between gap-3 mb-4">
                      <div className="d-flex align-items-center gap-3">
                        <div className="avatar avatar--large" aria-hidden="true">
                          {getInitials(application.userName)}
                        </div>
                        <div>
                          <h2 className="h5 mb-1">{application.userName}</h2>
                          <a href={`mailto:${application.userEmail}`} className="text-secondary">
                            {application.userEmail}
                          </a>
                        </div>
                      </div>
                      <div className="text-md-end">
                        <Badge bg="warning" text="dark" className="mb-2">Pending review</Badge>
                        <div className="small text-secondary">
                          Submitted {formatDateTime(application.submittedAt)}
                        </div>
                      </div>
                    </div>
                    <h3 className="h6">Application reason</h3>
                    <p className="application-reason text-secondary mb-0">{application.reason}</p>
                    <div className="d-flex flex-wrap justify-content-end gap-2 border-top mt-4 pt-3">
                      <Button
                        variant="outline-danger"
                        disabled={Boolean(busyApplicationId)}
                        aria-label={`Reject ${application.userName}'s application`}
                        onClick={() => openRejection(application)}
                      >
                        Reject
                      </Button>
                      <Button
                        variant="success"
                        disabled={Boolean(busyApplicationId)}
                        aria-label={`Approve ${application.userName}'s application`}
                        onClick={() => {
                          setActionError(null)
                          setApprovalTarget(application)
                        }}
                      >
                        Approve verification
                      </Button>
                    </div>
                  </Card.Body>
                </Card>
              ))}
            </div>
            {applicationPage && (
              <PaginationControls {...applicationPage} label="applications" onPageChange={setPage} />
            )}
            </>
          ) : (
            <EmptyState
              title="No matching applications"
              message="Try a different applicant name, email address, or keyword."
            />
          )}
        </>
      ) : (
        <EmptyState
          title="No pending applications"
          message="New Student applications will appear here when they are submitted."
        />
      )}

      <ConfirmModal
        show={Boolean(approvalTarget)}
        title="Approve this application?"
        message={`${approvalTarget?.userName ?? 'This user'} will receive a public verification badge. Their role and event permissions will not change.`}
        confirmLabel="Approve verification"
        confirmVariant="success"
        busy={busyApplicationId === approvalTarget?.id}
        onConfirm={() => void approveApplication()}
        onHide={() => setApprovalTarget(null)}
      />

      <Modal
        show={Boolean(rejectionTarget)}
        onHide={() => {
          if (busyApplicationId) return
          setRejectionTarget(null)
          setRejectionReason('')
        }}
        backdrop={busyApplicationId ? 'static' : true}
        keyboard={!busyApplicationId}
        centered
      >
        <Form onSubmit={(event) => void rejectApplication(event)}>
          <Modal.Header closeButton={!busyApplicationId}>
            <Modal.Title as="h2" className="h5">Reject application?</Modal.Title>
          </Modal.Header>
          <Modal.Body>
            <p className="text-secondary">
              {rejectionTarget?.userName ?? 'This Student'} will remain a Student and may submit a revised application later.
            </p>
            <Form.Group controlId="rejection-reason">
              <Form.Label>Feedback for the Student <span className="text-secondary">(optional)</span></Form.Label>
              <Form.Control
                as="textarea"
                rows={4}
                maxLength={MAXIMUM_REJECTION_REASON_LENGTH}
                value={rejectionReason}
                placeholder="Explain what could make a future application stronger."
                onChange={(event) => setRejectionReason(event.target.value)}
              />
              <Form.Text className="d-block text-end">
                {rejectionReason.length}/{MAXIMUM_REJECTION_REASON_LENGTH}
              </Form.Text>
            </Form.Group>
          </Modal.Body>
          <Modal.Footer>
            <Button
              variant="light"
              disabled={Boolean(busyApplicationId)}
              onClick={() => {
                setRejectionTarget(null)
                setRejectionReason('')
              }}
            >
              Cancel
            </Button>
            <Button type="submit" variant="danger" disabled={Boolean(busyApplicationId)}>
              {busyApplicationId ? 'Rejecting…' : 'Reject application'}
            </Button>
          </Modal.Footer>
        </Form>
      </Modal>
    </>
  )
}
