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

## Approval limits (`feature/approval-limits`)

- Let a Requester create, submit, and view invoices; deny Requester approve and reject actions.
- On submit, record the signed-in username as `SubmittedBy`.
- Let a Manager approve amounts up to and including 50,000.00.
- Let a Director approve amounts up to and including 500,000.00.
- Require two distinct approvers above 500,000.00: first a Manager or Director, then a Director.
- Compare usernames after trimming and ignoring case; never count one user twice.
- Deny approval by the submitter at either approval stage, for any amount and role, before applying amount limits. Invoices without `SubmittedBy` are unaffected.
- Use the exact self-approval denial reason: `You submitted this invoice, so someone else must approve it.`
- Deny Admin approval; Manager and Director replace Approver.
- For a denied approval attempt, throw `ApprovalDeniedException` with an actionable reason; change no fields and do not call `Update`.
- Test boundaries 50,000.00, 50,000.01, 500,000.00 and 500,000.01.
- Keep approval rules in `InvoiceApp.Domain`; use no role strings or HTTP types there.
- Map the session to Domain types in controllers, then call `Handle(...)`.
- Map `ApprovalDeniedException` in `ApiControllerBase` to `403 { "error": reason }`.
- Validate first and change state last.
- Retain `FirstApprovedBy` after a second approval. Show `Submitted by <name>` and `Approved by <name>` for one approver or `Approved by <first> and <second>` for two in the invoice list.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- From `backend/tests/InvoiceApp.Tests`, run mutation tests: `dotnet stryker --project InvoiceApp.Domain.csproj` (Stryker 4.13.0).
- From `e2e`, run end-to-end tests: `npx playwright test`.

## Demo accounts (not real credentials)

- `demo.manager` / `ChangeMe123!` (role Manager)
- `demo.director` / `ChangeMe123!` (role Director)
- `demo.director2` / `ChangeMe123!` (role Director)
- `demo.admin` / `ChangeMe123!` (role Admin)
- `demo.requester` / `ChangeMe123!` (role Requester)

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
