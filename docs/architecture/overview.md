# Architecture Overview

## Strategic goal
Create a secure, maintainable PaaS-style platform that combines:
- public marketing and product discovery
- authenticated user workflows
- partner/mitra operations
- administrative management
- platform-level features for search, contact, notifications, and content management

## Architectural style
The application is structured with layered responsibilities:

- Presentation: Razor Pages and MVC controllers
- Application: use cases, DTOs, validators, and service orchestration
- Domain: entities, value objects, rules, and aggregate boundaries
- Infrastructure: EF Core, PostgreSQL, Identity, repositories, storage abstraction, and external integrations

## Runtime architecture
Public and authenticated experience:

Razor Pages / MVC
  -> Application Services
  -> Domain Layer
  -> Infrastructure Layer
  -> PostgreSQL

REST APIs:

Client / Frontend / Integrators
  -> ASP.NET Core Web API
  -> Application Services
  -> Domain Layer
  -> Infrastructure Layer
  -> PostgreSQL

gRPC internal communication:

Razor/MVC / REST layer
  -> Application Services
  -> gRPC Services
  -> Application Services / Domain
  -> Infrastructure
  -> PostgreSQL

## Technology choices
- .NET 10
- ASP.NET Core
- Razor Pages
- ASP.NET Core MVC
- REST API with versioning
- gRPC for internal service requests
- EF Core + PostgreSQL
- ASP.NET Core Identity
- Role-based policy authorization
- Swagger / OpenAPI
- xUnit and integration test coverage
- Docker-ready deployment configuration

## Core product domains
- identity and access management
- portfolio and project showcase
- platform products and service catalog
- inquiries and service requests
- partner management and profile ownership
- content management and announcements
- notifications and audit logging
- file/media storage abstraction

## Security architecture
The platform must enforce security at the server boundary:
- identity with ASP.NET Core Identity
- RBAC with explicit roles: Admin, User, Mitra
- policy-based authorization for pages, controllers, APIs, and gRPC operations
- anti-forgery for state-changing forms
- secure cookie configuration and HTTPS enforcement
- structured validation and ProblemDetails responses
- audit logging for sensitive and administrative actions
- rate limiting and input validation

## Observability and quality goals
- logs for request and security events
- structured audit trail
- health and metrics readiness
- clean separation of public and internal interactions
- test coverage for auth, RBAC, ownership, contact, search, and APIs

## Delivery approach
The implementation will follow the required sprint sequence:
- Sprint 0: references and requirements audit
- Sprint 1: solution architecture
- Sprint 2: identity and RBAC foundation
- Sprint 3: domain and PostgreSQL
- Sprint 4: application layer
- Sprint 5: REST API
- Sprint 6: gRPC
- Sprint 7: public Razor Pages
- Sprint 8: user dashboard
- Sprint 9: mitra dashboard
- Sprint 10: admin dashboard
- Sprint 11: portfolio, services, and CMS
- Sprint 12: search, contact, and notification
- Sprint 13: security hardening
- Sprint 14: testing
- Sprint 15: performance and observability
- Sprint 16: Docker, CI/CD, and production readiness

## Non-goals for this first gate
This first pass is limited to:
- architecture documentation
- solution creation
- restore, build, and test verification

It does not implement the full business application yet, to respect the sprint gating requirement.
