# Authorization Flow

1. The request reaches the server boundary.
2. The application reads the authenticated principal and validates the session.
3. The action checks the required role or policy.
4. The system loads the target resource from the data layer.
5. Ownership or administrative authority is validated using the authenticated user identity.
6. Only then are the requested create, read, update, or delete operations allowed.
7. Successful or rejected access is logged to the audit trail.

## Rule ordering
- Step 1: deny by default
- Step 2: validate principal
- Step 3: validate role / policy
- Step 4: validate ownership / scope
- Step 5: validate operation-specific permission
- Step 6: execute
- Step 7: record audit

## Anti-patterns rejected
- Hiding controls in the UI as a security control.
- Trusting route parameters as the source of truth for authorization.
- Using the client to decide whether the user is allowed to access a resource.
- Accepting role values from the request body or auth cookie.
