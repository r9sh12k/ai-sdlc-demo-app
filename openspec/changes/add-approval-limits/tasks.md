# Tasks

## 1. Single-approver limits, API denial mapping, and demo users

- [x] 1.1 Implement Manager and Director single-approval limits in Domain and add unit tests for 50,000.00, 50,000.01, 500,000.00, and 500,000.01; verify with `dotnet test backend/InvoiceApp.sln`.
- [x] 1.2 Map session roles to Domain approver types, map `ApprovalDeniedException` to `403 { "error": reason }`, and configure Manager, Director, and Admin demo users; add API tests and verify the solution tests pass.

## 2. Distinct second approval for high-value invoices

- [x] 2.1 Record the first approval without changing `PendingApproval`, then require a distinct Director to complete approval; add unit tests for same-user casing/whitespace, a Manager as second approver, unchanged invoice fields, and no repository update on denial; verify with `dotnet test backend/InvoiceApp.sln`.

## 3. UI explanation and end-to-end coverage

- [x] 3.1 Show the next-approver requirement for high-value invoices and add one Playwright test using the page object and role-based locators; verify with `npx playwright test`.

## 4. Requester access and self-approval prevention in Domain and API

- [ ] 4.1 Add the Requester role's create, submit, and view permissions and deny its approve and reject actions; record the signed-in username as `SubmittedBy` on submission; add focused API tests for permitted and denied actions.
- [ ] 4.2 Deny approval by the submitter at either approval stage, regardless of role or amount, using trimmed, case-insensitive username matching before amount-limit checks; preserve invoices without `SubmittedBy`; assert the exact denial reason, unchanged invoice fields, and no repository update on denial, with Domain and API tests.

## 5. Approver attribution in the UI

- [ ] 5.1 Preserve `FirstApprovedBy` after the second approval and show `Submitted by <name>` and `Approved by <name>` or `Approved by <first> and <second>` in the invoice list; add focused UI and Playwright coverage.
