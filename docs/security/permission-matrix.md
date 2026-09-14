# Permission Matrix

| Permission | Scope | Description |
| --- | --- | --- |
| users.read | Admin | Read user records |
| users.create | Admin | Create user accounts |
| users.update | Admin | Update other users |
| users.delete | Admin | Remove user accounts |
| mitra.read | Admin / Mitra | Review partner records |
| mitra.update | Mitra | Update own partner profile |
| mitra.approve | Admin | Approve or reject partner status |
| portfolio.read | Public / User / Mitra / Admin | Read portfolio content |
| portfolio.create.own | User / Mitra | Create portfolio content for self |
| portfolio.update.own | User / Mitra | Modify own portfolio content |
| portfolio.delete.own | User / Mitra | Remove own portfolio content |
| portfolio.publish | Admin | Publish platform-level portfolio items |
| services.read | Public | Read service catalog |
| services.create.own | Mitra | Create service offers for self |
| services.update.own | Mitra | Modify own service offers |
| services.delete.own | Mitra | Remove own service offers |
| contact.read.own | User / Mitra / Admin | Read own inquiries or assigned contact items |
| contact.update.own | User / Mitra | Update own inquiry state |
| contact.assign | Admin / Mitra | Assign or route inquiries |
| profile.read.own | User / Mitra | Read own profile |
| profile.update.own | User / Mitra | Update own profile |
| audit.read | Admin | View platform audit logs |

This permission list is centralized in [Authorization/PermissionConstants.cs](../../Authorization/PermissionConstants.cs) and should be treated as the source of truth for server-side access checks.
