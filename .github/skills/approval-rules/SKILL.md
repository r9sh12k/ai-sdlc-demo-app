---
name: approval-rules
description: Approval role limits, outcomes, and required denial tests.
---

# Approval rules

Follow [AGENTS.md](../../../AGENTS.md); do not duplicate its guidance.

| Role | Amount band | Outcome |
|---|---:|---|
| Requester | Any amount or stage | May create, submit, and view invoices; cannot approve or reject |
| Manager | ≤ 50,000.00 | May approve as the sole approver |
| Manager | > 50,000.00 through 500,000.00 | Deny |
| Manager | > 500,000.00 | May be first approver only |
| Director | ≤ 500,000.00 | May approve as the sole approver |
| Director | > 500,000.00 | May be first approver; may be second only if distinct |
| Admin | ≤ 500,000.00 | May approve as the sole approver, using Director limits |
| Admin | > 500,000.00 | May be first approver; may be second only if distinct |
| Any role | > 500,000.00, second approval | Require a distinct Director or Admin; prior Manager, Director, or Admin is eligible |

Admins may approve but may not reject invoices.

On submission, record the signed-in username as `SubmittedBy`. Deny approval by the submitter at either approval stage, for any amount and role. Compare usernames after trimming and ignoring case, and perform this check before amount-limit checks. A denial must use `You submitted this invoice, so someone else must approve it.`, leave every invoice field unchanged, and not call `Update`. If `SubmittedBy` is absent, do not apply the self-approval rule.

Boundaries to test: 50,000.00, 50,000.01, 500,000.00, and 500,000.01.
Compare usernames after trimming and ignoring case; never count one user twice.

| Case | Expected outcome |
|---|---|
| First approver `demo.director`, second `" Demo.Director "` | Deny as the same user |
| Manager attempts the second approval above 500,000.00 | Deny |
| Submitter attempts first approval within their role's limit | Deny with the exact self-approval reason |
| Submitter attempts second approval as a Director after another user approved first | Deny with the exact self-approval reason |
| Approval attempt on an invoice without `SubmittedBy` | Apply existing role and amount rules without self-approval denial |
| Requester attempts to approve or reject | Deny; assert invoice unchanged |
| Each denial, including boundary, role, and self-approval denials | Assert every invoice field equals its pre-attempt value and `UpdateCount` is 0 |

Test valid distinct Manager→Director and Director→Director approvals above 500,000.00.
Test valid distinct Manager→Admin and Admin→Director approvals above 500,000.00.
All denial tests must assert both the actionable denial and the unchanged-invoice invariant.
Retain `FirstApprovedBy` after the second approval. The invoice list must show `Submitted by <name>` and `Approved by <name>` for one approver or `Approved by <first> and <second>` for two.
