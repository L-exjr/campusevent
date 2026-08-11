import { useEffect } from 'react'

export default function StructuredBusinessData() {
  useEffect(() => {
    const name = import.meta.env.VITE_BUSINESS_NAME?.trim()
    const locality = import.meta.env.VITE_BUSINESS_LOCALITY?.trim()
    const country = import.meta.env.VITE_BUSINESS_COUNTRY?.trim()
    if (!name || !locality || !country) return

    const script = document.createElement('script')
    script.id = 'local-business-structured-data'
    script.type = 'application/ld+json'
    script.text = JSON.stringify({
      '@context': 'https://schema.org',
      '@type': 'LocalBusiness',
      name,
      url: import.meta.env.VITE_BUSINESS_URL?.trim() || undefined,
      telephone: import.meta.env.VITE_BUSINESS_PHONE?.trim() || undefined,
      openingHours: import.meta.env.VITE_BUSINESS_OPENING_HOURS?.trim() || undefined,
      address: {
        '@type': 'PostalAddress',
        streetAddress: import.meta.env.VITE_BUSINESS_STREET_ADDRESS?.trim() || undefined,
        addressLocality: locality,
        addressRegion: import.meta.env.VITE_BUSINESS_REGION?.trim() || undefined,
        postalCode: import.meta.env.VITE_BUSINESS_POSTAL_CODE?.trim() || undefined,
        addressCountry: country,
      },
    })
    document.head.append(script)
    return () => script.remove()
  }, [])

  return null
}
