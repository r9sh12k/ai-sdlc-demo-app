# Design

## Context

See `proposal.md` for motivation and `specs/invoice-approval/spec.md` for behavior. `ApprovalService` currently approves a pending invoice in one operation; the API delegates through `Handle(...)`, and the UI displays request errors.

## Goals / Non-Goals

**Goals:**
- Keep approval policy in the Domain layer and validate the role, amount, and prior approver before changing invoice state.
- Preserve `PendingApproval` after the first high-value approval and expose the required next approver through the UI.

**Non-Goals:**
- Prevent races between concurrent approval attempts.

## Decisions

- **Manager and Director replace Approver; Admin cannot approve.** Map the signed-in session to Domain approver types in the controller. This keeps role strings and HTTP concerns out of Domain; retaining a generic Approver role would not express the different limits.
- **Record the first high-value approver while status remains PendingApproval.** Store `FirstApprovedBy` on the invoice and require a second, distinct Director. This fits the existing lifecycle; adding an intermediate status would change current status handling unnecessarily.
- **Use Domain `ApprovalDeniedException` for policy denials and map it to HTTP 403 `{ "error": reason }`.** This distinguishes authorization denials from existing 400/404/409 service mappings and gives the UI an actionable message.
- **Compare usernames after trimming and ignoring case.** This prevents spelling/casing variations from counting the same person twice without requiring a new identity system.

## Risks / Trade-offs

- [Concurrent approvals may race and violate the two-person rule] -> Known risk; concurrency control is out of scope.
- [Existing approval clients may still use the Approver role] -> Update demo users and approval UI together with the API behavior.
