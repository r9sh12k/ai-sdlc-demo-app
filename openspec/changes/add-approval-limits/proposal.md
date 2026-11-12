# Proposal

## Why

Approvals currently allow any configured approver to decide an invoice, regardless of amount, and do not prevent the submitter from approving their own invoice. Role-based limits, a second approval for high-value invoices, and clear requester and approver attribution make the demo's workflow more realistic and accountable.

## What Changes

- Replace the single `Approver` role with `Manager` and `Director` approval limits; Admins cannot approve.
- Add a `Requester` role that can create, submit, and view invoices, but cannot approve or reject them.
- Record the submitter and prevent them from approving the invoice at either approval stage, regardless of role or amount.
- Require a distinct Director after the first Manager or Director approval for amounts above 500,000.00.
- Return actionable denial errors without changing the invoice.
- Preserve and display submitter and approver names in the invoice list, including both approvers for high-value invoices.
- Add demo users, a UI explanation for the second-approval flow, and focused unit and Playwright coverage.

## Capabilities

### New Capabilities
- `invoice-approval`: Amount- and role-based invoice approval, including distinct two-person approval for high-value invoices.

### Modified Capabilities

## Impact

Approval rules affect `ApprovalService` and invoice state in the Domain project; the API records the signed-in submitter and maps denial exceptions to HTTP 403; demo-user configuration and the static invoice UI change; unit, API, and Playwright tests cover the behavior.
