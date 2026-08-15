import { useCallback, useState } from 'react'
import Badge from 'react-bootstrap/Badge'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import { Link } from 'react-router-dom'
import LinkButton from '../components/shared/LinkButton'
import { api } from '../api'
import EmptyState from '../components/shared/EmptyState'
import ErrorState from '../components/shared/ErrorState'
import LoadingState from '../components/shared/LoadingState'
import PaginationControls from '../components/shared/PaginationControls'
import { useApiResource } from '../hooks/useApiResource'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { EVENT_CATEGORIES } from '../types'

export default function OrganizerDirectoryPage() {
  const [search, setSearch] = useState(''); const [category, setCategory] = useState(''); const [page, setPage] = useState(1)
  const debounced = useDebouncedValue(search, 300)
  const load = useCallback((signal: AbortSignal) => api.getOrganizers(debounced, category, page, 12, signal), [debounced, category, page])
  const { data, loading, error, reload } = useApiResource(load)
  return <>
    <header className="page-header"><p className="eyebrow">Public directory</p><h1>Find an Organizer</h1><p>Browse opted-in Campus Events organizers by name and specialty.</p></header>
    <Card className="border-0 mb-4"><Card.Body className="p-4"><Row className="g-3">
      <Col md={7}><Form.Label htmlFor="organizer-search">Search by name</Form.Label><Form.Control id="organizer-search" value={search} onChange={event => { setSearch(event.target.value); setPage(1) }} placeholder="Organizer name" /></Col>
      <Col md={5}><Form.Label htmlFor="organizer-category">Specialty</Form.Label><Form.Select id="organizer-category" value={category} onChange={event => { setCategory(event.target.value); setPage(1) }}><option value="">All specialties</option>{EVENT_CATEGORIES.map(item => <option key={item}>{item}</option>)}</Form.Select></Col>
    </Row></Card.Body></Card>
    <Card className="border-0 mb-4"><Card.Body className="p-4 d-flex flex-column flex-md-row justify-content-between gap-3 align-items-md-center"><div><h2 className="h4 mb-1">No specific Organizer in mind?</h2><p className="text-secondary mb-0">Send your event details to the coordination team for triage.</p></div><LinkButton to="/request-organizer">Submit without a preference</LinkButton></Card.Body></Card>
    {loading ? <LoadingState label="Loading organizers" /> : error ? <ErrorState message={error} onRetry={() => void reload()} /> : data?.items.length ? <>
      <Row className="g-4">{data.items.map(organizer => <Col md={6} lg={4} key={organizer.id}><Card className="border-0 h-100 organizer-card overflow-hidden">
        {organizer.bannerUrl && <Card.Img variant="top" src={organizer.bannerUrl} alt="" className="organizer-card__banner object-fit-cover" />}
        <Card.Body className="p-4"><h2 className="h4"><Link className="stretched-link text-decoration-none" to={`/organizers/${organizer.id}`}>{organizer.name}</Link> {organizer.verificationStatus === 'verified' && <Badge bg="primary" aria-label="Verified organizer">Verified</Badge>}</h2><p className="text-secondary">{organizer.bio || 'View this organizer’s profile and published events.'}</p><div className="d-flex flex-wrap gap-2">{organizer.specialties.map(item => <Badge bg="light" text="dark" key={item}>{item}</Badge>)}</div></Card.Body>
      </Card></Col>)}</Row><PaginationControls {...data} label="organizers" onPageChange={setPage} /></> : <EmptyState title="No organizers found" message="Try a different name or specialty." />}
  </>
}
