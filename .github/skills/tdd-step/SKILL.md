---
name: tdd-step
description: Complete one task group or bug list with a focused test-first change.
---

Follow [AGENTS.md](../../../AGENTS.md); do not duplicate its guidance.

For one task group or bug list:
1. Write focused failing tests for the requested behavior.
2. Run exactly `dotnet test backend/InvoiceApp.sln --nologo -v q`.
3. Show that the tests fail for the expected reason, not a setup or compile error.
4. Make the smallest change that fixes the behavior.
5. Run the same suite once more and report its result.
6. Stop. Do not commit, and do not run the suite again.

If failure is unrelated or not for the expected reason, diagnose it without claiming
the test-first check passed; fix only what is needed to validate this task.
Do not silently skip either suite run or report a passing result without its output.

## Step report

End the response with exactly this heading and these four entries:
- Tests added: ...
- Suite result: ...
- Files changed: ...
- Not done: ...
