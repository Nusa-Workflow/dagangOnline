# Reference Audit

## Objective
This document records the analysis of the provided inspiration sources and translates them into a product and architecture direction for a new platform that is secure, extensible, and role-aware without copying visual design or source code.

## Inspiration sources reviewed
1. https://cv-swandaru-tirta.vercel.app/
2. https://prisma-rose.vercel.app/
3. https://github.com/swandrax/umkm-bu-inem.git
4. https://elka-grafika-printing.vercel.app/

## Audit summary
The references collectively suggest a direction for:
- personal/brand storytelling
- service and portfolio presentation
- digital services for SMEs and public/community stakeholders
- strong calls to action and trust-building sections
- marketing-driven landing pages paired with action-oriented inquiry flows
- a platform ecosystem that supports services, portfolio, partnerships, and operational workflows

## What is intentionally not copied
This platform must not inherit or reproduce:
- the exact HTML/CSS layout or design
- the branding system or visual identity
- the same component structure or page composition
- any copied source code, implementation patterns, or proprietary business logic

## Product insights extracted
### 1. Public brand presence
The reference set supports a company profile and service-first narrative. The product must communicate:
- technology capability
- portfolio credibility
- service value proposition
- trust, capability, and outcomes

### 2. Service and solution orientation
The examples emphasize digital transformation, especially for small businesses, communities, and institutions. The platform must package capabilities in categories such as:
- software development
- web and mobile platform delivery
- integration and automation
- data and analytics
- infrastructure and modernization
- digitalization for UMKM
- community/public service platform solutions

### 3. Problem-to-solution journey
The platform should support a customer journey from discovery, inquiry, consultation, assignment, execution, and delivery to ongoing platform engagement.

### 4. Role-based ecosystem
The references suggest a business environment where one platform serves more than a marketing site. The product must support:
- public visitors
- authenticated users
- partner/mitra organizations
- administrators

## Feature inventory
### Public experience
- marketing landing page
- service catalog
- product/platform overview
- portfolio showcase
- about/company story
- contact inquiry form
- search engine for content and public listings
- newsletter/announcement surfaces

### Authenticated platform experience
- profile management
- saved content/favorites
- service requests
- project tracking
- inquiries and communication history
- notifications
- role-specific dashboard and operational flow

## Content and information architecture direction
The content model must be maintainable through admin-managed structures instead of hardcoded marketing content. It should support:
- homepage content blocks
- carousel items
- announcements
- product and service descriptions
- portfolio projects
- about-page sections
- partner data
- testimonials/trust signals

## Business constraints and design decisions
- The platform is not a generic portfolio-only site; it is a technology platform and service marketplace.
- Public pages must use Razor Pages for fast server-rendered experience.
- Complex business flows, dashboards, and management workflows should live in MVC.
- Internal service communication must use gRPC, while public APIs use REST.
- Search must be implemented as an abstraction service, not embedded into pages.
- Authorization must be enforced server-side and not trusted from hidden fields or client state.

## Acceptance alignment
This audit supports the following product direction:
- Public marketing and discovery experience
- Platform logic and workflows for users, partners, and admin
- Secure identity and RBAC foundation
- Search, contact, and notification services
- Maintainable content system and role-specific dashboards

## Risk and guardrails
- Avoid visual cloning from references.
- Avoid mixing public marketing logic with secured platform logic.
- Avoid thin, client-side-only authorization.
- Keep business logic in application and domain layers; minimize logic in Razor pages/controllers.
