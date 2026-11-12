# Tasks

## 1. Domain rule fixes

- [x] 1.1 Test first: a non-requester submit is denied with `Only the requester who created this draft can submit it.`, the invoice is unchanged and `UpdateCount` is 0; the requester's own submit (any letter case) still succeeds. Add the ownership check to `ApprovalService.Submit`, then verify with `dotnet test backend/InvoiceApp.sln`.
- [x] 1.2 Test first: approve and reject store a trimmed `DecidedBy`, and reject stores a trimmed `DecisionNote`. Implement, then verify with `dotnet test backend/InvoiceApp.sln`.
- [x] 1.3 Test first: `CanSubmit`, `CanApprove` and `CanReject` agree with the actions for requester, non-requester, routed approver, unrouted approver, Admin (routed and unrouted), Requester role (`null`), decided invoices and the high-value first approver. Refactor the actions to share the same denial methods, then verify with `dotnet test backend/InvoiceApp.sln`.

## 2. API

- [x] 2.1 Test first: sign-in accepts ` Demo.Manager ` and returns `demo.manager`; a wrong password still returns 401. Implement in `AuthController`, then verify with `dotnet test backend/InvoiceApp.sln`.
- [x] 2.2 Test first: invoice and approval list items return `canSubmit`, `canApprove` and `canReject` for the caller and a formatted `firstApprovedBy`; `waitingOnUsername` is gone. Add `SessionRoles.ToApproverRole`, use it in both controllers, update `InvoicePresentation`, then verify with `dotnet test backend/InvoiceApp.sln`.

## 3. Web UI

- [x] 3.1 Use the server's action flags for Submit, Approve and Reject; disable each button while its request runs; show project-load and decision errors in visible alerts; announce successes in a status region; reset to the Invoices tab on sign-in; split approvals into `Pending` and `Decided`. Update the page objects and add Playwright coverage for: a DM sees no Submit on a requester's draft, double-clicking Create invoice adds one invoice, the created-invoice status message, the Pending/Decided grouping, the formatted first approver, and the tab reset. Verify with `npx playwright test`.
- [x] 3.2 Restyle the workspace: a wider responsive layout, a sign-in card with the demo usernames, status badges, right-aligned amounts, empty states, tab counts and visible focus. Verify that the full Playwright suite passes and that, in a 1280px window, the table needs no page-wide horizontal scroll.

## 4. Integration

- [x] 4.1 Run `dotnet test backend/InvoiceApp.sln` and `npx playwright test` against a freshly started app; update README if user-visible behaviour it describes has changed.
