# Design

## Context

See `proposal.md` and the three capability specs. The application is an ASP.NET Core API with a static UI, an in-memory invoice repository, and authentication users configured separately from invoices. `ApprovalService` currently owns invoice lifecycle and amount rules; invoice creation and list filtering are currently in the API controller.

## Goals / Non-Goals

**Goals:**
- Keep organization-based eligibility, routing, and decision rules in Domain code.
- Make the assigned route available to authorization checks and list views.
- Enforce project and role scope on the server, not only in the UI.
- Retain the in-memory demo workflow and existing amount boundaries.

**Non-Goals:**
- Add persistent storage or an organization-management interface.
- Change existing amount limits, self-approval protections, or high-value two-person approval.
- Let Admin bypass the route to approve an invoice at or below 500,000.00.

## Decisions

- **Add Domain organization types and a repository interface, with an in-memory API implementation.** Represent Business Units, Accounts, Projects, and allocations in Domain-facing types; seed the demo hierarchy in the API. This keeps policy independent of storage and avoids a database migration. Keep authentication credentials in the existing user configuration and associate them to organization records by username.
- **Resolve and snapshot the approver route on submission.** Store the routed first and second usernames separately from actual approval attribution. This makes `Approval 1`, `Approval 2`, and `Waiting on` stable for an in-flight invoice and prevents later organization changes from silently changing who may act. Use the existing first-approval and decision fields for actions actually taken.
- **Keep lifecycle and routing decisions in Domain services.** Extend the existing approval workflow to assign and enforce routes using the organization directory. Controllers map the signed-in session to Domain identity and translate service outcomes through the existing `Handle(...)` mechanism; they do not choose approvers. A route failure should be detected before leaving Draft and returned as `No second approver available`.
- **Treat the Admin as a route fallback, not an ordinary route bypass.** A low- or mid-value invoice can be approved by Admin only when routed to Admin; Admin may complete a distinct routed second step above 500,000.00, and may reject any pending invoice with a reason. Other approvals require the current routed identity.
- **Expose only scoped project choices and lists.** Add a signed-in project-options API for the create form and an approval-list API for pending invoices in the caller's scope plus invoices they decided. Apply the same scope policy to invoice-list API results so hiding a row in JavaScript is never the access-control boundary.
- **Render identities from directory data.** Use the username as the display name because no separate display names are provided, and format identities as `username (username@example.com)`. Keep existing invoice ID, customer, and amount columns while adding the requested project, requester, routed approvers, waiting target, and rejection attribution.

## Risks / Trade-offs

- [Invoice seeds or existing clients may omit a project] → Update the demo invoice seeds and create request/UI together; reject requests without a valid allocated project.
- [Requester exclusion and upward escalation could select the same person twice] → Resolve both high-value steps as a pair, enforce distinct normalized usernames, and use Admin for a duplicate second step.
- [Directory membership may change while invoices are pending] → Snapshot route assignments at submission; current requests and scope checks still use the caller's current identity and organizational access.
- [A misconfigured organization may not yield a second approver] → Fail before submission changes the Draft invoice and return the specified `No second approver available` message.
- [A user may have both project and Business Unit roles] → Combine authorized invoice scopes, while approval remains limited to the currently routed person.

## Migration Plan

No persistent data migration is needed. The in-memory store is initialized from the updated demo organization and invoice fixtures on application start. Roll back by reverting the application change.
