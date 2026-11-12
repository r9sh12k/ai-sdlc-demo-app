# Proposal

## Why

Approvals currently allow any configured approver to decide an invoice, regardless of amount. Role-based limits and a second approval for high-value invoices make the demo's approval workflow more realistic and accountable.

## What Changes

- Replace the single `Approver` role with `Manager` and `Director` approval limits; Admins cannot approve.
- Require a distinct Director after the first Manager or Director approval for amounts above 500,000.00.
- Return actionable denial errors without changing the invoice.
- Add demo users, a UI explanation for the second-approval flow, and focused unit and Playwright coverage.

## Capabilities

### New Capabilities
- `invoice-approval`: Amount- and role-based invoice approval, including distinct two-person approval for high-value invoices.

### Modified Capabilities

## Impact

Approval rules affect `ApprovalService` and invoice state in the Domain project; the API maps denial exceptions to HTTP 403; demo-user configuration and the static approval UI change; unit and Playwright tests cover the behavior.
