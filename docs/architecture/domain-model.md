# Domain Model

## Core entities
### User
Represents authenticated system users.
Attributes:
- Id
- UserName
- Email
- EmailConfirmed
- PhoneNumber
- CreatedAt
- UpdatedAt
- IsActive
- Profile relationship
- Role assignments

Rules:
- A user can have at most one active profile at a time.
- Identity and authorization decisions must be server-side.

### Role
Domain-level role definitions:
- Admin
- User
- Mitra

Rules:
- Roles are assigned through Identity and policy evaluation.
- A role is a permission boundary, not just a UI toggle.

### Permission
Represents authorization units used in policy checks.
Examples:
- ManageUsers
- ManagePortfolio
- ManageServices
- ViewAdminDashboard
- ManageMitraProfile
- ApproveInquiry

### UserProfile
Public and personal data for a standard user.
Attributes:
- DisplayName
- Username
- AvatarUrl
- Bio
- Contact info
- Preferences
- Notification preferences

### MitraProfile
Partner profile entity for organizations or providers.
Attributes:
- CompanyName
- LogoUrl
- Description
- Website
- BusinessCategory
- Location
- Assigned status
- Partnership information

Rules:
- Ownership is validated against the authenticated MitraProfile owner.
- Mitra profile updates require server-side authorization.

### Service
Business service offering for public or partner consumption.
Attributes:
- Title
- Description
- Category
- Status
- Availability
- TargetAudience
- Pricing / status metadata
- CreatedBy / OwnedBy

### Product
Platform or solution product offering.
Attributes:
- Name
- Description
- Category
- Status
- Tags
- Features
- Related services

### PortfolioProject
Project showcase item.
Attributes:
- Title
- Summary
- Description
- Problem
- Solution
- Result
- Client/Organization name
- ProjectCategory
- Industry
- StartDate / EndDate
- Status
- IsPublished
- Media references

Rules:
- Public portfolio must respect publication state and ownership scope.
- Admin can manage all projects; Mitra can manage its own projects.

### PortfolioTechnology
Join/metadata entity for technologies used in a portfolio project.
Attributes:
- PortfolioProjectId
- TechnologyName
- Category

### ContactInquiry
Public inquiry form record.
Attributes:
- Name
- Email
- Subject
- Message
- Category
- Organization
- ConsentAccepted
- Status
- CreatedAt
- AssignedMitraId

Rules:
- Only authorized users or assigned partners may access or update specific inquiries.
- Status transitions are domain-controlled.

### ServiceRequest
Request or purchase intent from user or organization.
Attributes:
- UserId
- ServiceId
- Title
- Details
- Status
- Priority
- CreatedAt
- UpdatedAt

### Notification
Notification record for user, admin, or mitra events.
Attributes:
- RecipientUserId
- RecipientRole
- Message
- Type
- IsRead
- CreatedAt

### Announcement
Platform-level public announcement.
Attributes:
- Title
- Content
- PublishedAt
- Status
- Audience

### CarouselItem
Featured content or highlight for home page.
Attributes:
- Title
- Subtitle
- LinkUrl
- ImageUrl
- Type
- Sequence
- Status

### ContentPage
Admin-managed page content.
Attributes:
- Slug
- Title
- Content
- Status
- LastUpdatedBy

### AuditLog
Security and governance trail.
Attributes:
- ActorUserId
- ActorRole
- Action
- EntityType
- EntityId
- Timestamp
- Result
- RequestMetadata

Rules:
- Admin must not be allowed to delete audit records.
- Sensitive changes must be logged.

## Relationships
- User 1:1 UserProfile
- User many-to-many Role
- MitraProfile 1:many Service
- MitraProfile 1:many PortfolioProject
- User 1:many ServiceRequest
- Service 1:many ServiceRequest
- PortfolioProject many-to-many PortfolioTechnology
- ContactInquiry optional assignment to MitraProfile
- User 1:many Notification
- ContentPage 1:many Announcement / CarouselItem

## Domain invariants
- Ownership is always resolved at the server boundary.
- Publication status determines public visibility.
- Role assignment determines dashboard access and operational permissions.
- Sensitive operations require audit logging.
