const fs = require('node:fs');
const path = require('node:path');

const field = (value, snake, camel) => value?.[snake] ?? value?.[camel];
const text = value => typeof value === 'string' ? value : value == null ? '' : JSON.stringify(value);

async function main() {
  try {
    let raw = '';
    for await (const chunk of process.stdin) raw += chunk;
    const data = JSON.parse(raw || '{}');
    const event = process.argv[2] || '';
    const sessionId = field(data, 'session_id', 'sessionId') || '';
    const agent = field(data, 'agent_name', 'agentName') || field(data, 'agent_type', 'agentType') || '';
    const tool = field(data, 'tool_name', 'toolName') || '';
    const name = tool || agent;
    let reply = field(data, 'last_assistant_message', 'lastAssistantMessage') || data.response;
    if (event === 'Stop' && !reply) {
      const transcript = field(data, 'transcript_path', 'transcriptPath');
      if (transcript) {
        const content = fs.readFileSync(transcript, 'utf8');
        let parsed;
        try { parsed = JSON.parse(content); }
        catch { parsed = content.split(/\r?\n/).filter(Boolean).map(line => JSON.parse(line)); }
        const entries = Array.isArray(parsed) ? parsed : parsed.messages || parsed.transcript || [parsed];
        const last = entries.filter(entry => /assistant/i.test(entry.role || entry.type || entry.message?.role || '')).at(-1);
        reply = last?.content ?? last?.message?.content ?? last?.message ?? last?.text;
      }
    }
    if (event === 'Stop' && !reply) reply = 'Turn finished';
    const prompt = field(data, 'prompt', 'prompt') || field(data, 'user_message', 'userMessage');
    const target = field(data, 'tool_input', 'toolInput') || field(data, 'tool_target', 'toolTarget');
    const detail = text(reply || target || prompt || data).slice(0, 4000);
    const summary = text(prompt || target || reply || data).replace(/\s+/g, ' ').slice(0, 140);
    const actor = event === 'UserPromptSubmit' ? 'user' : event === 'SubagentStart' || event === 'SubagentStop' ? 'subagent' : event === 'Stop' ? 'assistant' : event === 'PostToolUse' ? 'assistant' : 'system';
    const fields = { ts: new Date().toISOString(), session: String(sessionId).slice(0, 8), event, actor, name: text(name), summary, detail };
    const dir = path.resolve(process.cwd(), 'docs', 'ai-log');
    fs.mkdirSync(dir, { recursive: true });
    fs.appendFileSync(path.join(dir, 'ai-interactions.jsonl'), `${JSON.stringify(fields)}\n`);
    const markdown = path.join(dir, 'ai-interactions.md');
    if (!fs.existsSync(markdown)) fs.writeFileSync(markdown, '| ts | session | event | actor | name | summary |\n| --- | --- | --- | --- | --- | --- |\n');
    const cells = [fields.ts, fields.session, fields.event, fields.actor, fields.name, fields.summary].map(value => value.replace(/\|/g, '/'));
    fs.appendFileSync(markdown, `| ${cells.join(' | ')} |\n`);
  } catch {
    process.exitCode = 0;
  }
}

main();
