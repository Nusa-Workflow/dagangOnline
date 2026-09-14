# REST vs gRPC Boundary

## REST API responsibilities
Use REST for client-facing and integration-facing operations.
Examples:
- /api/v1/auth
- /api/v1/users
- /api/v1/mitra
- /api/v1/services
- /api/v1/products
- /api/v1/portfolio
- /api/v1/contact
- /api/v1/search
- /api/v1/notifications

REST API principles:
- versioned routes: /api/v1/...
- DTO-driven contracts
- OpenAPI/Swagger documentation
- validation and ProblemDetails
- consistent HTTP status handling
- pagination and filtering support
- authorization filters and policies

## gRPC responsibilities
Use gRPC for internal application-to-application communication where strong typing, low overhead, and clear contracts are valuable.
Examples:
- UserService
- MitraService
- PortfolioService
- ServiceCatalogService
- SearchService
- NotificationService
- ContactService

gRPC principles:
- internal communications only
- no business logic duplication between REST and gRPC
- domain/application service as single source of truth
- use the same validation and authorization pipeline

## Boundary guidance
- Public website and dashboards call application services directly.
- REST is exposed to client-side or external integrations.
- gRPC is used for internal service-to-service communication or high-throughput internal queries.

## Avoided anti-pattern
Do not duplicate business logic in both REST and gRPC endpoints; both should delegate to the same application/domain services across the same authorization and validation rules.
