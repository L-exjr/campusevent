import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import { useParams } from 'react-router-dom'
import { api } from '../api'
import EventCard from '../components/events/EventCard'
import ErrorState from '../components/shared/ErrorState'
import LoadingState from '../components/shared/LoadingState'
import { useApiResource } from '../hooks/useApiResource'
import LinkButton from '../components/shared/LinkButton'

export default function OrganizerDetailPage() {
  const { id = '' } = useParams(); const { data, loading, error, reload } = useApiResource(() => api.getOrganizer(id))
  if (loading) return <LoadingState label="Loading organizer" />
  if (error || !data) return <ErrorState message={error || 'Organizer not found.'} onRetry={() => void reload()} />
  const socials = [['Instagram', data.instagramUrl], ['X / Twitter', data.twitterUrl], ['Facebook', data.facebookUrl], ['Website', data.websiteUrl]].filter(([, url]) => url)
  return <><Card className="border-0 overflow-hidden mb-4">{data.bannerUrl && <img src={data.bannerUrl} alt="" className="organizer-detail__banner object-fit-cover" />}<Card.Body className="p-4 p-lg-5"><p className="eyebrow">Organizer profile</p><h1>{data.name}</h1><p className="lead">{data.bio || 'Campus Events organizer'}</p><div className="d-flex flex-wrap gap-2 mb-4">{data.specialties.map(item => <Badge bg="light" text="dark" key={item}>{item}</Badge>)}</div><div className="d-flex flex-wrap gap-2">{socials.map(([label, url]) => <Button key={label} as="a" href={url!} target="_blank" rel="noreferrer" variant="light">{label}</Button>)}<LinkButton to={`/request-organizer?organizerId=${data.id}`}>Request {data.name}</LinkButton></div></Card.Body></Card><h2 className="h3 mb-3">Past published events</h2><div className="event-grid">{data.events.map(event => <EventCard event={event} key={event.id} />)}</div></>
}
