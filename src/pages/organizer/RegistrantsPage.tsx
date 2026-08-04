import { useCallback, useEffect, useMemo, useState } from 'react'
import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import { Navigate, useParams } from 'react-router-dom'
import { api } from '../../api'
import RegistrantTable from '../../components/organizer/RegistrantTable'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import LinkButton from '../../components/shared/LinkButton'
import PageHeader from '../../components/shared/PageHeader'
import { useApiResource } from '../../hooks/useApiResource'
import { useAuth } from '../../hooks/useAuth'
import { canManageEvent } from '../../utils/permissions'

export default function RegistrantsPage() {
  const { id = '' } = useParams()
  const { user } = useAuth()
  const [search, setSearch] = useState('')
  const [attendance, setAttendance] = useState('')
  const loadData = useCallback(
    async () => {
      const event = await api.getManagementEvent(id)
      if (!user || !canManageEvent(user, event)) {
        return { event, registrants: null }
      }
      return { event, registrants: await api.getEventRegistrants(id) }
    },
    [id, user],
  )
  const { data, loading, error, reload } = useApiResource(loadData)
  const registrants = useMemo(() => data?.registrants ?? [], [data?.registrants])
  const filteredRegistrants = useMemo(() => {
    const query = search.trim().toLowerCase()
    return registrants.filter((registrant) => {
      const matchesSearch =
        !query ||
        registrant.name.toLowerCase().includes(query) ||
        registrant.email.toLowerCase().includes(query)
      const matchesAttendance =
        !attendance ||
        (attendance === 'attended' ? registrant.attended : !registrant.attended)
      return matchesSearch && matchesAttendance
    })
  }, [attendance, registrants, search])

  useEffect(() => {
    if (!data?.event.title) return
    document.title = `${data.event.title} registrants | Campus Events`
    return () => {
      document.title = 'Campus Events'
    }
  }, [data?.event.title])

  const clearFilters = () => {
    setSearch('')
    setAttendance('')
  }

  if (loading) return <LoadingState label="Loading registrants" />
  if (error || !data) return <ErrorState message={error ?? 'No data returned.'} onRetry={() => void reload()} />
  if (!user || !canManageEvent(user, data.event) || !data.registrants) {
    return <Navigate to="/unauthorized" replace />
  }

  const attendedCount = registrants.filter((registrant) => registrant.attended).length
  const backPath = user.role === 'admin' ? '/admin/events' : '/organizer/events'

  return (
    <>
      <LinkButton to={backPath} variant="link" className="px-0 text-decoration-none mb-2">← Back to events</LinkButton>
      <PageHeader
        eyebrow="Registrant list"
        title={data.event.title}
        description="Review everyone currently registered for this event."
        action={(
          <div className="d-flex flex-wrap gap-2">
            <Badge bg="primary" className="summary-badge">{registrants.length} registered</Badge>
            <Badge bg="success" className="summary-badge">{attendedCount} attended</Badge>
          </div>
        )}
      />
      {registrants.length ? (
        <>
          <Card className="filter-card border-0 mb-4">
            <Card.Body>
              <Row className="g-3 align-items-end">
                <Col md={7}>
                  <Form.Group controlId="registrant-search">
                    <Form.Label>Search registrants</Form.Label>
                    <Form.Control
                      value={search}
                      onChange={(event) => setSearch(event.target.value)}
                      placeholder="Name or email address"
                    />
                  </Form.Group>
                </Col>
                <Col md={4}>
                  <Form.Group controlId="registrant-attendance-filter">
                    <Form.Label>Attendance status</Form.Label>
                    <Form.Select
                      value={attendance}
                      onChange={(event) => setAttendance(event.target.value)}
                    >
                      <option value="">All registrants</option>
                      <option value="attended">Attended</option>
                      <option value="not-marked">Not marked</option>
                    </Form.Select>
                  </Form.Group>
                </Col>
                <Col md={1}>
                  <Button
                    variant="light"
                    className="w-100"
                    disabled={!search && !attendance}
                    onClick={clearFilters}
                  >
                    Reset
                  </Button>
                </Col>
              </Row>
            </Card.Body>
          </Card>
          {filteredRegistrants.length ? (
            <RegistrantTable registrants={filteredRegistrants} />
          ) : (
            <EmptyState
              title="No matching registrants"
              message="Try a different name, email address, or attendance status."
              action={<Button onClick={clearFilters}>Clear all filters</Button>}
            />
          )}
        </>
      ) : (
        <EmptyState title="No registrations yet" message="Student names will appear here when they register." />
      )}
    </>
  )
}
