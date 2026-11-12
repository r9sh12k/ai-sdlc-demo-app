# Invoice Approval Demo App

A small invoice approval app used for live AI-in-SDLC demos. It is deliberately simple: a .NET 8 API, a plain web UI, and an in-memory store, so it runs with one command and no database.

## Run it

```
dotnet run --project backend/src/InvoiceApp.Api
```

Open http://localhost:5000 and sign in:

| User | Password | Role |
| --- | --- | --- |
| `demo.manager` | `ChangeMe123!` | Manager |
| `demo.manager2` | `ChangeMe123!` | Manager |
| `demo.director` | `ChangeMe123!` | Director |
| `demo.director2` | `ChangeMe123!` | Director |
| `demo.admin` | `ChangeMe123!` | Admin |
| `demo.requester` | `ChangeMe123!` | Requester |
| `demo.requester2` | `ChangeMe123!` | Requester |

These are demo accounts for a local, in-memory app. They are not real credentials.
Each demo identity is displayed with an email address of `<username>@example.com`.

## Organization and routing

The demo organization is:

- BU-DIGITAL (head `demo.director`) > ACC-ACME > PRJ-PORTAL (DM `demo.manager`) and PRJ-MOBILE (DM `demo.manager2`)
- BU-ENTERPRISE (head `demo.director2`) > ACC-GLOBEX > PRJ-ERP (no DM)
- `demo.requester` is allocated to PRJ-PORTAL and PRJ-MOBILE; `demo.requester2` is allocated to PRJ-ERP.

## What it does

1. Create a draft for a project you are allocated to.
2. Submit it for approval. Only the requester who created a draft can submit it.
3. The routed approver approves or rejects it with a reason. Above $500,000, two distinct approvals are required.

Invoices move through Draft, Pending Approval, then Approved or Rejected.

Amounts up to and including $50,000 route to the project DM; amounts above $50,000 through $500,000 route to the Business Unit head. Above $500,000, the DM approves first and a distinct Business Unit head approves second. Missing approvers move upward, with Admin as the final fallback. A requester or a person ranked below them is skipped. Admin may approve only when routed to them, and may reject any pending invoice with a reason. Approval and rejection require confirmation; an empty rejection reason displays an error dialog.

The UI shows Submit, Approve and Reject only when the server says the signed-in person may take that action. Approvals are grouped into Pending and Decided. Usernames are matched at sign-in without regard to letter case or surrounding spaces.

Invoice lists are scoped: Requesters see their own invoices, DMs see invoices for their projects, Business Unit heads see invoices in their Business Unit, and Admins see all invoices. Approval lists show pending invoices in the caller's scope and decisions they have made.

## Tests

```
dotnet test backend/InvoiceApp.sln
```

## Project layout

```
backend/
  src/InvoiceApp.Domain   business rules
  src/InvoiceApp.Api      controllers, sign-in, web UI (wwwroot)
  tests/InvoiceApp.Tests  xUnit tests
docs/use-cases            short feature briefs
AGENTS.md                 conventions for AI coding agents
```

## Use cases

- [UC-1 Bulk invoice approval](docs/use-cases/UC-1-bulk-approval.md)
- [UC-2 Partial approval](docs/use-cases/UC-2-partial-approval.md)
