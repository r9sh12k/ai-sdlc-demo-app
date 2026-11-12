# Proposal

## Why

A review of the org-routing work found defects that let the wrong person act on an invoice or hide feedback from the user. Any signed-in user who knows an invoice ID can submit another person's draft and become its recorded requester. The web UI decides who may approve, reject or submit on its own, so it can disagree with the server. Some errors never appear. Double clicks can create duplicate invoices. The UI also lays a 1,280px table inside a 960px page and mixes pending and decided invoices in one list, which makes the workspace hard to scan.

## What Changes

- Only the requester recorded on a draft may submit it. A denied submit changes nothing.
- Recorded decision attribution and rejection reasons are trimmed before they are stored.
- Sign-in accepts a username in any letter case and with surrounding spaces, and the session uses the canonical username.
- Invoice and approval list items report what the signed-in person may do (`canSubmit`, `canApprove`, `canReject`). The Domain computes these with the same checks the actions use, so `app.js` holds no approval rule.
- The approval list shows the first approver as `username (username@example.com)`, not as a bare username.
- UI fixes: errors from loading project options are visible; buttons are disabled while a request runs, so a double click cannot create two invoices or send two decisions; decision errors appear at the top of the Approvals panel; the Invoices tab is selected after each sign-in.
- UI refresh: a wider responsive layout, a centred sign-in card with the demo usernames, status badges, right-aligned amounts, empty states, counts on the tabs, a confirmation message after each action, approvals split into "Pending" and "Decided" sections, and visible keyboard focus. Existing accessible names, column order and dialog behaviour stay the same.

## Capabilities

### New Capabilities

- `invoice-workspace-ui`: How the web workspace shows invoices, approvals, feedback and available actions to the signed-in person.

### Modified Capabilities

None in `openspec/specs/` (the project has no main specs yet). This change adds requirements to the `invoice-access` and `invoice-approval-routing` capabilities introduced by the in-flight `add-org-routing` change, as ADDED deltas under the same capability paths.

## Impact

- Domain: `ApprovalService` (submit ownership, trimming, `CanSubmit`/`CanApprove`/`CanReject`).
- API: `AuthController` sign-in matching, `InvoicePresentation` list item fields, shared session-to-`ApproverRole` mapping used by both controllers.
- Web UI: `wwwroot/index.html`, `app.js`, `styles.css`.
- Tests: xUnit coverage for each rule; Playwright coverage for the new UI behaviour, with page objects updated to match.
- No new dependencies. The JSON list item gains fields and `firstApprovedBy` now uses the display format. `waitingOnUsername` is removed because the UI no longer needs it.
