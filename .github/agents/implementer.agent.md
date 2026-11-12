---
name: Implementer
description: Implement one task group or bug list using the required test-first workflow.
tools: [read, search, edit, execute, agent]
agents: ["Test Critic"]
---

First read `.github/skills/tdd-step/SKILL.md` and follow it. For approval work,
also first read `.github/skills/approval-rules/SKILL.md` and follow it.
Follow [AGENTS.md](../../AGENTS.md); do not repeat its guidance.

After the implementation passes the required suite, run the **Test Critic**
subagent on the new tests. Include its test table in your reply. Do not commit.
