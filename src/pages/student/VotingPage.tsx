import { useCallback, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import { useLocation, useParams } from 'react-router-dom'
import { api } from '../../api'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import LinkButton from '../../components/shared/LinkButton'
import PageHeader from '../../components/shared/PageHeader'
import { useApiResource } from '../../hooks/useApiResource'
import { useAuth } from '../../hooks/useAuth'
import { formatDateTime } from '../../utils/formatters'

export default function VotingPage() {
  const { id = '' } = useParams()
  const { user } = useAuth()
  const location = useLocation()
  const [quantities, setQuantities] = useState<Record<string, number>>({})
  const [busyNominee, setBusyNominee] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const loadCampaign = useCallback(() => api.getVotingCampaign(id), [id])
  const { data: campaign, loading, error, reload } = useApiResource(loadCampaign)

  const castFreeVote = async (categoryId: string, nomineeId: string) => {
    setBusyNominee(nomineeId)
    setActionError(null)
    try {
      await api.castFreeVote(categoryId, nomineeId)
      await reload()
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'Your vote could not be recorded.')
    } finally {
      setBusyNominee(null)
    }
  }

  const buyVotes = async (categoryId: string, nomineeId: string) => {
    setBusyNominee(nomineeId)
    setActionError(null)
    try {
      const payment = await api.initializeVotingPayment(
        categoryId,
        nomineeId,
        quantities[categoryId] ?? 1,
      )
      window.location.assign(payment.authorizationUrl)
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : 'Vote checkout could not be opened.')
      setBusyNominee(null)
    }
  }

  if (loading) return <LoadingState label="Loading event voting" />
  if (error || !campaign) {
    return <ErrorState message={error ?? 'Voting campaign not found.'} onRetry={() => void reload()} />
  }

  const canVote = campaign.status === 'Open' && user?.role !== 'admin'

  return (
    <>
      <PageHeader
        eyebrow="Event voting"
        title={campaign.eventTitle}
        description={`Voting ${campaign.status.toLowerCase()}. Opens ${formatDateTime(campaign.opensAt)} and closes ${formatDateTime(campaign.closesAt)}.`}
        action={<LinkButton to={`/events/${campaign.eventId}`}>Event details</LinkButton>}
      />
      {actionError && <Alert variant="danger">{actionError}</Alert>}
      {!user && campaign.status === 'Open' && (
        <Alert variant="info">
          <LinkButton to="/login" state={{ from: location.pathname }} size="sm" className="me-2">
            Sign in
          </LinkButton>
          Use a Student account to vote.
        </Alert>
      )}
      {!campaign.resultsVisible && (
        <Alert variant="light">Public totals will be revealed when voting closes.</Alert>
      )}
      <div className="d-grid gap-4">
        {campaign.categories.map((category) => (
          <Card key={category.id} className="detail-card border-0">
            <Card.Body className="p-4">
              <div className="d-flex flex-wrap justify-content-between gap-2 mb-3">
                <div>
                  <h2 className="h4 mb-1">{category.name}</h2>
                  {category.description && <p className="text-secondary mb-0">{category.description}</p>}
                </div>
                <Badge bg={category.mode === 'paid' ? 'warning' : 'success'} text="dark" className="align-self-start">
                  {category.mode === 'paid'
                    ? `GHS ${(category.pricePerVoteMinor / 100).toFixed(2)} per vote`
                    : 'One free vote'}
                </Badge>
              </div>
              {category.hasVoted && <Alert variant="success">Your free vote in this category is recorded.</Alert>}
              {category.mode === 'paid' && canVote && (
                <Form.Group className="mb-3" style={{ maxWidth: 180 }}>
                  <Form.Label>Votes to purchase</Form.Label>
                  <Form.Control
                    type="number"
                    min={1}
                    max={100}
                    value={quantities[category.id] ?? 1}
                    onChange={(event) => setQuantities({
                      ...quantities,
                      [category.id]: Math.min(100, Math.max(1, Number(event.target.value) || 1)),
                    })}
                  />
                </Form.Group>
              )}
              <Row className="g-3">
                {category.nominees.map((nominee) => (
                  <Col md={6} xl={4} key={nominee.id}>
                    <Card className="h-100 border">
                      <Card.Body className="d-flex flex-column">
                        <h3 className="h5">{nominee.name}</h3>
                        {nominee.description && <p className="text-secondary flex-grow-1">{nominee.description}</p>}
                        {nominee.voteCount !== null && (
                          <p className="h4 mb-3">{nominee.voteCount.toLocaleString()} votes</p>
                        )}
                        {canVote && category.mode === 'free' && (
                          <Button
                            disabled={category.hasVoted || busyNominee !== null}
                            onClick={() => void castFreeVote(category.id, nominee.id)}
                          >
                            {busyNominee === nominee.id ? 'Recording…' : 'Vote'}
                          </Button>
                        )}
                        {canVote && category.mode === 'paid' && (
                          <Button
                            disabled={busyNominee !== null}
                            onClick={() => void buyVotes(category.id, nominee.id)}
                          >
                            {busyNominee === nominee.id ? 'Opening checkout…' : 'Buy votes'}
                          </Button>
                        )}
                      </Card.Body>
                    </Card>
                  </Col>
                ))}
              </Row>
            </Card.Body>
          </Card>
        ))}
      </div>
    </>
  )
}
