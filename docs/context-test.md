# Context Test

Source: `docs/ai-log/ai-interactions.md`

Prompt used (Prompt 2, sent word for word in both runs):
"Do not edit files. Using only this repository, answer in under 120 words: a Manager approves a 75,000 invoice. What should happen, and what does the user see? Which files would you change? Which command verifies it? How will the change be reviewed? No new rows were added"

## Before (session dcce4852, 2026-11-12 11:55 UTC)

AGENTS.md had not been extended yet. The agent did not read it.

- Tool calls: 14 (Grep 3, Glob 2, Bash 1, Read 8)
- Time: about 44s (11:55:31 to 11:56:15)
- Started with broad searches (`75000`, `Manager`, `PendingApproval`, `approval`, `threshold`) and `git status`
- Read: `UC-1-bulk-approval.md`, `ApprovalService.cs`, `ApprovalsController.cs`, `app.js`, `Invoice.cs`, `InvoiceStatus.cs`, `ApprovalServiceTests.cs`, `InvoiceApp.sln`
- Globbed the `backend/tests` and `e2e` folders; searched for roles (`RequireRole`, `Manager`, `Approver`, `Admin`)
- Did not open: `AGENTS.md`, `appsettings.json`, `ApiControllerBase.cs`, `check-review-format.cjs`

### Answer

Search for `75000|75,000|Manager|manager|PendingApproval|Approved|approval|threshold`

Find files matching `**/*`

Search for `Manager|75,000|approval|approve`

Ran terminal command: git status --short; Get-ChildItem -Name

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\docs\\use-cases\\UC-1-bulk-approval.md"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\src\\InvoiceApp.Domain\\ApprovalService.cs"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\src\\InvoiceApp.Api\\Controllers\\ApprovalsController.cs"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\src\\InvoiceApp.Api\\wwwroot\\app.js"
}

Find files matching `**/*`

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\src\\InvoiceApp.Domain\\Invoice.cs"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\src\\InvoiceApp.Domain\\InvoiceStatus.cs"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\tests\\InvoiceApp.Tests\\ApprovalServiceTests.cs"
}

Search for `RequireRole|Manager|Approver|Admin|role`

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\InvoiceApp.sln"
}

The repo’s `UC-1 brief` says invoices over $1,000 need a second approval before becoming final. So the Manager’s approval should leave the $75,000 invoice awaiting that approval; the UI should show it as awaiting a second approver, then show final approval and both approvers’ audit details. Whether the same person can approve twice is an open question. Today, `ApprovalService` approves immediately.

I’d change `InvoiceStatus.cs`, `Invoice.cs`, the service and its tests, `ApprovalsController.cs`, and the UI in `wwwroot`. Verify with `dotnet test backend/InvoiceApp.sln`. Review via a pull request, checking the threshold, second-approval/audit behavior, UI, and tests.

## Change between runs (session 3cd1b0f2, 11:57 UTC)

`AGENTS.md` was extended so an agent arriving cold can build and review the approval-limits feature. The agent read it, edited it, then ran `git diff --check` and a line count.

## After (session 884ffd6b, 2026-11-12 12:11 UTC, new chat)

- Tool calls: 11 (Read 9, Glob 1, Grep 1)
- Time: about 31s (12:11:53 to 12:12:24)
- Read `AGENTS.md` first, then one broad Glob and one Grep
- Then read: `ApprovalService.cs`, `Invoice.cs`, `ApprovalsController.cs`, `ApprovalServiceTests.cs`, `app.js`, `appsettings.json`, `check-review-format.cjs`, `ApiControllerBase.cs`
- Did not run `git status`; did not open the use-case doc, e2e folder, `.sln` or `InvoiceStatus.cs`

### Answer

I’ll trace the approval rules, UI behavior, tests, and repository review conventions, without changing files.

Search for `75[,\s]?000|threshold|Manager|manager|approve|Approved|PendingApproval`

Find files matching `**/*`

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\AGENTS.md"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\src\\InvoiceApp.Domain\\ApprovalService.cs"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\src\\InvoiceApp.Domain\\Invoice.cs"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\src\\InvoiceApp.Api\\Controllers\\ApprovalsController.cs"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\src\\InvoiceApp.Api\\Controllers\\ApiControllerBase.cs"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\src\\InvoiceApp.Api\\wwwroot\\app.js"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\tests\\InvoiceApp.Tests\\ApprovalServiceTests.cs"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\backend\\src\\InvoiceApp.Api\\appsettings.json"
}

Read 
Completed with input: {
  "path": "C:\\Users\\Rishab\\Desktop\\final\\ai-sdlc-demo-app\\.github\\hooks\\check-review-format.cjs"
}

Per the `approval-limit rules`, a Manager cannot approve $75,000—their limit is $50,000. The invoice stays pending; a Director (limit $500,000) can approve it. The API should return 403 with an actionable reason, shown to the user as an inline error.

I’d change `ApprovalService.cs`, `Invoice.cs`, `ApprovalsController.cs`, `ApiControllerBase.cs`, `appsettings.json`, and `ApprovalServiceTests.cs`.

Verify with `dotnet test backend/InvoiceApp.sln`. Review as a PR using the required findings table; check limits, boundary cases, unchanged state on denial, and Domain-layer rule placement.

## What changed
- The answer changed. Before: second approval above $1,000, from the UC-1 brief. After: Manager limit of $50,000 versus Director limit of $500,000, with a 403 on denial, from the approval-limit rules.
- The agent read AGENTS.md first and went straight to the approval, test, config and review-format files. Tool calls dropped from 14 to 11 and time from about 44s to about 31s.

