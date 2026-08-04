import { useCallback, useEffect, useRef, useState } from 'react'

function isAbortError(error: unknown) {
  return error instanceof Error && error.name === 'AbortError'
}

export function useApiResource<T>(loader: (signal: AbortSignal) => Promise<T>) {
  const [data, setData] = useState<T | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const requestRef = useRef<AbortController | null>(null)

  const reload = useCallback(async () => {
    requestRef.current?.abort()
    const controller = new AbortController()
    requestRef.current = controller
    setLoading(true)
    setError(null)
    try {
      const result = await loader(controller.signal)
      if (requestRef.current === controller && !controller.signal.aborted) {
        setData(result)
      }
    } catch (caught) {
      if (requestRef.current === controller && !isAbortError(caught)) {
        setError(caught instanceof Error ? caught.message : 'Something went wrong.')
      }
    } finally {
      if (requestRef.current === controller) {
        requestRef.current = null
        setLoading(false)
      }
    }
  }, [loader])

  useEffect(() => {
    let active = true
    void Promise.resolve().then(() => {
      if (active) return reload()
    })

    return () => {
      active = false
      requestRef.current?.abort()
    }
  }, [reload])

  return { data, loading, error, reload, setData }
}
