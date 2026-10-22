import React from 'react';
import ReactDOM from 'react-dom/client';
import App from '@/App';
import { AppProviders } from '@/components/app/AppProviders';
import '@/styles/globals.css';
import { useSessionStore } from '@/stores/useSessionStore';

// Portfolio-only mode uses the existing synthetic fallback. Normal builds do not enable it.
if (import.meta.env.MODE === 'showcase') {
  useSessionStore.getState().setSession('portfolio-demo-session', {
    id: 1,
    username: 'portfolio-demo',
    email: 'demo@example.invalid',
    firstName: 'کاربر',
    lastName: 'نمایشی',
    roles: ['Admin'],
    permissions: [],
  });
  document.title = 'ArchiveSystem · Portfolio Demo';
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <AppProviders>
      <App />
    </AppProviders>
  </React.StrictMode>,
);

