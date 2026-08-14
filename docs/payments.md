# Payment architecture

`IPaymentProvider` defines provider-neutral initialization, verification, refund, webhook-signature, and successful-notification behavior. `PaymentProviderResolver` selects the active implementation from `PAYMENTS_PROVIDER` or `Payments:Provider`; the tracked default remains `Paystack`.

Paystack and Flutterwave are registered simultaneously. Orders persist their provider, tier, original price, discount, final amount, and provider reference. The selected provider receives only the server-calculated final amount.

- Paystack webhook: `POST /api/payments/webhooks/paystack`, `x-paystack-signature` HMAC verification.
- Flutterwave webhook: `POST /api/payments/webhooks/flutterwave`, configured `verif-hash` verification.

Both endpoints independently verify the transaction reference, amount, and currency with their provider before changing local state. Receipts and database locks make registration/vote handling idempotent. Provider endpoints are intentionally separate.

Paystack remains the production default. Flutterwave is implemented for sandbox/staging verification and must not become active until merchant onboarding, Ghana MoMo coverage, callbacks, refunds, and reconciliation pass end-to-end testing. Paystack removal, disclosure changes, and production cutover are separate work.

USSD, Apple Pay, direct bank-payment checkout, organizer wallet balances, marketplace payout reconciliation, and payout-time guarantees are not implemented.
