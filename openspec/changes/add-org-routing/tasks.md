# Tasks

## 1. Organization model and demo data

- [ ] 1.1 Add Domain organization and allocation types plus the in-memory directory for Business Units, Accounts, Projects, heads, DMs, and project roles; add focused tests for the hierarchy, demo assignments, and DM allocation, then verify with `dotnet test backend/InvoiceApp.sln`.
- [ ] 1.2 Add `demo.manager2` and `demo.requester2`, expose `<username>@example.com` identity data, and update README, AGENTS.md, and `.github/skills/approval-rules/SKILL.md` with the demo hierarchy and routing policy; verify with focused tests and check the guidance for conflicting rules.

## 2. Routing and decisions

- [ ] 2.1 Add test-first route coverage for 50,000.00, 50,000.01, 500,000.00, and 500,000.01; missing DMs; requester/lower-rank escalation; duplicate assignees; Admin fallback; and a missing distinct second approver. Implement route assignment and snapshot it on submission; verify with `dotnet test backend/InvoiceApp.sln`.
- [ ] 2.2 Add test-first decision coverage for only the current routed approver approving, Admin approval only when routed (including a distinct high-value second step), Admin rejection of any pending invoice, required rejection reason, and unchanged invoice state on denials; verify with `dotnet test backend/InvoiceApp.sln`.

## 3. API and lists

- [ ] 3.1 Add an authenticated project-options endpoint and require `ProjectId` on invoice creation; test that only allocated users can create for a project and that denied requests add no invoice, then verify with `dotnet test backend/InvoiceApp.sln`.
- [ ] 3.2 Scope invoice and approval-list API results by Requester, DM, Business Unit head, and Admin; include pending invoices in scope plus decisions by the caller, return `Waiting on` and the required display fields, and test that out-of-scope invoices are not exposed; verify with `dotnet test backend/InvoiceApp.sln`.

## 4. UI and end-to-end coverage

- [ ] 4.1 Add an allocated-project selector, role-scoped approval view, and invoice columns for project, requester, routed approvers, waiting target, status, and rejection attribution; update page objects and verify accessible controls and rendered values with Playwright.
- [ ] 4.2 Add end-to-end coverage for project allocation, role-specific invoice visibility, routed approval/denial, high-value second approval, Admin rejection, and rejection attribution; run `dotnet test backend/InvoiceApp.sln` and `npx playwright test`.
