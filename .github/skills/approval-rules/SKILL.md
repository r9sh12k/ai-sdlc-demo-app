---
name: approval-rules
description: Approval role limits, outcomes, and required denial tests.
---

# Approval rules

Follow [AGENTS.md](../../../AGENTS.md); do not duplicate its guidance.

Approval is routed by project organization, not by role-only amount limits:

| Amount | Approval route |
|---|---|
| ≤ 50,000.00 | Project DM |
| > 50,000.00 through 500,000.00 | Business Unit head |
| > 500,000.00 | Project DM, then a distinct Business Unit head |
| Missing or duplicate candidate | Move upward through DM, Business Unit head, and Admin; Admin may be the distinct second approver |

The requester and anyone at or below their routing rank (DM < Business Unit head < Admin) must be skipped. Only the current routed person may approve; Admin does not bypass the route. The current routed approver may reject, and Admin may reject any pending invoice, with a non-empty reason. A self-approval check remains in the Domain service as a safety net.

Compare usernames after trimming and ignoring case; never count one user twice. For every denial, assert its actionable reason, every invoice field unchanged, and `UpdateCount` 0. No validation failure may change state or call `Update`.

Boundaries to test: 50,000.00, 50,000.01, 500,000.00, and 500,000.01. Include requester/lower-rank escalation, missing DMs, distinct high-value approvers, Admin routing/fallback, Admin rejection, non-routed approval/rejection, and empty rejection reasons.

The demo hierarchy is BU-DIGITAL (head `demo.director`) > ACC-ACME > PRJ-PORTAL (DM `demo.manager`) and PRJ-MOBILE (DM `demo.manager2`); BU-ENTERPRISE (head `demo.director2`) > ACC-GLOBEX > PRJ-ERP (no DM). `demo.requester` is allocated to PORTAL and MOBILE, and `demo.requester2` to ERP. Display emails as `<username>@example.com`.
