/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_HUB_PROTOCOL?: 'msgpack' | 'json'
  readonly VITE_API_BASE_URL?: string
}
