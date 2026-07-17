import { useCallback, useState } from 'react'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import InputGroup from 'react-bootstrap/InputGroup'
import Row from 'react-bootstrap/Row'
import { api } from '../../api'
import EventCard from '../../components/events/EventCard'
import EmptyState from '../../components/shared/EmptyState'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import PageHeader from '../../components/shared/PageHeader'
import { useApiResource } from '../../hooks/useApiResource'
import { EVENT_CATEGORIES } from '../../types'

export default function EventsPage() {
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('')
  const [date, setDate] = useState('')
  const loadEvents = useCallback(
    () => api.getEvents({ search, category, date }),
    [category, date, search],
  )
  const { data: events, loading, error, reload } = useApiResource(loadEvents)

  const clearFilters = () => {
    setSearch('')
    setCategory('')
    setDate('')
  }

  return (
    <>
      <PageHeader
        eyebrow="Explore campus"
        title="Find your next event"
        description="Search by topic, choose a category, or jump to a specific day."
      />
      <Card className="filter-card border-0 mb-4">
        <Card.Body className="p-3 p-lg-4">
          <Row className="g-3 align-items-end">
            <Col lg={5}>
              <Form.Group controlId="event-search">
                <Form.Label>Search</Form.Label>
                <InputGroup>
                  <InputGroup.Text aria-hidden="true">⌕</InputGroup.Text>
                  <Form.Control
                    value={search}
                    onChange={(event) => setSearch(event.target.value)}
                    placeholder="Title, description, or location"
                  />
                </InputGroup>
              </Form.Group>
            </Col>
            <Col md={5} lg={3}>
              <Form.Group controlId="event-category-filter">
                <Form.Label>Category</Form.Label>
                <Form.Select value={category} onChange={(event) => setCategory(event.target.value)}>
                  <option value="">All categories</option>
                  {EVENT_CATEGORIES.map((item) => (
                    <option key={item}>{item}</option>
                  ))}
                </Form.Select>
              </Form.Group>
            </Col>
            <Col md={5} lg={3}>
              <Form.Group controlId="event-date-filter">
                <Form.Label>Date</Form.Label>
                <Form.Control type="date" value={date} onChange={(event) => setDate(event.target.value)} />
              </Form.Group>
            </Col>
            <Col md={2} lg={1}>
              <Button variant="light" className="w-100" onClick={clearFilters} aria-label="Clear filters">
                Reset
              </Button>
            </Col>
          </Row>
        </Card.Body>
      </Card>
      {loading ? (
        <LoadingState label="Finding events" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void reload()} />
      ) : events?.length ? (
        <>
          <p className="text-secondary mb-3">{events.length} event{events.length === 1 ? '' : 's'} found</p>
          <Row className="g-4">
            {events.map((event) => (
              <Col xl={4} md={6} key={event.id}>
                <EventCard event={event} />
              </Col>
            ))}
          </Row>
        </>
      ) : (
        <EmptyState
          title="No matching events"
          message="Try a different search, category, or date."
          action={<Button onClick={clearFilters}>Clear all filters</Button>}
        />
      )}
    </>
  )
}
