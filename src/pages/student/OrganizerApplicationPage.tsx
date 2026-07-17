import { useCallback, useState, type FormEvent } from 'react'
import Alert from 'react-bootstrap/Alert'
import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import { api } from '../../api'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import PageHeader from '../../components/shared/PageHeader'
import { useApiResource } from '../../hooks/useApiResource'
import { formatDateTime } from '../../utils/formatters'
import type { OrganizerApplicationStatus } from '../../types'

const MINIMUM_REASON_LENGTH = 20
const MAXIMUM_REASON_LENGTH = 2000

const STATUS_PRESENTATION: Record<
  OrganizerApplicationStatus,
  { label: string; badge: string }
> = {
  pending: { label: 'Pending review', badge: 'warning' },
  approved: { label: 'Approved', badge: 'success' },
  rejected: { label: 'Not approved', badge: 'secondary' },
}

export default function OrganizerApplicationPage() {
  const loadApplication = useCallback(() => api.getMyOrganizerApplication(), [])
  const { data: application, loading, error, reload, setData } = useApiResource(loadApplication)
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const normalizedReason = reason.trim()
    setSubmitError(null)
    setNotice(null)

    if (normalizedReason.length < MINIMUM_REASON_LENGTH) {
      setSubmitError(`Please provide at least ${MINIMUM_REASON_LENGTH} characters.`)
      return
    }
    if (normalizedReason.length > MAXIMUM_REASON_LENGTH) {
      setSubmitError(`Please keep your reason within ${MAXIMUM_REASON_LENGTH} characters.`)
      return
    }

    setBusy(true)
    try {
      const submitted = await api.submitOrganizerApplication(normalizedReason)
      setData(submitted)
      setReason('')
      setNotice('Your application has been submitted for Admin review.')
    } catch (caught) {
      setSubmitError(caught instanceof Error ? caught.message : 'Unable to submit your application.')
    } finally {
      setBusy(false)
    }
  }

  if (loading) return <LoadingState label="Loading your organizer application" />
  if (error) return <ErrorState message={error} onRetry={() => void reload()} />

  const canSubmit = !application || application.status === 'rejected'
  const status = application ? STATUS_PRESENTATION[application.status] : null

  return (
    <>
      <PageHeader
        eyebrow="Organizer access"
        title="Help shape campus events"
        description="Tell the Admin team how you plan to create useful, well-run events for the campus community."
      />

      {notice && <Alert variant="success">{notice}</Alert>}

      <Row className="g-4 align-items-start">
        <Col lg={8}>
          {application && (
            <Card className="application-card border-0 mb-4">
              <Card.Body className="p-4 p-md-5">
                <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-4">
                  <div>
                    <p className="eyebrow mb-1">Latest application</p>
                    <h2 className="h4 mb-0">Submitted {formatDateTime(application.submittedAt)}</h2>
                  </div>
                  <Badge bg={status!.badge} className="px-3 py-2">
                    {status!.label}
                  </Badge>
                </div>

                {application.status === 'pending' && (
                  <Alert variant="info">
                    Your application is awaiting review. You cannot submit another application while this one is pending.
                  </Alert>
                )}
                {application.status === 'approved' && (
                  <Alert variant="success">
                    Your application was approved. Sign in again if your Organizer workspace does not appear automatically.
                  </Alert>
                )}
                {application.status === 'rejected' && (
                  <Alert variant="warning">
                    <strong>Admin feedback:</strong>{' '}
                    {application.rejectionReason || 'No additional feedback was provided.'}
                  </Alert>
                )}

                <h3 className="h6 mt-4">Your reason</h3>
                <p className="application-reason text-secondary mb-0">{application.reason}</p>
              </Card.Body>
            </Card>
          )}

          {canSubmit && (
            <Card className="application-card border-0">
              <Card.Body className="p-4 p-md-5">
                <p className="eyebrow mb-2">
                  {application ? 'Apply again' : 'Your application'}
                </p>
                <h2 className="h3 mb-2">
                  {application ? 'Submit a revised application' : 'Why do you want to organize events?'}
                </h2>
                <p className="text-secondary mb-4">
                  Share the kinds of events you want to run, your relevant experience, and how students will benefit.
                </p>

                {submitError && <Alert variant="danger">{submitError}</Alert>}
                <Form onSubmit={(event) => void handleSubmit(event)}>
                  <Form.Group controlId="organizer-application-reason">
                    <Form.Label>Application reason</Form.Label>
                    <Form.Control
                      as="textarea"
                      rows={8}
                      required
                      minLength={MINIMUM_REASON_LENGTH}
                      maxLength={MAXIMUM_REASON_LENGTH}
                      value={reason}
                      placeholder="I would like to organize…"
                      onChange={(event) => {
                        setReason(event.target.value)
                        setSubmitError(null)
                      }}
                    />
                    <div className="d-flex justify-content-between gap-3 mt-2">
                      <Form.Text>Minimum {MINIMUM_REASON_LENGTH} characters.</Form.Text>
                      <Form.Text>{reason.length}/{MAXIMUM_REASON_LENGTH}</Form.Text>
                    </div>
                  </Form.Group>
                  <Button type="submit" size="lg" className="mt-4" disabled={busy}>
                    {busy ? 'Submitting…' : 'Submit application'}
                  </Button>
                </Form>
              </Card.Body>
            </Card>
          )}
        </Col>

        <Col lg={4}>
          <Card className="application-card border-0">
            <Card.Body className="p-4">
              <p className="eyebrow mb-2">Organizer responsibilities</p>
              <h2 className="h5">What access includes</h2>
              <ul className="text-secondary ps-3 mb-0 d-grid gap-2">
                <li>Create and update your own events.</li>
                <li>Review event registrants.</li>
                <li>Record attendance after an event.</li>
                <li>Keep event details accurate and appropriate.</li>
              </ul>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </>
  )
}
