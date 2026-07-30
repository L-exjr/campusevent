import {
  MAX_IMAGE_SIZE_BYTES,
  validateImageFile,
} from '../../api/supabaseStorage'

describe('supabaseStorage image validation', () => {
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
})
