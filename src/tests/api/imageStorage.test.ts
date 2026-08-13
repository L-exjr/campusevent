import {
  MAX_IMAGE_SIZE_BYTES,
  uploadImage,
  validateImageFile,
} from '../../api/imageStorage'

describe('imageStorage image validation', () => {
  it.each(['image/jpeg', 'image/png', 'image/webp'])('accepts %s images', (type) => {
    const file = new File(['image'], 'image', { type })

    expect(() => validateImageFile(file)).not.toThrow()
  })

  it('rejects unsupported file types before upload', () => {
    const file = new File(['not-an-image'], 'document.pdf', { type: 'application/pdf' })

    expect(() => validateImageFile(file)).toThrow('Choose a JPG, PNG, or WebP image.')
  })

  it('rejects files larger than 5 MB before upload', () => {
    const file = new File(
      [new Uint8Array(MAX_IMAGE_SIZE_BYTES + 1)],
      'large.png',
      { type: 'image/png' },
    )

    expect(() => validateImageFile(file)).toThrow('Images must be 5 MB or smaller.')
  })

  it('routes uploads through the authenticated backend API', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'test-csrf-token' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }))
      .mockResolvedValueOnce(new Response(
        JSON.stringify({ url: 'https://example.supabase.co/storage/image.png' }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ))
    vi.stubGlobal('fetch', fetchMock)
    const file = new File(['image'], 'profile.png', { type: 'image/png' })

    const url = await uploadImage(file, 'profile-images')

    expect(url).toBe('https://example.supabase.co/storage/image.png')
    expect(fetchMock.mock.calls[0][0]).toBe('http://localhost:5080/api/auth/csrf')
    const [requestUrl, request] = fetchMock.mock.calls[1]
    expect(requestUrl).toBe('http://localhost:5080/api/uploads/profile-image')
    expect(request.credentials).toBe('include')
    expect(request.headers.has('Authorization')).toBe(false)
    expect(request.headers.get('X-CSRF-TOKEN')).toBe('test-csrf-token')
    expect(request.headers.has('Content-Type')).toBe(false)
    expect(request.body).toBeInstanceOf(FormData)
    vi.unstubAllGlobals()
  })
})
