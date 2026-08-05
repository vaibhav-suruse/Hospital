# Security Hardening Notes

This document records the security review findings and the fixes applied to
the HMS application. It is a living reference — update it as further changes
are made.

## Summary of Critical Findings (from review)

| # | Finding | Severity | Status |
|---|---------|----------|--------|
| 1 | Only `DailyNotesController` required a login; every other controller (patients, billing, IPD, documents, users) was reachable by anyone with the URL | **Critical** | ✅ Fixed |
| 2 | Login flows leaked raw exception text / echoed credentials back to the browser | **High** | ✅ Fixed |
| 3 | No HSTS / HTTPS enforcement in production | Medium | ✅ Fixed |
| 4 | DB + email SMTP secrets committed in `appsettings.json` | **High** | ⚠️ Partially (see below) |
| 5 | Passwords stored in plaintext (needs DB migration to hash) | **High** | ⛔ Deferred (requires DB/schema change) |

## Fixes Applied

### 1. Global Authentication (Critical)
- **New file:** `Filters/RequireLoginGlobalFilter.cs`
- Registered globally in `Startup.ConfigureServices` via
  `options.Filters.Add(new RequireLoginGlobalFilter())`.
- Requires `Session["UserId"]` on every controller/action.
- Public allowlist:
  - Controllers: `Login`, `Home`, `PatientPortal` (PatientPortal runs its own
    `AccountId`/`PatientId` session checks internally).
  - Endpoints: `User/Login`, `User/IdentifyUser`, `User/GetSubHospitals`.
- Unauthenticated HTML requests redirect to `User/Login`; AJAX/JSON requests
  return a `{ success: false }` JSON payload.

### 2. Login Error Leakage (High)
- `LoginController.PatientLogin` no longer echoes the exception message or the
  entered email/password.
- `UserController.LoginClick` now returns a generic message instead of the raw
  stored-procedure exception text.

### 3. HSTS / HTTPS (Medium)
- `Startup.Configure`: `app.UseHsts()` enabled in non-development
  environments; `app.UseHttpsRedirection()` enabled.
- Comment these out only if your hosting/load-balancer handles TLS
  termination.

### 4. Secrets in Source Control (High — partially addressed)
- `appsettings.json` still contains the working local DB + SMTP credentials so
  the app runs out of the box.
- **Recommended next step:** move these to environment variables (highest
  precedence) or to a gitignored `appsettings.Development.json`. ASP.NET Core
  maps `__` in env-var names to `:` section separators:
  - `ConnectionStrings__MySqlConnection`
  - `EmailSettings__Host` / `EmailSettings__Port` /
    `EmailSettings__Username` / `EmailSettings__Password`
- `.gitignore` was updated to exclude `appsettings.Development.json`,
  `appsettings.Production.json`, `Logs/`, `bin/`, `obj/`, `.vs/`.

### 5. Plaintext Passwords (Deferred — needs DB migration)
- Passwords are currently stored/compared in plaintext via stored procedures.
- Hashing requires a DB-side migration (e.g. migrate to BCrypt/PBKDF2 at the
  repository layer and update the login/register stored procedures). This is
  intentionally **not** auto-changed because it would break existing accounts
  and touch the database schema.

## Recommended Next Steps
1. Rotate the DB and SMTP credentials (they are committed in git history).
2. Move secrets to environment variables / user-secrets and blank them in
   `appsettings.json`.
3. Plan a password-hashing migration (repository + stored procedures).
4. Consider replacing the session-based global filter with proper
   ASP.NET Core authentication + `[Authorize]` policies when the team is ready
   to invest in that refactor.
