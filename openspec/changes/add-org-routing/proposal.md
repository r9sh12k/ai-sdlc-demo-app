# Proposal

## Why

Invoices currently have no project ownership, and every signed-in user can see the same invoice and approval lists. This change adds the organization structure needed to route decisions to accountable project and business-unit approvers while limiting invoice creation and visibility to the user's scope.

## What Changes

- Model Business Units, Accounts, Projects, project allocations, Business Unit heads, and optional Delivery Managers (DMs); seed the requested demo organization and users.
- Require a project for invoice creation and require the creator to be allocated to that project; a project DM counts as allocated.
- Route invoices by amount and organization role, exclude the requester and lower-ranked candidates, enforce distinct high-value approvers, and use Admin as the specified fallback.
- Scope approval and invoice lists by Requester, DM, Business Unit head, and Admin; require the current routed approver to approve, while allowing Admin to reject any pending invoice with a reason.
- Show project, requester, routed approval steps, current wait target, status, and rejection attribution in the UI.
- Update README and approval guidance, and add tests across the domain, API, and end-to-end UI.

## Capabilities

### New Capabilities

- `organization-directory`: Business Unit > Account > Project hierarchy, heads, DMs, project allocations, and the demo organization.
- `invoice-approval-routing`: Amount-based approver routing, upward escalation, distinct approval steps, and routed decision permissions.
- `invoice-access`: Project-restricted invoice creation and role-scoped invoice and approval workspaces.

### Modified Capabilities

None. The repository has no main OpenSpec capability specs; the related approval behavior currently appears only in other change deltas.

## Impact

- Domain invoice and organization models, repository interfaces, routing/decision rules, and in-memory API implementations.
- API authentication/demo-user data, invoice creation, approvals, and scoped list endpoints.
- Static UI project selection, approval queue, and invoice table.
- README and agent approval guidance, plus xUnit and Playwright coverage.

No external database or dependency is required; the application remains an in-memory demo.
