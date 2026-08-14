import Badge from 'react-bootstrap/Badge'
import Col from 'react-bootstrap/Col'
import Row from 'react-bootstrap/Row'
import Accordion from 'react-bootstrap/Accordion'
import { useAuth } from '../hooks/useAuth'
import LinkButton from '../components/shared/LinkButton'
import { getHomeForRole } from '../utils/permissions'

const workflows = [
  {
    number: '01',
    symbol: '⌕',
    title: 'Discover',
    copy: 'Find events across campus and see the details that matter before you commit.',
  },
  {
    number: '02',
    symbol: '◇',
    title: 'Reserve',
    copy: 'Register in a few clicks, then keep every upcoming event in one clear view.',
  },
  {
    number: '03',
    symbol: '✓',
    title: 'Show up',
    copy: 'Get timely updates while organizers manage attendance and the guest experience.',
  },
]

const capabilities = [
  ['For students', 'Browse, register, and keep track of the events that make campus feel connected.'],
  ['For organizers', 'Create polished listings, follow registrations, and take attendance from one workspace.'],
  ['For administrators', 'Support your community with clear approvals, oversight, and reporting.'],
]

const highlights = [
  ['One', 'connected campus'],
  ['3', 'purpose-built roles'],
  ['24/7', 'event discovery'],
  ['100%', 'clearer coordination'],
]

const faqs = [
  ['Do I need an account to explore events?', 'No. Event discovery and details are public. A Student account is required for registration, tickets, and voting.'],
  ['How are paid registrations confirmed?', 'Paid places are confirmed only after the server verifies the configured payment provider’s webhook and transaction record. A browser success message alone never confirms a booking.'],
  ['Can I request help organizing an event?', 'Yes. Submit the public organizer request form with your preferred date, audience size, and event details.'],
  ['How does event check-in work?', 'Each confirmed registration has a signed QR ticket and a short ticket code. An authorized event owner can scan the QR code or type the code once to record attendance.'],
  ['When can I download a certificate?', 'After the event date has passed and an organizer has confirmed your attendance, a private certificate download becomes available.'],
  ['When are voting results visible?', 'Organizers can always monitor totals. They can choose to show live public results, otherwise public totals appear only after voting closes.'],
  ['Which payment methods are available?', 'Card and Ghana mobile-money availability depends on the configured payment provider. USSD, Apple Pay, and direct bank-payment checkout are planned but are not currently offered.'],
  ['Does the platform hold organizer balances or provide wallet payouts?', 'Not yet. Organizer wallets, payout balances, and payout-time guarantees are planned features; they are not part of the current platform.'],
  ['Can organizers publish a public nomination form?', 'Not yet. Organizers currently configure nominees inside voting campaign settings; a public nomination-form builder is planned.'],
]

export default function LandingPage() {
  const { user } = useAuth()

  return (
    <div className="landing-page">
      <section className="landing-hero" aria-labelledby="landing-title">
        <div className="landing-hero__copy">
          <Badge className="landing-kicker">Built for the moments between lectures</Badge>
          <h1 id="landing-title">
            Every campus moment, <span>within reach.</span>
          </h1>
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
              <span className="landing-event-preview__tag">Live on campus</span>
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
          <div className="landing-live-note" aria-hidden="true">
            <span />
            <strong>18 events</strong>
            <small>happening this month</small>
          </div>
        </div>
      </section>

      <section className="landing-proof" aria-label="Platform highlights">
        {highlights.map(([value, label]) => (
          <div key={label}>
            <strong>{value}</strong>
            <span>{label}</span>
          </div>
        ))}
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
                <div className="landing-step__top">
                  <span className="landing-step__symbol" aria-hidden="true">{item.symbol}</span>
                  <span>{item.number}</span>
                </div>
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
          <p className="landing-roles__intro mt-4">
            One calm, connected workspace—from the first announcement to the final check-in.
          </p>
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

      <section className="landing-section" aria-labelledby="community-stories">
        <p className="eyebrow mb-2">Community stories</p>
        <div className="landing-section__heading">
          <h2 id="community-stories">Real outcomes, published with permission.</h2>
          <p>We will only publish identifiable customer stories and reviews after they are verified and approved.</p>
        </div>
        <Row className="g-4 mt-2">
          <Col md={6}>
            <article className="content-placeholder h-100">
              <span>Case study structure ready</span>
              <h3>Success story awaiting verified customer content</h3>
              <p>The final case study will document the real event goal, measurable outcome, organizer attribution, and approved supporting media.</p>
            </article>
          </Col>
          <Col md={6}>
            <article className="content-placeholder h-100">
              <span>Testimonials structure ready</span>
              <h3>Customer reviews awaiting permission</h3>
              <p>No review text, names, ratings, or outcomes are fabricated. Verified submissions can be added here when supplied.</p>
            </article>
          </Col>
        </Row>
      </section>

      <section className="landing-section" aria-labelledby="frequently-asked-questions">
        <p className="eyebrow mb-2">Frequently asked questions</p>
        <div className="landing-section__heading">
          <h2 id="frequently-asked-questions">Useful answers before you commit.</h2>
          <p>Need organizer support for something more specific? We aim to reply within 24 hours on working days.</p>
        </div>
        <Accordion className="mt-4">
          {faqs.map(([question, answer], index) => (
            <Accordion.Item eventKey={String(index)} key={question}>
              <Accordion.Header>{question}</Accordion.Header>
              <Accordion.Body>{answer}</Accordion.Body>
            </Accordion.Item>
          ))}
        </Accordion>
      </section>

      <section className="landing-cta">
        <div>
          <p className="eyebrow mb-2">Your next event starts here</p>
          <h2>Make room for something memorable.</h2>
        </div>
        <div className="d-flex flex-wrap gap-2">
          <LinkButton to="/events" variant="light" size="lg">See what’s on</LinkButton>
          <LinkButton to="/request-organizer" variant="outline-light" size="lg">Plan an event</LinkButton>
          <LinkButton to="/about" variant="outline-light" size="lg">About us</LinkButton>
        </div>
      </section>
    </div>
  )
}
