# Tasks

## 1. Enable Admin approval with Director rules

- [x] 1.1 Update Domain approval authorization so Admins follow Director limits and high-value distinct-approver rules; add focused tests for approval through 500,000.00, first approval above 500,000.00, distinct second approval, self-approval denial, and unchanged invoice state on denials; verify with `dotnet test backend/InvoiceApp.sln`.
- [x] 1.2 Scope API role filters so Admins may approve but remain unable to reject; add controller tests for eligible Admin approval and forbidden Admin rejection; verify with `dotnet test backend/InvoiceApp.sln`.
- [x] 1.3 Update `AGENTS.md` and `.github/skills/approval-rules` to reflect Admin's Director-equivalent approval authority and unchanged rejection restriction; verify no policy text still says Admin approval is always denied.

## 2. Show permission denials in a modal

- [x] 2.1 Replace the page-level permission-denied display with a dismissible accessible modal; handle the existing role-denied response separately from other decision errors, and clear the message on dismissal and session changes; verify modal behavior with focused Playwright coverage.
- [x] 2.2 Update `README.md` to document Admin approval under Director limits and the temporary permission-denied modal; verify the documented role behavior matches the capability specs.

## 3. Verify integrated behavior

- [x] 3.1 Run `dotnet test backend/InvoiceApp.sln` and `npx playwright test`; verify Admin approval and rejection outcomes, modal dismissal, and no permission message remains after sign-out or a subsequent login.
