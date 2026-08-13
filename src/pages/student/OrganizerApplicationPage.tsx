import { useCallback, useState, type FormEvent } from 'react'
import Alert from 'react-bootstrap/Alert'
import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import Spinner from 'react-bootstrap/Spinner'
import { api } from '../../api'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import NotificationToast from '../../components/shared/NotificationToast'
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
  const [reviewing, setReviewing] = useState(false)

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

    if (!reviewing) {
      setReason(normalizedReason)
      setReviewing(true)
      return
    }

    setBusy(true)
    try {
      const submitted = await api.submitOrganizerApplication(normalizedReason)
      setData(submitted)
      setReason('')
      setReviewing(false)
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

      <NotificationToast message={notice} onClose={() => setNotice(null)} title="Application submitted" />

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
                <ol className="form-progress" aria-label="Organizer application progress">
                  <li className={!reviewing ? 'is-active' : 'is-complete'} aria-current={!reviewing ? 'step' : undefined}><span>1</span> Your plan</li>
                  <li className={reviewing ? 'is-active' : ''} aria-current={reviewing ? 'step' : undefined}><span>2</span> Review</li>
                </ol>

                {submitError && reviewing && <Alert variant="danger" role="alert">{submitError}</Alert>}
                <Form noValidate aria-busy={busy} onSubmit={(event) => void handleSubmit(event)}>
                  {reviewing ? (
                    <section aria-labelledby="application-review-title">
                      <h2 id="application-review-title" className="h3 mb-2">Review your application</h2>
                      <p className="text-secondary mb-4">The Admin team will use this statement to understand your plans and readiness.</p>
                      <div className="application-review-copy">{reason}</div>
                    </section>
                  ) : (
                    <>
                      <h2 className="h3 mb-2">
                        {application ? 'Submit a revised application' : 'Why do you want to organize events?'}
                      </h2>
                      <p className="text-secondary mb-3">A strong response briefly covers:</p>
                      <ul className="application-prompts">
                        <li>The events you want to create</li>
                        <li>Your relevant experience</li>
                        <li>How students will benefit</li>
                      </ul>
                      <Form.Group controlId="organizer-application-reason">
                        <Form.Label>Your plan</Form.Label>
                        <Form.Control
                          as="textarea"
                          rows={8}
                          required
                          minLength={MINIMUM_REASON_LENGTH}
                          maxLength={MAXIMUM_REASON_LENGTH}
                          isInvalid={Boolean(submitError)}
                          aria-describedby="organizer-application-guidance organizer-application-count"
                          value={reason}
                          placeholder="I would like to organize practical career workshops because…"
                          onChange={(event) => {
                            setReason(event.target.value)
                            setSubmitError(null)
                          }}
                        />
                        <Form.Control.Feedback type="invalid">{submitError}</Form.Control.Feedback>
                        <div className="d-flex justify-content-between gap-3 mt-2">
                          <Form.Text id="organizer-application-guidance">Minimum {MINIMUM_REASON_LENGTH} characters.</Form.Text>
                          <Form.Text id="organizer-application-count" className={reason.length > MAXIMUM_REASON_LENGTH ? 'text-danger' : undefined}>{reason.length}/{MAXIMUM_REASON_LENGTH}</Form.Text>
                        </div>
                      </Form.Group>
                    </>
                  )}
                  <div className="form-actions mt-4">
                    {reviewing && <Button type="button" variant="light" disabled={busy} onClick={() => setReviewing(false)}>Back to edit</Button>}
                    <Button type="submit" size="lg" disabled={busy}>
                      {busy && <Spinner size="sm" className="me-2" aria-hidden="true" />}
                      {busy ? 'Submitting application…' : reviewing ? 'Submit application' : 'Review application'}
                    </Button>
                  </div>
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
