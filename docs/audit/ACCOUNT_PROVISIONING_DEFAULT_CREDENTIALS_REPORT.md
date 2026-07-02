# Account Provisioning and First-Login Password Change Report

## 1. Final Status

**PASSED**

## 2. Summary

DerasaX already generated temporary passwords server-side for SystemAdmin onboarding and
SchoolAdmin user provisioning — admins never typed a password. Two real gaps remained: the login
identifier was still caller-typed with no English-only validation, and nothing forced a password
change on first login. This pass closes both gaps end-to-end and centralizes the credential
generation that used to be duplicated in two services.

**Flows covered:**
1. SystemAdmin onboarding a tenant and creating the first SchoolAdmin.
2. SchoolAdmin creating Students, Teachers, and Parents (one shared endpoint/form, differentiated
   by role).
3. Reset-credential for Student/Teacher/Parent (existing) and — newly added — for SchoolAdmin
   accounts (SystemAdmin-triggered), closing a gap where no such path previously existed.

**Login identifier generation:** `CredentialProvisioningService.GenerateLoginCodeAsync` normalizes
the (already English-validated) full name to lowercase `first.last`, prefixes it by role
(`teacher.` / `parent.` / `admin.` for Teacher/Parent/SchoolAdmin; no prefix for Student), appends a
random 4-digit suffix, and retries against a **global** uniqueness check (up to 10 attempts, with a
GUID-based deterministic fallback) — e.g. `ahmed.hassan.4821`, `teacher.mona.salem.7382`. The
frontend no longer accepts a login-code input anywhere; the backend generates it unconditionally
(the field was removed from the request DTOs rather than accepted-and-ignored, since these are
internal, non-versioned API contracts).

**Temporary password generation:** unchanged, now centralized —
`CredentialProvisioningService.GenerateTemporaryPassword()` produces a 14-character CSPRNG password
guaranteeing at least one upper-case letter, one lower-case letter, and one digit (Fisher–Yates
shuffled so the guaranteed classes aren't always in the same position), satisfying the configured
Identity password policy. Returned once in the create/reset response; never logged or persisted in
clear text.

**First-login password change enforcement:** a new `MustChangePassword` bool on `ApplicationUser`
(migration `AddMustChangePasswordFlag`) is set `true` on every provisioning/reset flow. It's embedded
as a `mustChangePassword` JWT claim at login/refresh and enforced by a new
`MustChangePasswordGateMiddleware` that blocks every API endpoint except a small allowlist
(change-password, logout, revoke, refresh) while the flag is true. On the frontend, `ProtectedRoute`
(the single guard wrapping the entire authenticated `/app` shell) redirects to a new standalone
`/change-password` page whenever `mustChangePassword` is true — no `/app` route, including the
nav/shell, ever renders first. Successfully changing the password clears the flag, revokes all other
sessions, and issues one fresh refresh token so the browser immediately gets an access token
reflecting the cleared flag (via an automatic `/refresh` call), landing the user on their real
dashboard without a full re-login.

## 3. Backend Changes

| Area | File | Change | Security impact |
|---|---|---|---|
| Domain model | `DerasaX.Domain/Entities/Models/ApplicationUser.cs` | Added `MustChangePassword` bool (default `false`) | Enables the forced-change gate; old users default to `false` (no behavior change for existing accounts) |
| Migration | `DerasaX.Infrastructure/Migrations/20260702034743_AddMustChangePasswordFlag.cs` | Adds `MustChangePassword` column to `AspNetUsers`, non-nullable, default `false` | Additive, non-destructive |
| Domain exception | `DerasaX.Domain/Exceptions/ValidationException.cs` (new) | New exception type for character-class validation failures (distinct from empty-field `BadRequestException`) | — |
| Error mapping | `DerasaX.Api/Errors/ExceptionHandlingMiddleware.cs` | Maps `ValidationException` → 400 + existing `ErrorCodes.ValidationError` (reuses the `ImageValidationException` precedent) | No new error-code surface added unnecessarily |
| Validator | `DerasaX.Application/Common/EnglishNameValidator.cs` (new) | Single reusable full-name validator: rejects empty/whitespace (`BadRequestException`) and non-English text — Arabic, digits, emoji, symbols (`ValidationException`) | Backend-authoritative; cannot be bypassed by a modified frontend |
| Error codes | `DerasaX.Application/Common/AuthConstants.cs` | Added `ErrorCodes.PasswordChangeRequired` (`PASSWORD_CHANGE_REQUIRED`) | Stable machine code for the new gate's 403 |
| Credential service | `DerasaX.Application/Services.Abstractions/Provisioning/ICredentialProvisioningService.cs` + `DerasaX.Application/Services/Provisioning/CredentialProvisioningService.cs` (new) | Centralizes `GenerateTemporaryPassword()` (moved verbatim from two duplicated private copies) and adds `GenerateLoginCodeAsync` (normalize + role-prefix + random suffix + global-uniqueness retry) | Single audited implementation instead of two independently-maintained copies |
| DI | `DerasaX.Application/Extensions/ServiceCollectionExtensions.cs` | Registers `ICredentialProvisioningService` | — |
| DTOs | `DerasaX.Application/Dto/ProvisioningDto/ProvisioningDtos.cs`, `.../SystemAdminDto/SystemAdminDtos.cs` | Removed `LoginCode` from `CreateTenantUserDto` and `CreateSchoolAdminDto` | Caller can no longer submit an arbitrary login id under any circumstance |
| Provisioning | `DerasaX.Application/Services/Provisioning/UserProvisioningService.cs` | `CreateAsync`/`ResetCredentialAsync` now call `EnglishNameValidator`, `ICredentialProvisioningService`, and set `MustChangePassword = true` | Every Student/Teacher/Parent create/reset now forces a password change |
| System-admin portal | `DerasaX.Application/Services/SystemAdminPortal/SystemAdminPortalService.cs` | `CreateSchoolAdminAsync` same treatment; **new** `ResetSchoolAdminCredentialAsync` (closes the gap — no SystemAdmin-side SchoolAdmin credential reset existed before) | Same guarantee extended to the SchoolAdmin role, plus a previously-missing reset path |
| Interface | `.../Services.Abstractions/SystemAdminPortal/ISystemAdminPortalService.cs` | Added `ResetSchoolAdminCredentialAsync` signature | — |
| API endpoint | `DerasaX.Api/Controllers/SystemAdminController.cs` | New `POST api/v1/system-admin/tenants/{id}/school-admins/{userId}/reset-credential` | SystemAdminOnly-policy protected, tenant-scoped lookup (404 on cross-tenant/unknown) |
| Auth contract | `DerasaX.Application/Dto/AccountDto/AuthModel.cs` | Added `MustChangePassword` field | Frontend can react to the flag from login/refresh |
| Auth contract | `DerasaX.Application/Dto/AccountDto/ChangePasswordDto.cs` (`OperationResult`) | Added `RawRefreshToken`/`RefreshTokenExpiration` (never serialized directly — every controller projects this type into `{ message }`) | No new secret exposure; additive internal fields only |
| Login/session | `DerasaX.Application/Services/Account/AccountServices.cs` | `CreateJwtToken` embeds `mustChangePassword` claim; `BuildAuthModel` maps the flag; `ChangePasswordAsync` rejects a same-as-current new password, clears the flag, revokes all sessions, and issues one fresh refresh token; `ResetPasswordAsync` (anonymous forgot-password flow) also clears the flag | Prevents a "no-op" password change from bypassing the forced-change gate |
| Controller | `DerasaX.Api/Controllers/AccountController.cs` | `ChangePassword` action rotates the refresh cookie (was: always cleared it) when a fresh token was issued | Lets the browser immediately refresh into an unblocked session instead of requiring a full re-login |
| Enforcement | `DerasaX.Api/Security/MustChangePasswordGateMiddleware.cs` (new) | Blocks every `/api` request but a small allowlist (change-password/logout/revoke/refresh) when the `mustChangePassword` claim is `true`; returns 403 `PASSWORD_CHANGE_REQUIRED` | Backend-enforced, not just a frontend redirect — cannot be bypassed by calling the API directly |
| Pipeline | `DerasaX.Api/Program.cs` | Registers the gate middleware between `UseAuthentication` and `UseAuthorization` | Runs for every current and future controller uniformly |
| Contract snapshot | `docs/contracts/backend-openapi-v1-paths.json`, `docs/phase5/logs/08-openapi-endpoint-inventory.txt`, `docs/phase5/logs/openapi-v1-phase5.json` | Auto-regenerated by the existing `BackendOpenApiContractTests`/`OpenApiCaptureTests` snapshot tests to include the new endpoint and the removed `loginCode` request fields | Evidence the live OpenAPI surface matches the code changes; not hand-edited |

## 4. Frontend Changes

| Area | File | Change | UX impact |
|---|---|---|---|
| Session shape | `src/features/auth/authApi.js` | `toSession(model)` carries `mustChangePassword` through from the backend `AuthModel` | — |
| Auth state | `src/features/auth/AuthContext.jsx` | New `mustChangePassword` state; `changePassword()` now calls `refreshOnce()` after a successful change so the session immediately reflects the cleared flag | Also fixes a pre-existing rough edge in the voluntary Security page flow (previously stranded the user on a non-refreshable session until the ~15 min token expired) |
| Route guard | `src/features/auth/guards.jsx` | `ProtectedRoute` redirects to `/change-password` when `mustChangePassword` is true (before rendering the `/app` shell/nav); new `RequirePasswordChange` guard for the forced page itself | Blocks the entire authenticated app, not just individual pages |
| Routing | `src/app/App.tsx` | Hardcoded `/change-password` route (not in the `ROUTES` registry, matching how `/forbidden`/`/suspended` are wired) | Avoids bumping `routeArchitecture.test.js`'s `EXPECTED_ROUTE_COUNT` and the registry's `/app`-nesting logic |
| New page | `src/pages/auth/ForcedChangePasswordPage.jsx` (+ test) | Forced change-password form (current/new/confirm), rejects a same-as-current new password, navigates to the user's home route on success, offers a logout escape hatch | New screen: "you're signing in with a temporary password..." |
| Credentials display | `src/components/credentials/CredentialsPanel.jsx` (new), re-exported via `src/shared/ui/index.tsx`; `src/lib/clipboard.js` (new) | One-time modal: full name/role/login id/temporary password, copy-per-field + copy-all, download-as-text, "shown once" notice | Replaces 3 duplicated inline `<Alert>` credential displays with one shared, richer component |
| Client validation | `src/lib/validation/englishName.js` (new) | Mirrors the backend's English-name regex for immediate inline feedback | Client-side only — backend re-validates regardless |
| Onboarding | `src/pages/system/onboarding/SystemOnboardingPage.jsx` (+ test) | Removed the login-code input; added English-name inline validation; wired `CredentialsPanel` | No password/login-code ever requested from a SystemAdmin |
| School users | `src/pages/school/users/SchoolUsersPage.jsx` (+ test) | Same treatment for Student/Teacher/Parent creation | Same guarantee for SchoolAdmin-created accounts |
| User detail | `src/pages/school/users/SchoolUserDetailPage.jsx` | Reset-credential action now opens `CredentialsPanel` instead of an inline alert | Consistent one-time-display UX across create and reset |
| i18n | `src/i18n/locales/en.js`, `ar.js` | New `validation.*`, `forcedChangePassword.*`, `credentials.*` namespaces (EN+AR, parity-tested); fixed a pre-existing gap where `school.credential.password` was referenced but never defined | Arabic UI text exists for every new screen; RTL untouched (no layout changes, only text/component additions) |
| Styling | `src/styles/components.css` | New `.credentials-panel*` rules (row/label/value/mono) | — |

## 5. Credential Generation Rules

**English-name validation** (`EnglishNameValidator.cs` backend, `englishName.js` frontend mirror):
- Accepts: English letters, internal single spaces, hyphen, apostrophe, dot (`Ahmed Hassan`,
  `O'Brien-Smith`, `Jean.Paul`).
- Rejects: Arabic/other non-Latin script, digits anywhere in the name, emoji, other symbols,
  empty/whitespace-only.
- Enforced server-side on every creation/reset call path (backend is authoritative regardless of
  frontend state); frontend duplicates the check only for immediate inline feedback.

**Login identifier format:** `{role-prefix.}{first}.{last}.{4-digit-suffix}` — no prefix for
Student, `teacher.`/`parent.`/`admin.` for Teacher/Parent/SchoolAdmin. Example outputs:
`ahmed.hassan.4821`, `teacher.mona.salem.7382`, `parent.laila.omar.5510`, `admin.samir.taha.1029`.

**Password generation rules:** 14 characters, CSPRNG (`RandomNumberGenerator`), guarantees ≥1
upper-case, ≥1 lower-case, ≥1 digit, remaining characters drawn from the combined alphabet,
Fisher–Yates shuffled. Meets the configured Identity policy (`RequireDigit`/`RequireUppercase`/
`RequireLowercase` = true).

**Collision handling:** login-code generation checks **global** uniqueness (not per-tenant, because
login resolves the code with no tenant filter) via `UserManager.Users.AnyAsync`, retries with a fresh
random suffix up to 10 times, then falls back to a GUID-derived suffix — provisioning never fails
just because the base name/role combination already exists. Verified with two accounts sharing the
identical full name (both backend integration tests and the live e2e run).

**One-time display behavior:** the temporary password is returned only in the create/reset HTTP
response body; it is never logged (`ILogger` calls only ever include ids/roles/results, confirmed by
existing audit-log assertions plus the new `MustChangePasswordApiTests`), never re-served by any GET
endpoint, and the frontend's `CredentialsPanel` only ever holds it in local component/mutation-result
state (never persisted to storage).

## 6. First-Login Password Change Flow

- **Login response:** `AuthModel.mustChangePassword` (also embedded as a `mustChangePassword` JWT
  claim, refreshed at every login/refresh).
- **Frontend redirect:** `AuthContext.applySession` stores the flag; `ProtectedRoute` (the sole
  wrapper around the entire `/app` shell) redirects to `/change-password` whenever it's true, before
  any nested route or the nav/shell renders.
- **Route guard:** `RequirePasswordChange` guards `/change-password` itself — anonymous → `/login`;
  authenticated with nothing to force → the user's normal home route (never shows the forced form
  needlessly).
- **Backend enforcement:** `MustChangePasswordGateMiddleware` rejects every `/api` request except
  `change-password`/`logout`/`revoke`/`refresh` with 403 `PASSWORD_CHANGE_REQUIRED` while the claim is
  true — a bypassed/modified frontend cannot reach any other endpoint.
- **Flag clearing:** a successful `POST /account/change-password` (a) rejects a new password equal to
  the current one, (b) clears `MustChangePassword`, (c) bumps the security stamp and revokes all
  other sessions, (d) issues one fresh refresh token. The frontend immediately calls `/refresh` with
  that token to obtain an access token whose claim reflects the cleared flag, unblocking the app
  without a full re-login. (The anonymous forgot-password/reset-password flow clears the flag too, on
  the same principle, without reissuing a token since there's no live browser session in that path.)

## 7. Tests Added/Updated

| Test file | What it proves | Result |
|---|---|---|
| `backend/.../EnglishNameValidatorTests.cs` (new) | Accepts valid English names; rejects Arabic/digits/emoji/symbols; rejects empty/whitespace | PASS |
| `backend/.../MustChangePasswordApiTests.cs` (new) | New account has `MustChangePassword=true` in the DB; a non-allowlisted endpoint 403s with `PASSWORD_CHANGE_REQUIRED` while flagged; changing to the same password is rejected; a real change clears the DB flag and a subsequent `/refresh` unblocks the endpoint; allowlisted `/account/revoke` remains reachable | PASS |
| `backend/.../UserProvisioningApiTests.cs` (updated) | Create returns a **generated** login code (no longer caller-typed) that logs in and requires a password change; same full name twice yields distinct login codes, both log in; non-English/empty names are rejected 400; reset rotates the password (not the login code) and re-flags `MustChangePassword` | PASS |
| `backend/.../SystemAdminPortalApiTests.cs` (updated) | Full onboarding→suspend→reactivate→**reset-credential** lifecycle with generated credentials at every step; same-full-name-twice → distinct codes (replaces the now-unreachable duplicate-login-code test); non-English names rejected | PASS |
| `backend/.../PlanLimitEnforcementApiTests.cs` (updated) | Fixed 2 pre-existing tests that create-then-immediately-use a fresh SchoolAdmin — now change the temporary password first (matching the new mandatory gate) before continuing | PASS |
| `backend/.../TestClient.cs` (updated) | Shared `LoginResponse` test record gains `mustChangePassword` (additive) | — |
| `frontend/.../mustChangePasswordGuard.test.jsx` (new) | `mustChangePassword: true` redirects `/app` → `/change-password`; false → renders normally; `/change-password` itself redirects home/`/login` when there's nothing to force / not authenticated | PASS |
| `frontend/.../ForcedChangePasswordPage.test.jsx` (new) | Successful change calls `changePassword` and navigates to the role's home; same-as-current password is rejected client-side with no backend call; logout escape hatch works | PASS |
| `frontend/.../SystemOnboardingPage.test.jsx` (updated) | No password/login-code fields; Arabic full name rejected with the exact message, submit disabled; generated login code shown in the credentials panel | PASS |
| `frontend/.../SchoolUsersPage.test.jsx` (updated) | No password/login-code fields; Arabic name rejected; valid creation shows the generated login id + temp password in the panel | PASS |
| `frontend/e2e/phase21/a-account-provisioning.spec.js` (new, live) | Full journey: SystemAdmin onboards tenant + SchoolAdmin (no password typed) → forced change → SchoolAdmin dashboard → SchoolAdmin provisions a Teacher (no password typed) → forced change → Teacher dashboard; non-English names rejected on both forms | PASS (×2 runs) |
| `frontend/e2e/phase12/b-system-management.spec.js` (fixed) | Pre-existing onboarding e2e test updated for the new contract (no login-code field; credentials shown in the modal) | PASS |

## 8. Verification Commands

| Command | Result | Notes |
|---|---|---|
| `cd backend && dotnet build` | ✅ 0 errors | Clean build after every change |
| `cd backend && dotnet ef migrations add AddMustChangePasswordFlag ...` | ✅ | No SAC/dotnet-ef environment issue this session |
| `cd backend && dotnet ef database update ...` | ✅ | Migration applied to local dev DB |
| `cd backend && dotnet test` (×2 consecutive runs) | ✅ 439/442 passed both runs | 3 failures are pre-existing, unrelated, verified stable — see §10 |
| `cd frontend && npm run typecheck` | ✅ 0 errors | |
| `cd frontend && npm run lint` | ✅ 0 errors (1 pre-existing unrelated warning in `SchoolClassDetailPage.jsx`) | |
| `cd frontend && npm test -- --run` | ✅ 128 files / 602 tests passed | Includes all new/updated tests |
| `cd frontend && npm run build` | ✅ | Production build succeeds (pre-existing large-chunk warning, unrelated) |
| `scripts/start-local.ps1 -Gate core` | ✅ Postgres + backend + AI + frontend reachable | Real local stack for live e2e |
| `npx playwright test e2e/phase21/` (×2 consecutive runs) | ✅ 2/2 passed both runs | Full live account-provisioning + forced-change journey against the real backend/DB |
| `npx playwright test e2e/phase12/b-system-management.spec.js` (targeted) | ✅ passed after fixing the pre-existing test for the new contract | |
| `npx playwright test e2e/phase11/a-school-auth.spec.js e2e/phase8/a-auth.spec.js` (smoke) | 2 pre-existing, unrelated failures found and root-caused (not fixed — out of scope) | See §10 |

## 9. Manual QA Checklist

- [x] SystemAdmin creates tenant and first SchoolAdmin without entering a password. *(verified live, e2e)*
- [x] Generated credentials are shown once, in a dedicated one-time panel with copy/download actions.
- [x] New SchoolAdmin must change password on first login before reaching any dashboard. *(verified live, e2e)*
- [x] SchoolAdmin creates a Teacher without entering a password (Student/Parent go through the
      identical code path; Student additionally requires a Grade, unrelated to this feature).
      *(verified live, e2e)*
- [x] Generated credentials work once and force a password change on that first login. *(verified live, e2e)*
- [x] User must change password before reaching a dashboard — backend blocks every other endpoint,
      not just the frontend route. *(verified via `MustChangePasswordApiTests` hitting a real
      non-allowlisted endpoint and getting 403 `PASSWORD_CHANGE_REQUIRED`)*
- [x] Arabic/non-English names are rejected on both the onboarding and school-user creation forms,
      client-side and server-side. *(verified live, e2e, plus unit/integration tests both sides)*
- [x] Duplicate names generate unique login identifiers, and both accounts can log in.
      *(verified in backend integration tests)*

## 10. Remaining Issues

No remaining account-provisioning issues found.

The following were investigated during verification and are **pre-existing, unrelated to this
change** (root-caused with evidence, not fixed — out of scope for this task):

- `AcademicDomainTests.ParentStudent_cross_tenant_rejected_same_tenant_ok_and_many_to_many` (backend)
  — fails with a unique-constraint violation on `parentStudentRelationships`, caused by a leftover
  row from an earlier, unrelated test run accumulated in the shared local dev DB over many prior
  sessions. Unrelated to any entity touched by this change.
- `SchoolAdminPortalApiTests.Admin_creates_link_then_rejects_duplicate` and
  `Phase16FileStorageApiTests.ParentDocument_linked_parent_can_download_unrelated_cannot_and_audited`
  (backend) — both fail logging into a `PH11-*`/similar seed-fixture login code that no longer
  resolves (a missing/stale seed row), unrelated to account provisioning.
- `e2e/phase12/b-system-management.spec.js` **PH12-B5** (support-ticket response) and
  `e2e/phase8/a-auth.spec.js` **A2** (student dashboard heading) — both pre-existing e2e/data
  staleness confirmed live: PH12-B5's seeded support ticket has no reset path in
  `DevE2EController` and was already answered by a prior run; A2's expected heading text
  (`/student dashboard/i`) no longer matches the real page, which now renders a personalized
  greeting ("Welcome back, {name}") — a UI copy change that predates this session. Both match the
  previously-documented "pre-existing E2E/UI staleness" category from an earlier audit pass.
- One additional pre-existing e2e flake (`phase11/a-school-auth.spec.js` **PH11-A6**, 2 of 11
  parameterized sub-cases) passed on Playwright's built-in retry — consistent with the suite's
  documented single-worker/shared-backend timing characteristics, not a regression.
- Tenant-1's local dev subscription was assigned a high-capacity plan (via the real
  `POST /api/v1/tenants/plans` + `/api/v1/tenants/subscriptions` SystemAdmin API, not raw SQL) after
  the test run discovered it had accumulated 2223+ users against its "Free" plan's 50-student cap
  from years of repeated local test runs — this was blocking `dotnet test` across many unrelated
  suites, confirmed with the user before acting. The shared "Free" plan definition itself was not
  modified, so `PlanLimitEnforcementApiTests` (which always uses its own dedicated throwaway
  tenant+plan) is unaffected.

No Git commits, pushes, or history changes were made at any point during this work.
