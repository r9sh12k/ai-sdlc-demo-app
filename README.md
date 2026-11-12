# Invoice Approval Demo App

A small invoice approval workflow designed for live AI-in-SDLC demos and engineering walkthroughs. The sample is intentionally lightweight: a .NET 8 web API, a plain browser UI, and an in-memory data store so it can run locally without a database.

The repository also shows a complete AI-assisted delivery loop. Specs are written first with OpenSpec. Custom agents and skills implement each change test-first and review it. Hooks log every AI interaction, and mutation testing checks that the tests really guard the rules.

## Repository links

- GitHub repository: https://github.com/r9sh12k/ai-sdlc-demo-app
- Issues: https://github.com/r9sh12k/ai-sdlc-demo-app/issues
- Pull requests: https://github.com/r9sh12k/ai-sdlc-demo-app/pulls
- Use-case docs: [docs/use-cases](docs/use-cases)
- OpenSpec files: [openspec](openspec)
- AI agent guidance: [AGENTS.md](AGENTS.md)

## Contents

- [Why this repo exists](#why-this-repo-exists)
- [Prerequisites](#prerequisites)
- [Quick start](#quick-start)
- [Demo accounts](#demo-accounts)
- [Business flows](#business-flows)
- [Architecture](#architecture)
- [API](#api)
- [Testing](#testing)
- [AI-assisted SDLC setup](#ai-assisted-sdlc-setup)
- [Project layout](#project-layout)
- [Notes](#notes)

## Why this repo exists

This project demonstrates a realistic approval flow with business rules, guardrails, and UI behavior in a compact codebase. It is useful for:

- AI-assisted SDLC and review demos
- Domain-driven approval logic discussions
- API + UI workflow walkthroughs
- Requirements and test-driven development exercises

## Prerequisites

| Tool | Needed for | Install |
| --- | --- | --- |
| .NET 8 SDK | Build, run and unit tests | https://dotnet.microsoft.com/download |
| Node.js 18+ | Playwright tests, hooks, OpenSpec CLI | https://nodejs.org |
| Playwright Chromium | End-to-end tests | `cd e2e && npm install && npx playwright install chromium` |
| OpenSpec CLI | Spec-driven changes | `npm install -g @fission-ai/openspec` |
| Stryker.NET 4.13.0 | Mutation testing (optional) | `dotnet tool install -g dotnet-stryker --version 4.13.0` |

## Quick start

```bash
dotnet run --project backend/src/InvoiceApp.Api
```

Then open http://localhost:5000 in a browser and sign in with one of the demo accounts below. The sign-in page lists the demo usernames; choosing one fills in the username field.

## Demo accounts

These are local demo identities for the in-memory app and are not real credentials.

| User | Password | Role |
| --- | --- | --- |
| `demo.manager` | `ChangeMe123!` | Manager |
| `demo.manager2` | `ChangeMe123!` | Manager |
| `demo.director` | `ChangeMe123!` | Director |
| `demo.director2` | `ChangeMe123!` | Director |
| `demo.admin` | `ChangeMe123!` | Admin |
| `demo.requester` | `ChangeMe123!` | Requester |
| `demo.requester2` | `ChangeMe123!` | Requester |

Each demo identity is displayed with an email address of `<username>@example.com`. Usernames are matched at sign-in ignoring letter case and surrounding spaces; passwords must match exactly.

## Business flows

### Organization

The demo organization is:

- BU-DIGITAL (head `demo.director`) > ACC-ACME > PRJ-PORTAL (DM `demo.manager`) and PRJ-MOBILE (DM `demo.manager2`)
- BU-ENTERPRISE (head `demo.director2`) > ACC-GLOBEX > PRJ-ERP (no DM)
- `demo.requester` is allocated to PRJ-PORTAL and PRJ-MOBILE; `demo.requester2` is allocated to PRJ-ERP.
- A project's Delivery Manager (DM) counts as allocated to that project.

### Invoice lifecycle

```text
Draft ──submit──▶ Pending Approval ──approve (final step)──▶ Approved
                        │
                        ├──approve (first of two steps)──▶ stays Pending, waiting on second approver
                        │
                        └──reject (reason required)──────▶ Rejected
```

1. **Create** a draft for a project you are allocated to. Customer name, a positive amount and a project are required.
2. **Submit** the draft for approval. Only the requester who created the draft can submit it. On submit, the routed approvers are worked out and saved on the invoice.
3. **Decide.** The current routed approver approves or rejects. Both actions ask for confirmation. Rejecting requires a non-empty reason; an empty reason opens an error dialog and nothing is sent.

### Approval routing

| Amount | Route |
| --- | --- |
| Up to and including $50,000.00 | Project DM |
| Above $50,000.00 through $500,000.00 | Business Unit head |
| Above $500,000.00 | Project DM, then a **distinct** Business Unit head |

- **Missing approvers move upward:** DM → Business Unit head → Admin.
- **Skipping:** the requester, and anyone at or below the requester's rank (DM < Business Unit head < Admin), is skipped.
- **Two-step fallback:** above $500,000.00, if both steps would go to the same person, Admin becomes the second approver. If no distinct second approver exists, submit fails with `No second approver available`.
- **Who may decide:** only the current routed approver may approve. Admin may approve only when routed to them, and may reject any pending invoice. Nobody can approve an invoice they submitted.
- **Denied actions** return `403 { "error": "<reason the user can act on>" }` and change nothing.

### Visibility

| Role | Invoice list | Approval list |
| --- | --- | --- |
| Requester | Their own invoices | Not shown |
| DM | Invoices for their projects | Pending invoices for their projects, plus invoices they decided |
| Business Unit head | Invoices in their Business Unit | Pending invoices in their Business Unit, plus invoices they decided |
| Admin | All invoices | All pending invoices, plus invoices they decided |

### Web UI

- **Invoices tab:** a create form with an allocated-project selector, and a table of invoices in your scope. Columns: ID, Customer, Amount, Project, Requested By, Approval 1, Approval 2, Waiting on, Status, Rejection reason, Actions.
- **Approvals tab:** cards grouped under **Pending** and **Decided**. Invoices waiting on you are marked "Needs your decision".
- **Actions shown come from the server:** Submit, Approve and Reject appear only when the API reports `canSubmit`, `canApprove` or `canReject` for the signed-in person. The UI holds no approval rules of its own.
- **Feedback:** a status message after every create, submit or decision; errors in visible alerts; a "Permission denied" dialog for role failures.
- **Double submits:** each button is disabled while its request runs. Every sign-in starts on the Invoices tab.

## Architecture

```text
Browser (wwwroot: index.html, app.js, styles.css)
   │  Bearer token
   ▼
SessionAuthMiddleware ──▶ Controllers (ApiControllerBase.Handle maps errors to 400/403/404/409)
                               │  map session → Domain types (SessionRoles, InvoiceViewer)
                               ▼
                         InvoiceApp.Domain
                           ├─ ApprovalService       submit / approve / reject + CanSubmit/CanApprove/CanReject
                           ├─ ApprovalRouting       amount- and org-based approver routing
                           ├─ InvoiceAccessService  draft creation and scoped lists
                           └─ IInvoiceRepository, IOrgDirectory (interfaces)
                               ▼
                         In-memory implementations (InMemoryInvoiceRepository, InMemoryOrgDirectory)
```

Business rules live only in `InvoiceApp.Domain`. Controllers map the session to Domain types and call `Handle(...)`. Services are registered in `Program.cs`, and the demo users come from `appsettings.json`.

## API

Every endpoint except sign-in requires `Authorization: Bearer <token>`.

| Method | Route | Who | Purpose |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | Anyone | Sign in; returns `{ token, username, role }` |
| POST | `/api/auth/logout` | Signed in | End the session |
| GET | `/api/invoices?status=` | Signed in | Invoices in the caller's scope, optionally filtered by status |
| GET | `/api/invoices/{id}` | Signed in | One invoice, if it is in scope |
| GET | `/api/invoices/projects` | Signed in | Projects the caller may create invoices for |
| POST | `/api/invoices` | Allocated users | Create a draft `{ customerName, amount, projectId }` |
| POST | `/api/invoices/{id}/submit` | Draft's requester | Submit for approval |
| DELETE | `/api/invoices/{id}` | Admin | Delete an invoice |
| GET | `/api/approvals` | Signed in | Scoped pending invoices plus the caller's decisions |
| POST | `/api/approvals/{id}/approve` | Manager, Director, Admin | Approve (must be the routed approver) |
| POST | `/api/approvals/{id}/reject` | Manager, Director, Admin | Reject `{ reason }` (routed approver or Admin) |

List items include display fields (`requestedBy`, `approval1`, `approval2`, `waitingOn`, `rejectionReason`, `firstApprovedBy`, each formatted as `username (email)`) and the caller's action flags (`canSubmit`, `canApprove`, `canReject`).

## Testing

| Suite | Command | Notes |
| --- | --- | --- |
| Unit (xUnit) | `dotnet test backend/InvoiceApp.sln` | One `<ClassUnderTest>Tests` class per class; uses `FakeInvoiceRepository` and `FakeOrgDirectory` |
| End-to-end (Playwright) | `cd e2e && npx playwright test` | Starts the app on port 5000, or reuses one that is already running; page objects in `e2e/pages`, role-based locators only |
| Mutation (Stryker.NET) | `cd backend/tests/InvoiceApp.Tests && dotnet stryker --project InvoiceApp.Domain.csproj` | Mutates the Domain project; reports go to `StrykerOutput/` |

Useful extras: `npm run test:headed` and `npm run report` from `e2e/`.

**Mutation testing workflow:**
1. Stop the running app first; it locks `InvoiceApp.Api.dll`.
2. Run Stryker and save the output as [docs/mutation/before.txt](docs/mutation).
3. Add tests that kill the surviving mutants. Mark only truly equivalent mutants with `// Stryker disable once <Mutator> : <reason>`.
4. Run Stryker again and save the output as `docs/mutation/after.txt`.

Rule-testing expectations (from [AGENTS.md](AGENTS.md)):
- Test the boundaries 50,000.00, 50,000.01, 500,000.00 and 500,000.01.
- Every denial asserts its reason, an unchanged invoice and `UpdateCount` 0.
- A rule is done only when a test fails if the rule is removed.

## AI-assisted SDLC setup

All agent configuration lives in [AGENTS.md](AGENTS.md) and [.github/](.github). `CLAUDE.md` only imports `AGENTS.md`. Do not add tool-specific agent folders (such as `.claude/`) to this repo.

### End-to-end flow

```text
1. Spec        OpenSpec agent / opsx-propose → proposal, delta specs, design, tasks
2. Build       Implementer agent (tdd-step + approval-rules skills), one task group per prompt
3. Test check  Test Critic agent rates every new test: Keep / Fix / Delete
4. Review      PR Reviewer agent (pr-review skill); a hook enforces the findings-table format
5. Harden      Stryker before/after runs; kill surviving mutants
6. Close       opsx-sync / opsx-archive; commit as `<type>(<scope>): <what>` with a `Why:` line
               Hooks log every step to docs/ai-log throughout
```

### OpenSpec

- Config: [openspec/config.yaml](openspec/config.yaml) (schema `spec-driven`)
- Changes: [openspec/changes/](openspec/changes)
- Main specs, once synced: [openspec/specs/](openspec/specs)
- Run `openspec list` for change status, and `openspec validate <change> --strict` before applying.

| Change | Purpose |
| --- | --- |
| `add-approval-limits` | Role-based approval limits and two-step approval above $500,000 |
| `allow-admin-approval-and-permission-modal` | Admin approval authority and the permission-denied dialog |
| `admin-rejection-and-decision-dialogs` | Admin rejection, confirmation dialogs and the missing-reason dialog |
| `add-org-routing` | Business Unit > Account > Project routing, allocation and scoped lists |
| `fix-workflow-bugs-and-refresh-ui` | Submit ownership, server-driven action flags, trimming, UI fixes and refresh |

### Skills ([.github/skills](.github/skills))

| Skill | Use |
| --- | --- |
| `tdd-step` | Do one task group or bug list test-first: failing test, smallest fix, one suite rerun, step report |
| `approval-rules` | Routing table, rank and skip rules, demo hierarchy, required denial and boundary tests |
| `pr-review` | Evidence-only defect review in the required `## Review findings` table plus `## Summary` |
| `openspec-propose` | Create a change with proposal, specs, design and tasks (planning only) |
| `openspec-explore` | Think through ideas or requirements before or during a change |
| `openspec-update-change` | Revise a change's planning artifacts and keep them consistent |
| `openspec-apply-change` | Implement a change's tasks and tick them off |
| `openspec-sync-specs` | Merge delta specs into the main specs |
| `openspec-archive-change` | Archive a completed change |

### Custom agents ([.github/agents](.github/agents))

| Agent | Role | Can edit? |
| --- | --- | --- |
| Implementer | Reads `tdd-step` (and `approval-rules` for approval work), implements test-first, then runs Test Critic | Yes |
| Test Critic | Rates each test: does it fail if the rule is removed or inverted? | No |
| PR Reviewer | Reviews a diff and returns only the findings table and summary | No |
| OpenSpec | Drives the OpenSpec CLI: propose, validate, status, archive | Yes |

### Prompts ([.github/prompts](.github/prompts))

`opsx-propose`, `opsx-explore`, `opsx-update`, `opsx-apply`, `opsx-sync`, `opsx-archive`: slash-command entry points to the matching OpenSpec skills.

### Hooks ([.github/hooks](.github/hooks))

[`assignment.json`](.github/hooks/assignment.json) wires these events:

| Event | Command | Effect |
| --- | --- | --- |
| SessionStart, UserPromptSubmit, PostToolUse, SubagentStart, SubagentStop, Stop | `node .github/hooks/ai-log.cjs <Event>` | Appends one entry per event to `docs/ai-log/ai-interactions.jsonl` (full detail) and `ai-interactions.md` (summary table) |
| SubagentStop | `node .github/hooks/check-review-format.cjs` | For the PR Reviewer, blocks a reply missing `## Review findings`, the findings-table header or `## Summary`, or with a malformed `F-NN` row. It retries at most twice |

The hooks write `docs/ai-log`; do not edit it by hand. [docs/context-test.md](docs/context-test.md) compares an agent run before and after `AGENTS.md` was extended.

### MCP

[.mcp.json](.mcp.json) registers the Playwright MCP server (`npx @playwright/mcp@latest`), so agents can drive and inspect the running UI.

### CI ([.github/workflows](.github/workflows))

| Workflow | Trigger | What it does |
| --- | --- | --- |
| `ci.yml` | Push or PR to `main` | Release build and `dotnet test` |
| `copilot-setup-steps.yml` | Manual, or a change to the workflow | Installs and verifies the OpenSpec CLI for the Copilot coding agent |

### Conventions in brief

Full list in [AGENTS.md](AGENTS.md).

- **Controllers:** one per resource under `Controllers/`, route `api/[controller]`, inherit `ApiControllerBase`, and call `Handle(...)`.
- **Rules and roles:** no business rules in controllers, `Program.cs` or `app.js`. Domain code uses no role strings or HTTP types.
- **Access:** every endpoint is behind sign-in, and destructive actions are Admin-only.
- **Validation:** validate first and change state last. Error text must be fit for a user.
- **Reviews:** findings go in a table `| ID | Severity | File:line | Finding | Suggested fix |`, numbered F-01 upward with High, Medium or Low severity.
- **Secrets:** never commit real credentials, tokens or customer data.

## Project layout

```text
backend/
  InvoiceApp.sln
  src/InvoiceApp.Domain      business rules: ApprovalService, ApprovalRouting, InvoiceAccessService, org model
  src/InvoiceApp.Api         controllers, Auth/, in-memory store and org directory, web UI (wwwroot)
  tests/InvoiceApp.Tests     xUnit tests and fakes

e2e/
  tests/                     Playwright specs
  pages/                     page objects (sign-in, invoices, approvals)
  playwright.config.ts

docs/
  use-cases/                 short feature briefs
  mutation/                  Stryker before/after output
  ai-log/                    hook-written AI interaction log (do not edit)
  context-test.md            agent context experiment

openspec/                    config, changes and specs
.github/                     agents, skills, prompts, hooks, workflows
AGENTS.md                    AI coding conventions (CLAUDE.md imports it)
.mcp.json                    Playwright MCP server
```

## Relevant use cases

- [UC-1 Bulk invoice approval](docs/use-cases/UC-1-bulk-approval.md)
- [UC-2 Partial approval](docs/use-cases/UC-2-partial-approval.md)

## Notes

- This repository is intentionally simple and does not use a database or external services; all data resets when the app restarts.
- The app is intended for demos, rapid iteration, and AI-assisted SDLC workflows.
- All approval rules are enforced in the domain layer, not in controllers or the UI.
