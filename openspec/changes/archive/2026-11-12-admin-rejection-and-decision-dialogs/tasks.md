# Tasks

## 1. Authorize Admin rejection

- [x] 1.1 Update the controller test to prove an Admin can reject a pending invoice with a reason, while unauthorized roles remain denied; update the Reject role allow-list and verify with `dotnet test backend/InvoiceApp.sln`.
- [x] 1.2 Update `README.md`, `AGENTS.md`, and `.github/skills/approval-rules/SKILL.md` so each documents that Admins may reject with a reason; verify no contradictory permission guidance remains.

## 2. Add decision dialogs

- [ ] 2.1 Add Playwright coverage for approving/rejecting after confirmation, cancelling without changing invoice state, and an empty or whitespace-only reason showing an error dialog without submission; verify with `npx playwright test`.
- [ ] 2.2 Add accessible confirmation and missing-reason dialogs to the UI, validate the rejection reason before confirmation, and submit only after confirmation; update the Playwright page object and verify the focused scenarios with `npx playwright test`.

## 3. Verify integrated behavior

- [ ] 3.1 Run `dotnet test backend/InvoiceApp.sln` and `npx playwright test`; verify Admin rejection succeeds only with a reason, decision cancellations leave invoices pending, and confirmed decisions complete.
