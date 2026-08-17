import { defineConfig } from 'vitest/config'
import { loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ command, mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const isProductionBuild = command === 'build' && mode === 'production'
  if (isProductionBuild && env.VITE_USE_MOCK_API?.trim().toLowerCase() === 'true') {
    throw new Error('VITE_USE_MOCK_API=true is not allowed in production builds.')
  }

  return {
    plugins: [react()],
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: ['./src/tests/setup.ts'],
      css: false,
      maxWorkers: 4,
      testTimeout: 10_000,
    },
  }
})
