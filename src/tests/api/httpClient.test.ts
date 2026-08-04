import { afterEach, describe, expect, it, vi } from 'vitest'
import { fetchAllPages } from '../../api/httpClient'

describe('fetchAllPages', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('keeps at most four page requests in flight and preserves page order', async () => {
    let activeRequests = 0
    let maximumActiveRequests = 0
    vi.stubGlobal('fetch', vi.fn(async (input: string | URL | Request) => {
      const url = new URL(input.toString())
      const page = Number(url.searchParams.get('page'))
      activeRequests += 1
      maximumActiveRequests = Math.max(maximumActiveRequests, activeRequests)
      if (page > 1) await new Promise((resolve) => window.setTimeout(resolve, 5))
      activeRequests -= 1
      return new Response(JSON.stringify({
        items: [page],
        page,
        pageSize: 100,
        totalCount: 1000,
        totalPages: 10,
      }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }))

    await expect(fetchAllPages<number>('/events')).resolves.toEqual([
      1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
    ])
    expect(maximumActiveRequests).toBe(4)
  })
})
