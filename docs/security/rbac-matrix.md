# RBAC Matrix

| Role | Summary | Allowed high-level actions |
| --- | --- | --- |
| Anonymous | Public visitors | Read public content, browse public catalog, submit contact inquiry |
| User | End-user account | Manage own profile, read own submissions, create/update own portfolio items as allowed |
| Mitra | Business partner | Manage business profile, create/update own services, review assigned inquiries |
| Admin | Platform administrator | Manage users, approve partners, review all audit trails, publish platform content |

## Enforcement model
- Anonymous is allowed only for public actions.
- User, Mitra, and Admin are all denied by default unless a policy allows the action.
- Admin actions must be validated both by role and by resource policy.
- Ownership checks are required before any user or partner can access another account's data.
