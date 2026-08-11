export const PAYSTACK_GHANA_PROCESSING_FEE_BASIS_POINTS = 195
export const PLATFORM_FEE_BASIS_POINTS = 0

export function calculatePercentageFeeMinor(amountMinor: number, basisPoints: number) {
  if (!Number.isFinite(amountMinor) || amountMinor <= 0 || basisPoints <= 0) return 0
  return Math.ceil((amountMinor * basisPoints) / 10_000)
}

export function calculatePaidEventSettlement(priceMinor: number) {
  const processingFeeMinor = calculatePercentageFeeMinor(
    priceMinor,
    PAYSTACK_GHANA_PROCESSING_FEE_BASIS_POINTS,
  )
  const platformFeeMinor = calculatePercentageFeeMinor(priceMinor, PLATFORM_FEE_BASIS_POINTS)
  return {
    processingFeeMinor,
    platformFeeMinor,
    estimatedNetMinor: Math.max(0, priceMinor - processingFeeMinor - platformFeeMinor),
  }
}
