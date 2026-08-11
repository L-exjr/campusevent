import {
  calculatePaidEventSettlement,
  calculatePercentageFeeMinor,
} from '../../utils/paymentPolicy'

describe('payment policy', () => {
  it('rounds percentage fees up to the next pesewa', () => {
    expect(calculatePercentageFeeMinor(5_000, 195)).toBe(98)
  })

  it('shows a zero platform fee and the estimated organizer settlement', () => {
    expect(calculatePaidEventSettlement(12_500)).toEqual({
      processingFeeMinor: 244,
      platformFeeMinor: 0,
      estimatedNetMinor: 12_256,
    })
  })
})
