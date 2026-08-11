import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'

declare global {
  interface Window {
    dataLayer?: unknown[]
    gtag?: (...args: unknown[]) => void
  }
}

export default function Analytics() {
  const location = useLocation()
  const measurementId = import.meta.env.VITE_GA4_MEASUREMENT_ID?.trim()
  const validId = measurementId && /^G-[A-Z0-9]+$/i.test(measurementId) ? measurementId : null

  useEffect(() => {
    if (!validId || document.querySelector(`script[data-ga4="${validId}"]`)) return
    const script = document.createElement('script')
    script.async = true
    script.src = `https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(validId)}`
    script.dataset.ga4 = validId
    document.head.append(script)
    window.dataLayer = window.dataLayer ?? []
    window.gtag = (...args: unknown[]) => window.dataLayer?.push(args)
    window.gtag('js', new Date())
    window.gtag('config', validId, { send_page_view: false, anonymize_ip: true })
  }, [validId])

  useEffect(() => {
    if (!validId || !window.gtag) return
    window.gtag('event', 'page_view', {
      page_path: `${location.pathname}${location.search}`,
      page_title: document.title,
    })
  }, [location.pathname, location.search, validId])

  return null
}
