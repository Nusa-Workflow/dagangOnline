# gRPC Security

The platform must enforce the same rules for gRPC as for REST endpoints.

## Mandatory rules
- Reject unauthenticated calls to protected services by default.
- Validate caller identity metadata before performing any service action.
- Authorize by resource owner, not by client-supplied identifiers.
- Require policy checks for admin-only actions and sensitive updates.
- Log every privileged operation.

## Example pattern
- Read: permit if public or owner/admin
- Write: require operation permission plus resource ownership or admin status
- Delete: require explicit delete permission and ownership check

## Deny-by-default posture
If a gRPC method does not explicitly declare a policy, it must fail closed with an authorization error.
