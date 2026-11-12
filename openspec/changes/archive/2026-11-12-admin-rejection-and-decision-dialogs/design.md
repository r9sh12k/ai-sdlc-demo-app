# Design

## Context

The reject endpoint permits only Managers and Directors, while approval also permits Admins. The Domain already rejects empty or whitespace-only reasons. The static UI currently submits decisions immediately and reports decision errors inline; it already uses a native `<dialog>` for permission-denied feedback.

## Goals / Non-Goals

**Goals:**
- Add Admin to the reject endpoint's existing role allow-list.
- Require confirmation for both decisions and show a modal error for an empty rejection reason.
- Keep declined confirmations and invalid reasons from making API requests.

**Non-Goals:**
- Change invoice lifecycle rules, reason persistence, or existing Manager/Director permissions.
- Change authorization for any action other than invoice rejection.

## Decisions

- **Extend only the reject action's role allow-list.** The controller-level `RequireRole` attribute is the authorization point for rejection; adding Admin there fixes the observed denial without moving role policy into the Domain or changing the approve endpoint.
- **Keep role guidance aligned with API behavior.** Update the README and the project/approval-rule agent guidance so they no longer tell future contributors that Admin rejection is forbidden.
- **Use native modal dialogs in the existing UI.** Add accessible confirmation and missing-reason dialogs following the existing permission-dialog pattern, rather than browser `confirm()`/`alert()` calls or inline errors. This keeps action behavior consistent with the current accessible dialog and lets cancellation leave the request untouched.
- **Validate the reason before opening the rejection confirmation.** Treat empty and whitespace-only input as missing, show the error dialog, and make no request. For a non-empty reason, retain the entered value when sending the confirmed rejection.
- **Cover authorization and user-visible outcomes at their boundaries.** Add a controller test for Admin rejection and focused Playwright tests for confirmed/cancelled decisions and missing-reason feedback.

## Risks / Trade-offs

- [A role change could accidentally expand other permissions] → Change only the Reject action's role attribute and verify the other role restrictions remain intact.
- [Dialog flow could submit more than once or submit after cancellation] → Keep each confirmation bound to its invoice action and assert pending state after cancellation in Playwright tests.

## Migration Plan

No data migration or API contract change is required. Deploy normally; rollback by reverting the application change.
