import { act, render, screen, waitFor } from '@testing-library/react'
import { useCallback } from 'react'
import { useApiResource } from '../../hooks/useApiResource'

interface PendingRequest {
  query: string
  signal: AbortSignal
  resolve: (value: string) => void
}

describe('useApiResource', () => {
  it('aborts superseded requests and ignores their late results', async () => {
    const pending: PendingRequest[] = []
    function Probe({ query }: { query: string }) {
      const load = useCallback((signal: AbortSignal) => new Promise<string>((resolve) => {
        pending.push({ query, signal, resolve })
      }), [query])
      const { data } = useApiResource(load)
      return <div>{data ?? 'loading'}</div>
    }

    const view = render(<Probe query="old" />)
    await waitFor(() => expect(pending).toHaveLength(1))
    view.rerender(<Probe query="new" />)
    await waitFor(() => expect(pending).toHaveLength(2))
    expect(pending[0].signal.aborted).toBe(true)

    await act(async () => {
      pending[0].resolve('stale result')
      pending[1].resolve('current result')
    })

    expect(await screen.findByText('current result')).toBeVisible()
    expect(screen.queryByText('stale result')).not.toBeInTheDocument()
  })
})
