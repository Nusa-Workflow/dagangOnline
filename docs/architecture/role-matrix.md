# Role Matrix

## Role definitions
### Admin
Responsible for full platform governance and operational oversight.
Primary permissions:
- manage users
- manage mitra profiles
- manage services and products
- manage portfolio
- manage contact requests and inquiries
- manage announcements and carousel content
- manage CMS pages
- manage system settings
- manage role assignment and permissions
- view audit logs
- health and platform diagnostics

### User
Registered end-user and customer.
Primary permissions:
- view public content
- manage own profile
- submit inquiries and service requests
- save or favorite content
- view own request history
- manage own notification preferences
- access user dashboard

### Mitra
Partner/provider operating within the ecosystem.
Primary permissions:
- manage own profile and organization info
- manage own service catalog entries
- manage own portfolio projects
- respond to assigned inquiries
- manage assigned project status
- access mitra dashboard
- view relevant notifications and assignments

## Role boundary summary
| Role | Public Content | Protected Data | Admin APIs | Partner Data | User Data | Dashboard |
| --- | --- | --- | --- | --- | --- | --- |
| Guest | Read | No | No | No | No | No |
| User | Read | Own profile/request history | No | No | Own only | User |
| Mitra | Read | Own organization and assigned work | No | Own and assigned | No | Mitra |
| Admin | Read | Entire platform | Yes | Entire platform | Entire platform | Admin |

## Dashboard routing
- /dashboard/user
- /dashboard/mitra
- /admin/dashboard

These routes must not be the same experience with hidden widgets; they must be separate user flows and authorization boundaries.

## Role security principle
Roles determine server-side authorization scopes. UI hiding is not a security control.
