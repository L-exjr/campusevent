import Badge from 'react-bootstrap/Badge'
import Col from 'react-bootstrap/Col'
import Row from 'react-bootstrap/Row'
import { useAuth } from '../hooks/useAuth'
import LinkButton from '../components/shared/LinkButton'
import { getHomeForRole } from '../utils/permissions'

const workflows = [
  {
    number: '01',
    title: 'Discover',
    copy: 'Find events across campus and see the details that matter before you commit.',
  },
  {
    number: '02',
    title: 'Reserve',
    copy: 'Register in a few clicks, then keep every upcoming event in one clear view.',
  },
  {
    number: '03',
    title: 'Show up',
    copy: 'Get timely updates while organizers manage attendance and the guest experience.',
  },
]

const capabilities = [
  ['For students', 'Browse, register, and keep track of the events that make campus feel connected.'],
  ['For organizers', 'Create polished listings, follow registrations, and take attendance from one workspace.'],
  ['For administrators', 'Support your community with clear approvals, oversight, and reporting.'],
]

export default function LandingPage() {
  const { user } = useAuth()

  return (
    <div className="landing-page">
      <section className="landing-hero" aria-labelledby="landing-title">
        <div className="landing-hero__copy">
          <Badge className="landing-kicker">Made for campus life</Badge>
          <h1 id="landing-title">Every campus moment, within reach.</h1>
          <p>
            Discover what is happening, reserve your place, and give every event a smoother
            path from idea to full house.
          </p>
          <div className="d-flex flex-column flex-sm-row gap-3">
            <LinkButton to="/events" size="lg">
              Explore events
            </LinkButton>
            <LinkButton
              to={user ? getHomeForRole(user.role) : '/register'}
              variant="outline-light"
              size="lg"
            >
              {user ? 'Go to dashboard' : 'Join the community'}
            </LinkButton>
          </div>
        </div>

        <div className="landing-hero__visual" aria-label="A preview of the campus events experience">
          <div className="landing-orbit landing-orbit--one" />
          <div className="landing-orbit landing-orbit--two" />
          <article className="landing-event-preview">
            <div className="landing-event-preview__image">
              <span className="landing-event-preview__tag">Featured this week</span>
              <span className="landing-event-preview__glyph" aria-hidden="true">✦</span>
            </div>
            <div className="landing-event-preview__body">
              <div className="landing-date">
                <span>SEP</span>
                <strong>18</strong>
              </div>
              <div>
                <span className="landing-event-preview__type">Community &amp; culture</span>
                <h2>Campus Night Market</h2>
                <p className="mb-0">Central Quad · 6:30 PM</p>
              </div>
            </div>
          </article>
          <div className="landing-attendance-note">
            <span className="landing-attendance-note__icon" aria-hidden="true">✓</span>
            <div><strong>You’re on the list</strong><small>Registration confirmed</small></div>
          </div>
        </div>
      </section>

      <section className="landing-proof" aria-label="Platform highlights">
        <span>One campus</span>
        <span className="landing-proof__dot" />
        <span>Every event</span>
        <span className="landing-proof__dot" />
        <span>A place to belong</span>
      </section>

      <section className="landing-section" aria-labelledby="how-it-works">
        <p className="eyebrow mb-2">How it works</p>
        <div className="landing-section__heading">
          <h2 id="how-it-works">From “what’s on?” to “see you there.”</h2>
          <p>A simple path for students, backed by the tools organizers need.</p>
        </div>
        <Row className="g-4 mt-2">
          {workflows.map((item) => (
            <Col md={4} key={item.number}>
              <article className="landing-step h-100">
                <span>{item.number}</span>
                <h3>{item.title}</h3>
                <p>{item.copy}</p>
              </article>
            </Col>
          ))}
        </Row>
      </section>

      <section className="landing-roles" aria-labelledby="built-for-everyone">
        <div>
          <p className="eyebrow mb-2">One shared space</p>
          <h2 id="built-for-everyone">Built for everyone who makes campus happen.</h2>
        </div>
        <div className="landing-roles__list">
          {capabilities.map(([title, copy], index) => (
            <article key={title}>
              <span aria-hidden="true">{String(index + 1).padStart(2, '0')}</span>
              <div><h3>{title}</h3><p>{copy}</p></div>
            </article>
          ))}
        </div>
      </section>

      <section className="landing-cta">
        <div>
          <p className="eyebrow mb-2">Your next event starts here</p>
          <h2>There is more happening than you think.</h2>
        </div>
        <LinkButton to="/events" variant="light" size="lg">
          See what’s on
        </LinkButton>
      </section>
    </div>
  )
}
