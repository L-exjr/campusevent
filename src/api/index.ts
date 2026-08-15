import type { EventManagementApi } from './EventManagementApi'
import { mockApi } from './mockApi'
import { realApi } from './realApi'

// Mock data is available only in the Vite development server and must also be
// requested explicitly. Vite replaces DEV with false in every production build,
// allowing the mock implementation to be removed from the generated bundle.
export const usingMockApi =
  import.meta.env.DEV && import.meta.env.MODE !== 'test' && import.meta.env.VITE_USE_MOCK_API === 'true'

export const api: EventManagementApi = usingMockApi ? mockApi : realApi
