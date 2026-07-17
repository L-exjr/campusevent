import { useCallback, useEffect, useState } from 'react'

export function useApiResource<T>(loader: () => Promise<T>) {
  const [data, setData] = useState<T | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(await loader())
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Something went wrong.')
    } finally {
      setLoading(false)
    }
  }, [loader])

  useEffect(() => {
    let active = true
    void Promise.resolve()
      .then(() => {
        if (active) {
          setLoading(true)
          setError(null)
        }
        return loader()
      })
      .then((result) => active && setData(result))
      .catch((caught: unknown) => {
        if (active) {
          setError(caught instanceof Error ? caught.message : 'Something went wrong.')
        }
      })
      .finally(() => active && setLoading(false))

    return () => {
      active = false
    }
  }, [loader])

  return { data, loading, error, reload, setData }
}
