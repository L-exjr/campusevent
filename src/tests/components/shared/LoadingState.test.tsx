import { render, screen } from '@testing-library/react'
import LoadingState from '../../../components/shared/LoadingState'

describe('LoadingState', () => {
  it('announces loading and exposes a structural skeleton', () => {
    const { container } = render(<LoadingState label="Loading events" />)

    expect(screen.getByRole('status')).toHaveAttribute('aria-busy', 'true')
    expect(screen.getByText('Loading events…')).toBeVisible()
    expect(container.querySelector('.loading-skeleton')).toBeInTheDocument()
  })

  it('keeps full-page session restoration compact', () => {
    const { container } = render(<LoadingState label="Restoring session" fullPage />)

    expect(container.querySelector('.loading-skeleton')).not.toBeInTheDocument()
  })
})
