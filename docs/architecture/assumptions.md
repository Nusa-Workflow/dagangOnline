# Key Assumptions

## 1. Public website and platform are separate but connected experiences
The public marketing experience is designed for discovery and conversion; the authenticated platform experience is designed for operational workflows.

## 2. Identity is the primary trust boundary
All authorization goes through ASP.NET Core Identity and policy-based authorization. Client-side state is not trusted for access control.

## 3. PostgreSQL is the source of persisted truth
EF Core and PostgreSQL will be used as the main persistence layer for the application and platform data.

## 4. Content is managed from the database instead of code
Marketing and site content can be administered through admin-managed entities such as ContentPage, Announcement, CarouselItem, and Service/Product records.

## 5. Search is an abstraction and not a page concern
ISearchService provides an abstraction with an initial PostgreSQL implementation, allowing future migration to Elasticsearch/OpenSearch or a dedicated search service.

## 6. Notifications, storage, and contact flows are service-based
Notifications will be managed through a dedicated notification abstraction. File storage uses IFileStorageService with future compatibility for S3 or Azure Blob storage.

## 7. Dashboard routes are purposefully separate
The application will implement separate dashboard endpoints for user, mitra, and admin flows to avoid role confusion and to preserve proper scope boundaries.

## 8. Audit logs are immutable and security-sensitive
Audit logging participates in security and governance controls and cannot be treated as normal editable content.

## 9. The first implementation gate focuses on correctness and build health
The initial implementation phase prioritizes a working .NET solution, clean build, and passing tests before additional platform features are added.

## 10. Product quality is prioritized over cosmetic completion
The architecture and authorization model are as important as the final interface, especially for PaaS and role-based multi-tenant behavior.
