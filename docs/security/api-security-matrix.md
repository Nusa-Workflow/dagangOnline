# API Security Matrix

| Surface | Anonymous access | Authenticated user | Mitra | Admin |
| --- | --- | --- | --- | --- |
| Public catalog read | Allowed | Allowed | Allowed | Allowed |
| User profile | Denied | Own only | Own only | All |
| Partner profile | Denied | Denied | Own only | All |
| Contact submission | Allowed | Allowed | Allowed | Allowed |
| Inquiry detail | Denied | Own only | Own or assigned | All |
| Platform audit | Denied | Denied | Denied | Allowed |

## API rules
- All API actions use server-side authorization.
- All resource IDs are treated as untrusted and must be validated against the current principal.
- Role claims are not used as a resource authorization source.
- All high-risk write actions must be audited.
