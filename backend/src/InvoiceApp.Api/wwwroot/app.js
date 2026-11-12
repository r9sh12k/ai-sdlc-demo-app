(() => {
  const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
  const permissionDeniedMessage = 'Your account does not have permission to perform this action.';
  const $ = (id) => document.getElementById(id);
  let session = readStoredSession();
  let pendingDecision = null;
  // Bumped on every sign-in and sign-out so responses for an earlier session are ignored.
  let sessionGeneration = 0;

  function readStoredSession() {
    try {
      return JSON.parse(sessionStorage.getItem('session') || 'null');
    } catch {
      return null;
    }
  }

  function el(tag, attrs = {}, ...children) {
    const node = document.createElement(tag);
    for (const [key, value] of Object.entries(attrs)) {
      if (key === 'class') node.className = value;
      else node.setAttribute(key, value);
    }
    for (const child of children) node.append(child);
    return node;
  }

  async function api(path, options = {}) {
    const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) };
    if (session) headers.Authorization = `Bearer ${session.token}`;
    const response = await fetch(`/api${path}`, { ...options, headers });
    if (response.status === 401 && session) { signOut(); throw new Error('Your session expired. Sign in again.'); }
    const body = response.status === 204 ? null : await response.json().catch(() => null);
    if (!response.ok) throw new Error((body && body.error) || `Request failed (${response.status}).`);
    return body;
  }

  // Disables the button while the task runs, so a second click cannot send a second request.
  async function busy(button, task) {
    if (button.disabled) return undefined;
    button.disabled = true;
    button.setAttribute('aria-busy', 'true');
    try {
      return await task();
    } finally {
      button.disabled = false;
      button.removeAttribute('aria-busy');
    }
  }

  function show(id, visible) { $(id).hidden = !visible; }
  function showMessage(id, message) { const node = $(id); node.textContent = message || ''; node.hidden = !message; }
  function clearMessages(...ids) { for (const id of ids) showMessage(id, ''); }

  function clearPermissionDialog() {
    const dialog = $('permission-dialog');
    if (dialog.open) dialog.close();
    $('permission-message').textContent = '';
  }
  function showPermissionDialog(message) {
    $('permission-message').textContent = message;
    const dialog = $('permission-dialog');
    if (!dialog.open) dialog.showModal();
  }

  function setActiveTab(tab) {
    for (const other of ['invoices', 'approvals']) {
      $(`tab-${other}`).setAttribute('aria-selected', String(other === tab));
      $(`tab-${other}`).tabIndex = other === tab ? 0 : -1;
      show(`${other}-panel`, other === tab);
    }
  }

  function statusLabel(status) { return status.replace(/([a-z])([A-Z])/g, '$1 $2'); }

  function statusBadge(status) {
    return el('span', { class: `badge badge-${status}` }, statusLabel(status));
  }

  // Splits "username (email)" into two lines visually; the text content stays the same.
  function person(display) {
    if (!display) return el('span', { class: 'muted' }, '—');
    const match = /^(.*?) \((.*)\)$/.exec(display);
    if (!match) return el('span', {}, display);
    const [mailbox, domain] = match[2].split('@');
    const email = domain === undefined
      ? el('span', { class: 'person-email' }, `(${match[2]})`)
      : el('span', { class: 'person-email' }, `(${mailbox}`, el('wbr'), `@${domain})`);
    return el('span', { class: 'person' },
      el('span', { class: 'person-name' }, match[1]), ' ', email);
  }

  $('permission-dialog').addEventListener('close', () => {
    $('permission-message').textContent = '';
  });

  function render() {
    const signedIn = !!session;
    show('login-view', !signedIn);
    show('app-view', signedIn);
    show('session-bar', signedIn);
    if (!signedIn) {
      $('username').focus();
      return;
    }

    $('whoami').textContent = session.username;
    $('role-badge').textContent = session.role;
    $('avatar').textContent = session.username.replace(/^demo\./, '').slice(0, 2).toUpperCase();
    const canViewApprovals = session.role !== 'Requester';
    show('tab-approvals', canViewApprovals);
    setActiveTab('invoices');
    clearMessages('invoice-notice', 'approval-notice', 'decision-error', 'create-error');
    loadProjectOptions();
    refresh();
  }

  async function refresh() {
    const generation = sessionGeneration;
    showMessage('invoice-load-error', '');
    try {
      const invoices = await api('/invoices');
      const approvals = session && session.role !== 'Requester' ? await api('/approvals') : [];
      if (generation !== sessionGeneration) return;
      renderInvoices(invoices);
      renderApprovals(approvals);
    } catch (error) {
      if (generation === sessionGeneration) showMessage('invoice-load-error', error.message);
    }
  }

  async function loadProjectOptions() {
    const generation = sessionGeneration;
    showMessage('project-error', '');
    try {
      const projects = await api('/invoices/projects');
      if (generation !== sessionGeneration) return;
      $('project').replaceChildren(
        el('option', { value: '' }, 'Select a project'),
        ...projects.map((project) => el('option', { value: project.id }, project.name))
      );
      show('new-invoice-form', projects.length > 0);
      show('no-projects', projects.length === 0);
    } catch (error) {
      if (generation !== sessionGeneration) return;
      show('new-invoice-form', false);
      show('no-projects', false);
      showMessage('project-error', error.message);
    }
  }

  function renderInvoices(invoices) {
    $('invoices-count').textContent = String(invoices.length);
    show('invoices-empty', invoices.length === 0);
    show('invoices-table', invoices.length > 0);

    const body = $('invoices-table').querySelector('tbody');
    body.replaceChildren(...invoices.map((invoice) => {
      const actions = el('td', { class: 'actions' });
      if (invoice.canSubmit) {
        const submit = el('button', {
          type: 'button',
          class: 'secondary small',
          'aria-label': `Submit invoice ${invoice.id} for approval`
        }, 'Submit');
        const submitError = el('p', { role: 'alert', class: 'alert', hidden: '' });
        submit.addEventListener('click', () => busy(submit, async () => {
          submitError.hidden = true;
          clearMessages('invoice-notice');
          try {
            await api(`/invoices/${invoice.id}/submit`, { method: 'POST' });
          } catch (error) {
            submitError.textContent = error.message;
            submitError.hidden = false;
            return;
          }
          showMessage('invoice-notice', `Invoice ${invoice.id} submitted for approval.`);
          await refresh();
        }));
        actions.append(submit, submitError);
      }
      return el('tr', {},
        el('td', { class: 'mono' }, invoice.id),
        el('td', { class: 'customer' }, invoice.customerName),
        el('td', { class: 'num' }, money.format(invoice.amount)),
        el('td', { class: 'mono' }, invoice.project),
        el('td', {}, person(invoice.requestedBy)),
        el('td', {}, person(invoice.approval1)),
        el('td', {}, invoice.approval2 === 'Not applicable'
          ? el('span', { class: 'muted' }, invoice.approval2)
          : person(invoice.approval2)),
        el('td', {}, person(invoice.waitingOn)),
        el('td', { class: `status status-${invoice.status}` }, statusBadge(invoice.status)),
        el('td', { class: 'reason' }, invoice.rejectionReason),
        actions);
    }));
  }

  function renderApprovals(invoices) {
    const pending = invoices.filter((invoice) => invoice.status === 'PendingApproval');
    const decided = invoices.filter((invoice) => invoice.status !== 'PendingApproval');
    $('approvals-count').textContent = String(pending.length);

    $('pending-list').replaceChildren(...pending.map(approvalCard));
    $('decided-list').replaceChildren(...decided.map(approvalCard));
    show('pending-list', pending.length > 0);
    show('decided-list', decided.length > 0);
    show('pending-empty', pending.length === 0);
    show('decided-empty', decided.length === 0);
  }

  function approvalCard(invoice) {
    const needsYou = invoice.canApprove;
    const item = el('li', { class: needsYou ? 'card needs-you' : 'card' });

    const head = el('div', { class: 'card-head' },
      el('span', { class: 'mono card-id' }, invoice.id),
      el('span', { class: 'card-customer' }, ` · ${invoice.customerName} · `),
      el('span', { class: 'card-amount' }, money.format(invoice.amount)),
      ' ',
      statusBadge(invoice.status));
    if (needsYou) head.append(' ', el('span', { class: 'badge badge-you' }, 'Needs your decision'));
    item.append(head);

    const meta = el('div', { class: 'card-meta' },
      el('span', {}, 'Project: ', el('strong', {}, invoice.project)),
      el('span', {}, ' Requested by: ', el('strong', {}, invoice.requestedBy)));
    if (invoice.waitingOn) meta.append(el('span', {}, ' Waiting on: ', el('strong', {}, invoice.waitingOn)));
    if (invoice.firstApprovedBy) meta.append(el('span', {}, ` First approved by ${invoice.firstApprovedBy}`));
    if (invoice.rejectionReason) meta.append(el('span', {}, ` Rejection reason: ${invoice.rejectionReason}`));
    item.append(meta);

    if (!invoice.canApprove && !invoice.canReject) return item;

    const actions = el('div', { class: 'decision-actions' });
    if (invoice.canReject) {
      const reason = el('input', {
        id: `reason-${invoice.id}`,
        'aria-label': `Rejection reason for ${invoice.id}`,
        placeholder: 'Reason (required to reject)'
      });
      const reject = el('button', {
        type: 'button',
        class: 'danger',
        'aria-label': `Reject invoice ${invoice.id}`
      }, 'Reject');
      reject.addEventListener('click', () => {
        if (!reason.value.trim()) {
          $('reason-error-dialog').showModal();
          return;
        }
        openDecisionConfirmation('Reject', invoice.id, { reason: reason.value.trim() });
      });
      actions.append(el('div', { class: 'reject-row' }, reason, reject));
    }
    if (invoice.canApprove) {
      const approve = el('button', {
        type: 'button',
        class: 'approve',
        'aria-label': `Approve invoice ${invoice.id}`
      }, 'Approve');
      approve.addEventListener('click', () => openDecisionConfirmation('Approve', invoice.id, {}));
      actions.append(approve);
    }
    item.append(actions);
    return item;
  }

  function openDecisionConfirmation(action, invoiceId, payload) {
    pendingDecision = {
      action,
      invoiceId,
      path: `/approvals/${invoiceId}/${action.toLowerCase()}`,
      payload
    };
    $('decision-confirmation-title').textContent = `Confirm ${action.toLowerCase()}`;
    $('decision-confirmation-message').textContent =
      `Are you sure you want to ${action.toLowerCase()} invoice ${invoiceId}?`;
    $('confirm-decision').textContent = action;
    $('confirm-decision').className = action === 'Reject' ? 'danger' : 'approve';
    $('decision-confirmation').showModal();
  }

  function decisionNotice(decision, invoice) {
    if (decision.action === 'Reject') return `Invoice ${decision.invoiceId} rejected.`;
    return invoice && invoice.status === 'PendingApproval'
      ? `First approval recorded for invoice ${decision.invoiceId}.`
      : `Invoice ${decision.invoiceId} approved.`;
  }

  async function decide(decision) {
    clearMessages('decision-error', 'approval-notice');
    try {
      const invoice = await api(decision.path, { method: 'POST', body: JSON.stringify(decision.payload) });
      showMessage('approval-notice', decisionNotice(decision, invoice));
      await refresh();
    } catch (error) {
      if (error.message === permissionDeniedMessage) {
        showPermissionDialog(error.message);
      } else {
        showMessage('decision-error', error.message);
        $('decision-error').scrollIntoView({ block: 'nearest' });
      }
    }
  }

  function signOut() {
    clearPermissionDialog();
    for (const id of ['decision-confirmation', 'reason-error-dialog']) {
      if ($(id).open) $(id).close();
    }
    pendingDecision = null;
    sessionGeneration += 1;
    if (session) api('/auth/logout', { method: 'POST' }).catch(() => {});
    session = null;
    try { sessionStorage.removeItem('session'); } catch { /* storage unavailable */ }
    render();
  }

  $('login-form').addEventListener('submit', (event) => {
    event.preventDefault();
    busy($('sign-in-button'), async () => {
      showMessage('login-error', '');
      try {
        const result = await api('/auth/login', {
          method: 'POST',
          body: JSON.stringify({ username: $('username').value, password: $('password').value })
        });
        clearPermissionDialog();
        sessionGeneration += 1;
        session = { token: result.token, username: result.username, role: result.role };
        try { sessionStorage.setItem('session', JSON.stringify(session)); } catch { /* storage unavailable */ }
        $('password').value = '';
        render();
      } catch (error) {
        showMessage('login-error', error.message);
      }
    });
  });

  for (const chip of document.querySelectorAll('.demo-accounts [data-username]')) {
    chip.addEventListener('click', () => {
      $('username').value = chip.dataset.username;
      $('password').focus();
    });
  }

  $('new-invoice-form').addEventListener('submit', (event) => {
    event.preventDefault();
    busy($('create-invoice'), async () => {
      clearMessages('create-error', 'invoice-notice');
      try {
        const invoice = await api('/invoices', {
          method: 'POST',
          body: JSON.stringify({
            customerName: $('customer').value,
            amount: Number($('amount').value),
            projectId: $('project').value
          })
        });
        $('new-invoice-form').reset();
        showMessage('invoice-notice', `Invoice ${invoice.id} created as a draft.`);
        await refresh();
      } catch (error) {
        showMessage('create-error', error.message);
      }
    });
  });

  $('confirm-decision').addEventListener('click', () => {
    const decision = pendingDecision;
    pendingDecision = null;
    $('decision-confirmation').close();
    if (decision) busy($('confirm-decision'), () => decide(decision));
  });

  $('cancel-decision').addEventListener('click', () => {
    pendingDecision = null;
    $('decision-confirmation').close();
  });

  $('decision-confirmation').addEventListener('close', () => { pendingDecision = null; });

  $('sign-out').addEventListener('click', signOut);

  const tabs = ['invoices', 'approvals'];
  for (const tab of tabs) {
    $(`tab-${tab}`).addEventListener('click', () => setActiveTab(tab));
    $(`tab-${tab}`).addEventListener('keydown', (event) => {
      if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
      const visible = tabs.filter((name) => !$(`tab-${name}`).hidden);
      const next = visible[(visible.indexOf(tab) + (event.key === 'ArrowRight' ? 1 : visible.length - 1)) % visible.length];
      setActiveTab(next);
      $(`tab-${next}`).focus();
    });
  }

  render();
})();
