# Permission Matrix

## Required permissions map
| Permission | Admin | User | Mitra |
| --- | --- | --- | --- |
| ViewPublicContent | Yes | Yes | Yes |
| ManageOwnProfile | Yes | Yes | Yes |
| SubmitInquiry | Yes | Yes | No |
| SubmitServiceRequest | Yes | Yes | No |
| ViewOwnNotifications | Yes | Yes | Yes |
| ViewPlatformDashboard | Yes | Yes | Yes |
| ManageUsers | Yes | No | No |
| ManageMitraProfiles | Yes | No | No |
| ManageServices | Yes | No | Own only |
| ManagePortfolio | Yes | No | Own only |
| ManageSystemSettings | Yes | No | No |
| ManageContentPages | Yes | No | No |
| ManageAnnouncements | Yes | No | No |
| ManageCarousel | Yes | No | No |
| ViewAuditLogs | Yes | No | No |
| ApproveMitra | Yes | No | No |
| AssignInquiryToMitra | Yes | No | No |
| ViewAssignedProject | Yes | No | Yes |
| UpdateProjectStatus | Yes | No | Yes |
| DeleteAuditLogs | No | No | No |

## Enforcement rule
Every resource access decision should evaluate:
- authenticated user identity
- role
- ownership or association
- policy requirement
- resource visibility state

## Ownership authorization examples
- User may access only their profile, requests, and notifications.
- Mitra may access only their own profile and assigned inquiries/projects.
- Admin may access all resources, but audit logs remain immutable.

## Authorization layers
- Page authorization
- Controller authorization
- API authorization
- gRPC authorization
- Database-level ownership validation in service layer
