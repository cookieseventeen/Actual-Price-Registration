import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
// Noto Sans TC + JetBrains Mono are loaded from Google Fonts in index.html
// (dynamic unicode-range subsetting keeps CJK page-weight small). PrimeIcons
// is self-hosted via npm and bundled below.
import 'primeicons/primeicons.css';
import './styles/tokens.css';
import './styles/app.css';
import App from './App';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
