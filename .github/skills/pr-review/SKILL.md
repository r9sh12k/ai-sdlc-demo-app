---
name: pr-review
description: Review the supplied diff for evidence-backed defects without changing files.
---

Read [AGENTS.md](../../../AGENTS.md) and the supplied diff. Change nothing.
Report only real defects supported by evidence in the diff or relevant context.
Do not report style, speculation, or recommendations without a defect.

Output exactly:
1. `## Review findings`
2. The table `| ID | Severity | File:line | Finding | Suggested fix |`, with IDs
   starting at F-01 and increasing; severity must be High, Medium, or Low.
   If there are no findings, include the header and separator only.
3. `## Summary` followed by one paragraph.

Do not add other headings, preamble, or sections.
