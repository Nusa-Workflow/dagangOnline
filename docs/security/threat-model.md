# Threat Model

## Scope
This platform protects public marketing pages, authenticated user workflows, partner operations, and administrative management for catalog content and inquiries.

## Trust boundaries
- Public internet: anonymous visitors can view public content.
- Authenticated session boundary: ASP.NET Core Identity and server-side authorization.
- Resource ownership boundary: each entity must be checked against the current principal, not against route or form values.
- Admin boundary: admin operations are allowed only for explicitly authorized roles.

## Primary threats
1. IDOR/BOLA via manipulated IDs or user-controlled resource references.
2. Role escalation through client-supplied role claims or tampered payloads.
3. Missing ownership checks on partner or user resources.
4. UI-only controls allowing hidden actions to remain callable from the server.
5. Mass assignment or trust of request-bound properties.
6. Unlogged privileged operations and sensitive access patterns.

## Security controls
- Deny by default on all authorization decisions.
- Server-side authorization only; never trust client-side role flags.
- Require role + policy + ownership + operation permission checks together.
- Validate resource ownership from persisted data and authenticated user identity.
- Restrict admin-only operations using policy enforcement and audit trail entries.
- Treat all request IDs as untrusted inputs until validated against the current principal.

## Residual risk
The remaining residual risk is primarily operational: failed configuration, missing audit logging, and accidentally exposing actions without consistent policy checks. This is mitigated by automated regression tests and explicit permission documentation.
