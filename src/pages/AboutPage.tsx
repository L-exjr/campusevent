import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Row from 'react-bootstrap/Row'
import LinkButton from '../components/shared/LinkButton'
import PageHeader from '../components/shared/PageHeader'

export default function AboutPage() {
  return (
    <>
      <PageHeader
        eyebrow="About Campus Events"
        title="One place for the people who make campus happen."
        description="Campus Events connects discovery, registration, payments, attendance, voting, and organizer operations without losing sight of the student experience."
      />
      <Row className="g-4">
        <Col md={4}><Card className="detail-card border-0 h-100"><Card.Body className="p-4"><h2 className="h4">For students</h2><p className="text-secondary">Find events, reserve places, carry signed tickets, vote, and collect attendance-backed certificates.</p></Card.Body></Card></Col>
        <Col md={4}><Card className="detail-card border-0 h-100"><Card.Body className="p-4"><h2 className="h4">For organizers</h2><p className="text-secondary">Publish events, manage registrations, scan tickets, confirm attendance, and run fair voting campaigns.</p></Card.Body></Card></Col>
        <Col md={4}><Card className="detail-card border-0 h-100"><Card.Body className="p-4"><h2 className="h4">For administrators</h2><p className="text-secondary">Review access, oversee operations, and use reporting and audit records to support the community.</p></Card.Body></Card></Col>
      </Row>
      <section className="landing-cta mt-5">
        <div><p className="eyebrow mb-2">Start somewhere useful</p><h2>Explore an event or tell us what you want to organize.</h2></div>
        <div className="d-flex flex-wrap gap-2"><LinkButton to="/events" variant="light">Explore events</LinkButton><LinkButton to="/request-organizer" variant="outline-light">Request an Organizer</LinkButton></div>
      </section>
    </>
  )
}
