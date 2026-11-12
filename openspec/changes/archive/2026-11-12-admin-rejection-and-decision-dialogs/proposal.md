# Proposal

## Why

Admins cannot currently reject pending invoices, despite the requested rejection workflow. Approve and Reject actions also execute immediately, and a missing rejection reason is reported inline rather than through an error dialog.

## What Changes

- Allow Admins to reject pending invoices when they provide a non-empty reason, without changing existing Manager and Director rejection behavior.
- Require confirmation before an approval or rejection is submitted.
- Show an error dialog and do not submit a rejection when its reason is empty or whitespace.

This follow-up supersedes the earlier requirement that Admins cannot reject invoices.

## Capabilities

### New Capabilities

- `invoice-approval`: Define authorized rejection of pending invoices by Admins, with a required reason.
- `decision-confirmations`: Define confirmation and missing-reason dialogs for approval decisions.

### Modified Capabilities

None. The project has no main OpenSpec capability specs; the related `invoice-approval` behavior is currently described in a completed change delta.

## Impact

- Approval/rejection role authorization in the API.
- Static approval UI markup and JavaScript for accessible confirmation and error dialogs.
- README and agent guidance that describe Admin rejection permissions.
- Controller and Playwright coverage for Admin rejection, confirmation/cancellation, and missing reasons.
