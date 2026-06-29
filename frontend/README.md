# DerasaX Frontend (unified)

The single unified DerasaX frontend, built with React + Vite. As of the Phase 20
frontend unification, this one app serves **all** three surfaces:

- **Public / marketing** (light theme, `PublicLayout`): `/`, `/request-demo`,
  `/events`, `/activities`, `/news`
- **Auth** (`/login`) — backend-wired login, role-based redirect
- **Authenticated portal** (`/app/**`, dark theme, `AppShell`) — student, teacher,
  parent, school-admin, and system-admin areas with role guards and tenant isolation

> The former separate `DerasaX-public` marketing app was merged into this app and
> archived (`archived-derasax-public/`). There is now exactly one production frontend.

## Features
- Public marketing homepage (hero + content cards), request-a-demo, info pages
- AI Learning Assistant (RAG chatbot), student/teacher/parent/admin dashboards
- Quiz system, performance visualization, computer-vision attendance (Phase 15)
- Auth with in-memory access token + HttpOnly refresh cookie, role guards
- i18n (English + Arabic) with automatic RTL; one i18n strategy across public + portal
- Strict CSP / security headers (`vercel.json`); correlation-id propagation

## Tech Stack
- React 19, Vite 7, React Router 7 (single central route registry)
- React Query, i18next + react-i18next, Recharts, Lucide, SignalR
- JavaScript (.jsx), plain CSS + CSS variables

## Run locally

```bash
npm install
npm run dev          # dev server on http://localhost:5173
```

The unified frontend talks only to the DerasaX backend (`VITE_DOTNET_URL`,
default `http://localhost:5155`); the browser never calls the AI service directly.
For the full local stack (Postgres + backend + AI + this frontend) use
`scripts/start-local.ps1` from the workspace root.

## Scripts
- `npm run dev` — Vite dev server (5173)
- `npm run build` — production build
- `npm run preview` — preview the build (4173; used by e2e)
- `npm run lint` — ESLint
- `npm test` — Vitest unit/integration tests
- `npm run test:e2e` — Playwright e2e (includes `e2e/phase20` public/auth/portal smoke)

## Public marketing pages
Marketing pages live under `src/pages/public/` and `src/layouts/PublicLayout.jsx`
(+ `PublicNavBar`/`PublicFooter`). Their styles are scoped under `.public-root`
(`src/styles/public-*.css`) so the light marketing theme stays isolated from the
dark portal. All copy is internationalized under the `public.*` i18n namespace.
