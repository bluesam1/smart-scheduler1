# B) Amazon Cognito (User Pool + Hosted UI)

**Purpose:** AuthN/AuthZ for Admin, Dispatcher, Contractor.

**Setup Checklist**

1. **Create User Pool** with attributes: email (required), given/family name (optional). Disable public sign‑up if internal.
2. **App Client** (no secret for SPA): enable authorization code flow; allowed OAuth scopes: `openid`, `email`, `profile`.
3. **Hosted UI**: configure domain (e.g., `smartscheduler.auth.us-east-1.amazoncognito.com`).
4. **Callback/Sign‑out URLs**: `https://app.<your-domain>/auth/callback`, `https://app.<your-domain>/auth/signout`.
5. **Groups → Roles**: create groups `Admin`, `Dispatcher`, `Contractor`; map to app roles/claims.
6. **JWKS/JWT validation** in API: cache JWKS; validate `iss`, `aud`, `exp`, and `groups` claim.
7. **Token lifetimes**: default is fine (60m access/1d refresh); adjust per security policy.
8. **Local Dev**: create a dev pool and app client; set environment variables below.

**Environment Variables**

* `COGNITO_USER_POOL_ID`
* `COGNITO_APP_CLIENT_ID`
* `COGNITO_REGION` (e.g., `us-east-1`)
* `COGNITO_AUTH_DOMAIN` (Hosted UI domain)
* `JWT_ALLOWED_AUDIENCE` (App Client ID)
* `JWT_ALLOWED_ISSUER` (`https://cognito-idp.<region>.amazonaws.com/<poolId>`)

**API Integration (ASP.NET Core)**

* Use `AddAuthentication().AddJwtBearer(...)` pointed at Cognito issuer.
* Map Cognito `groups` claim → policy‑based authorization: `RequireRole("Admin")`, etc.

**Frontend (Next.js)**

* Use Hosted UI (redirect) or Cognito OAuth libraries; store tokens in memory; refresh via silent renew.

**Security**

* Enforce MFA for Admin; optional for others.
* Rotate app client secret if you ever enable confidential clients (backend‑to‑backend).

---
