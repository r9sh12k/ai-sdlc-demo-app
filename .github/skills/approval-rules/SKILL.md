---
name: approval-rules
description: Approval role limits, outcomes, and required denial tests.
---

# Approval rules

Follow [AGENTS.md](../../../AGENTS.md); do not duplicate its guidance.

| Role | Amount band | Outcome |
|---|---:|---|
| Manager | ≤ 50,000.00 | May approve as the sole approver |
| Manager | > 50,000.00 through 500,000.00 | Deny |
| Manager | > 500,000.00 | May be first approver only |
| Director | ≤ 500,000.00 | May approve as the sole approver |
| Director | > 500,000.00 | May be first approver; may be second only if distinct |
| Admin | Any amount or stage | Deny |
| Any role | > 500,000.00, second approval | Require a distinct Director; prior Manager or Director is eligible |

Boundaries to test: 50,000.00, 50,000.01, 500,000.00, and 500,000.01.
Compare usernames after trimming and ignoring case; never count one user twice.

| Case | Expected outcome |
|---|---|
| First approver `demo.director`, second `" Demo.Director "` | Deny as the same user |
| Manager attempts the second approval above 500,000.00 | Deny |
| Each denial, including boundary and role denials | Assert every invoice field equals its pre-attempt value and `UpdateCount` is 0 |

Test valid distinct Manager→Director and Director→Director approvals above 500,000.00.
All denial tests must assert both the actionable denial and the unchanged-invoice invariant.
