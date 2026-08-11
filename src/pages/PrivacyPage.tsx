import PageHeader from '../components/shared/PageHeader'

export default function PrivacyPage() {
  return (
    <article className="legal-page mx-auto">
      <PageHeader
        eyebrow="Privacy"
        title="Privacy policy"
        description="How Campus Events handles information used to provide event services. Last updated 11 August 2026."
      />
      <h2>Information we handle</h2>
      <p>Depending on how you use the service, this may include account and profile details, organizer requests, event registrations, attendance, signed ticket identifiers, certificate records, voting activity, and payment status or provider references. Payment card and mobile-money credentials are handled by the configured payment provider, not stored by Campus Events.</p>
      <h2>Why we use it</h2>
      <p>We use this information to authenticate users, deliver requested event services, prevent abuse and duplicate voting, verify payments, communicate confirmations and reminders, provide operational reporting, and protect the security of the platform.</p>
      <h2>Service providers</h2>
      <p>The service may use Railway for application hosting, PostgreSQL for records, Supabase for private or public file storage, Paystack for payments, Google or Mailtrap for configured email delivery, and Google Analytics only when a valid measurement ID is explicitly enabled.</p>
      <h2>Retention and security</h2>
      <p>Operational records are retained only as needed for the service, legal obligations, dispute handling, and configured retention policies. Access controls, signed tokens, private storage links, audit logging, and server-side payment verification help protect information, but no online system can promise absolute security.</p>
      <h2>Your choices</h2>
      <p>You may ask the campus administrator responsible for this deployment to correct or review your account information and to explain applicable deletion or retention requirements. Analytics remains disabled unless the deployment owner supplies and enables a real GA4 measurement ID.</p>
      <h2>Contact</h2>
      <p>For privacy questions, contact the institution or system administrator that operates this Campus Events deployment.</p>
    </article>
  )
}
