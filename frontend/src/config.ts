declare global {
  interface Window {
    __APP_CONFIG__?: { API_URL?: string };
  }
}

// Order: runtime config (Docker) -> Vite env (.env.local) -> default
export const API_URL = (
  window.__APP_CONFIG__?.API_URL ||
  import.meta.env.VITE_API_URL ||
  'http://localhost:8080'
).replace(/\/$/, '');
