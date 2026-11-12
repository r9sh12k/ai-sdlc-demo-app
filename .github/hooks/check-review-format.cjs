const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const crypto = require('node:crypto');

async function main() {
  let raw = '';
  for await (const chunk of process.stdin) raw += chunk;
  let data;
  try { data = JSON.parse(raw || '{}'); } catch { return; }
  {
    const agent = data.agent_name || data.agentName || data.agent_type || data.agentType || '';
    if (!/pr[- ]?reviewer/i.test(agent)) return;
    const reply = data.last_assistant_message || data.lastAssistantMessage || data.response || '';
    const missing = ['## Review findings', '| ID | Severity | File:line | Finding | Suggested fix |', '## Summary'].filter(marker => !reply.includes(marker));
    const invalidRow = reply.split(/\r?\n/).some(line => /^\|\s*F-/.test(line) && !/^\|\s*F-\d{2}\s*\|\s*(High|Medium|Low)\s*\|/.test(line));
    if (!missing.length && !invalidRow) return;
    const agentId = String(data.agent_id || data.agentId || agent);
    const counter = path.join(os.tmpdir(), `copilot-review-format-${crypto.createHash('sha256').update(agentId).digest('hex')}.txt`);
    const count = Number(fs.existsSync(counter) ? fs.readFileSync(counter, 'utf8') : 0);
    if (count >= 2) return;
    fs.writeFileSync(counter, String(count + 1));
    const issue = missing.length ? `Missing required section/header: ${missing.join(', ')}` : 'A finding row does not match the required ID and severity format.';
    process.stdout.write(JSON.stringify({ decision: 'block', reason: `${issue} Rewrite the whole report in the pr-review skill format.` }));
  }
}

main();
