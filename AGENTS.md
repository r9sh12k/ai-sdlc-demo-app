# Project conventions for AI agents

Invoice approval demo app: ASP.NET Core 8 Web API with a small static web UI.

## Layout

- `backend/src/InvoiceApp.Domain`: business rules, no web or storage code. `ApprovalService` owns the invoice lifecycle (Draft, PendingApproval, Approved, Rejected).
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), in-memory repository, and the web UI under `wwwroot/`.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`).
- `docs/use-cases`: short briefs for features we may build.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404, 409 and 400.
- Keep every endpoint behind sign-in. Restrict destructive actions to Admin.
- Business rules go in the Domain project, never in controllers.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Every new rule or endpoint needs tests. Use `FakeInvoiceRepository` in tests.
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Organization routing (`add-org-routing`)

- Associate each invoice with a project in Business Unit > Account > Project; require project allocation to create an invoice. A project's DM counts as allocated.
- Route amounts up to and including 50,000.00 to the DM; amounts above 50,000.00 through 500,000.00 to the Business Unit head; amounts above 500,000.00 to the DM then a distinct Business Unit head.
- If an approver is unavailable, move up through DM, Business Unit head, and Admin. If a candidate is the requester or is at/below the requester's rank, move upward. Use Admin as the fallback second approver when both steps resolve to one person; otherwise fail with `No second approver available`.
- Only the current routed approver may approve. Admin may approve only when routed, including a distinct second step; Admin may reject any pending invoice. Reject requires a non-empty reason.
- Compare usernames after trimming and ignoring case; never count one user twice. Keep a self-approval check in `ApprovalService` as a safety net.
- On submit, snapshot routed approvers and record the signed-in username as `SubmittedBy`.
- For a denied attempt, throw `ApprovalDeniedException` with an actionable reason; change no fields and do not call `Update`.
- Test boundaries 50,000.00, 50,000.01, 500,000.00 and 500,000.01; negative tests assert unchanged invoices and `UpdateCount` 0.
- Keep routing and approval rules in `InvoiceApp.Domain`; use no role strings or HTTP types there.
- Map the session to Domain types in controllers, then call `Handle(...)`. Map `ApprovalDeniedException` to `403 { "error": reason }`.
- Validate first and change state last.
- Scope invoice lists to Requester-owned invoices, DM projects, Business Unit head's BU, or all for Admin. Approval lists include scoped pending invoices plus invoices decided by the caller.
- Display person identity as `username (username@example.com)` and rejection attribution as `username (email): reason`.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- From `backend/tests/InvoiceApp.Tests`, run mutation tests: `dotnet stryker --project InvoiceApp.Domain.csproj` (Stryker 4.13.0).
- From `e2e`, run end-to-end tests: `npx playwright test`.

## Demo accounts (not real credentials)

- `demo.manager` / `ChangeMe123!` (role Manager)
- `demo.manager2` / `ChangeMe123!` (role Manager)
- `demo.director` / `ChangeMe123!` (role Director)
- `demo.director2` / `ChangeMe123!` (role Director)
- `demo.admin` / `ChangeMe123!` (role Admin)
- `demo.requester` / `ChangeMe123!` (role Requester)
- `demo.requester2` / `ChangeMe123!` (role Requester)

## Agent workflow

- Read the Implementer, Test Critic and PR Reviewer skills by path in `.github/skills`.
- Write the spec first with OpenSpec; handle one task group per prompt.
- Write tests first; consider a rule done only when a test fails if the rule is removed.
- Format commits as `<type>(<scope>): <what>` and include a `Why:` line.
- Let hooks write `docs/ai-log`; never edit it.

## Review expectations

- Report findings in a table: `| ID | Severity | File:line | Finding | Suggested fix |`.
- Number findings from F-01 upward; use High, Medium or Low severity.
- Check negative rules assert the invoice is unchanged and all four boundaries are tested.
- Check nothing changes state before validation and no rule sits in a controller or `app.js`.
- Check error text is fit for a user.

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs` or controllers.
