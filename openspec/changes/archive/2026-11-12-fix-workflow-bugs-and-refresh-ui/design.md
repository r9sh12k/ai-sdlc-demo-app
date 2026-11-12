# Design

## Context

`ApprovalService.Submit` checks only that the invoice is a draft. `InvoiceAccessService.CreateDraft` already records the creator as `SubmittedBy`, so a draft has an owner before it is submitted. `app.js` compares `waitingOnUsername` with the session username to decide whether to show Approve, and checks the role string `Admin` to decide whether to show Reject. That duplicates the Domain rules. `InvoicePresentation.ToListItem` builds list items from the invoice, the org directory and `InvoiceAccessService`, and does not know who is viewing. The role mapping from session role to `ApproverRole` is private to `ApprovalsController`.

## Goals / Non-Goals

**Goals:**
- One source of truth for "may this person submit/approve/reject this invoice", in the Domain, used both to enforce actions and to build list items.
- Keep the existing exceptions, HTTP mappings and error texts for every existing denial.
- Keep every accessible name, the column order and the dialog flow that the Playwright suite and page objects rely on.

**Non-Goals:**
- No change to routing, thresholds, scope rules or demo data.
- No frontend framework, build step or new dependency; the UI stays plain HTML, CSS and JavaScript.
- No change to the open task 5.1 in `add-approval-limits` ("Approved by" wording), which conflicts with the later Approval 1/Approval 2 columns.

## Decisions

### Permission checks return a denial reason instead of throwing
`ApprovalService` gains private `SubmitDenial`, `ApprovalDenial` and `RejectionDenial` methods that return the denial reason, or `null` when the action is allowed. `Submit`, `Approve` and `Reject` throw `ApprovalDeniedException` with that reason. The new public `CanSubmit`, `CanApprove` and `CanReject` return `true` when the status is right and the reason is `null`. Status checks keep throwing `InvalidOperationException` (409) in the actions.
- *Alternative:* call the action in a try/catch for each list item. Rejected, because it uses exceptions for normal control flow and could save state by mistake.
- *Alternative:* a separate permissions class. Rejected, because the rules would then live in two classes that can drift apart.

### Ownership uses the recorded requester when there is one
`SubmitDenial` refuses when `SubmittedBy` is set and is not the caller. A draft with no recorded requester (only seeded or test data; `CreateDraft` always sets one) can still be submitted by its first submitter, which keeps the existing behaviour for those records.

### Viewer is passed to presentation as Domain types
Controllers map the session to `(string username, ApproverRole? role)` with a shared `SessionRoles.ToApproverRole` in `Auth/`. The Requester role maps to `null`. `InvoicePresentation.ToListItem` takes the `ApprovalService` and this viewer and fills `CanSubmit`, `CanApprove` and `CanReject`. `Approve` still rejects a `null` role with the existing "Only Managers, Directors, and Admins may approve invoices." text.

### `waitingOnUsername` is dropped; `firstApprovedBy` is formatted
Nothing else reads the raw username, and keeping it would invite the UI to rebuild the rule again. `firstApprovedBy` uses `FormatPerson`, as the display rule requires.

### Sign-in normalises the typed username
`AuthController.Login` trims the typed username and matches it ignoring case. The password comparison stays exact. The response and session use the configured username.

### UI structure
- A single `busy(button, task)` helper disables a button while its request runs.
- Each panel has a `role="status"` live region for success messages and a `role="alert"` region for errors, placed where the user is looking: the project-load error sits outside the create form, and the decision error sits above the approval lists.
- Approvals render into `#pending-list` and `#decided-list` under `Pending` and `Decided` headings. Items stay `listitem`s, so the existing filters still work.
- Tab labels show counts in a separate `<span>` (`Approvals 2`). Playwright's default substring name match for `{ name: 'Approvals' }` still finds the tab.
- Status cells keep their visible text (`Pending Approval`) inside a badge `<span>`, so `toHaveText` still matches.
- `main` widens to 1,320px, and the table drops its 1,280px minimum width in favour of fixed column sizing and wrapping.

## Risks / Trade-offs

- [The tab name with a count no longer equals `Approvals` exactly] → Page objects use substring matching. New tests match `/^Approvals/`.
- [Removing `waitingOnUsername` changes the API] → Only `app.js` uses it. Controller tests are updated in the same change.
- [Drafts with no requester stay open to the first submitter] → Only seed and test data can have none. `CreateDraft` always records one, and a Domain test pins that.
