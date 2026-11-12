import { expect, test } from '@playwright/test';
import { ApprovalsPage } from '../pages/approvals.page';
import { InvoicesPage } from '../pages/invoices.page';
import { SignInPage } from '../pages/sign-in.page';

test('only the requester can submit a draft and a double click creates one invoice', async ({ page }) => {
  const signInPage = new SignInPage(page);
  const invoicesPage = new InvoicesPage(page);
  const customerName = `Double click ${Date.now()}`;

  await page.goto('/');
  await signInPage.signIn('demo.requester', 'ChangeMe123!');
  await invoicesPage.fillInvoice(customerName, 12_000);
  await invoicesPage.createButton.dblclick();

  await expect(invoicesPage.notice).toHaveText(/^Invoice INV-\d+ created as a draft\.$/);
  await expect(invoicesPage.invoiceRows().filter({ hasText: customerName })).toHaveCount(1);
  const invoiceId = await invoicesPage.invoiceIdFor(customerName);
  await expect(invoicesPage.submitButtonFor(invoiceId)).toBeVisible();

  await signInPage.signOut();
  await signInPage.signIn('demo.manager', 'ChangeMe123!');
  await expect(invoicesPage.invoiceRows().filter({ hasText: customerName })).toHaveCount(1);
  await expect(invoicesPage.submitButtonFor(invoiceId)).toHaveCount(0);
});

test('approvals are grouped into Pending and Decided and each sign-in starts on Invoices', async ({ page }) => {
  const signInPage = new SignInPage(page);
  const invoicesPage = new InvoicesPage(page);
  const approvalsPage = new ApprovalsPage(page);
  const highCustomer = `Grouped high ${Date.now()}`;
  const lowCustomer = `Grouped low ${Date.now()}`;

  await page.goto('/');
  await signInPage.signIn('demo.requester', 'ChangeMe123!');
  await invoicesPage.createInvoice(highCustomer, 600_000);
  const highInvoiceId = await invoicesPage.invoiceIdFor(highCustomer);
  await invoicesPage.submitInvoice(highCustomer);
  await expect(invoicesPage.notice).toHaveText(`Invoice ${highInvoiceId} submitted for approval.`);
  await invoicesPage.createInvoice(lowCustomer, 10_000);
  const lowInvoiceId = await invoicesPage.invoiceIdFor(lowCustomer);
  await invoicesPage.submitInvoice(lowCustomer);

  await signInPage.signOut();
  await signInPage.signIn('demo.manager', 'ChangeMe123!');
  await approvalsPage.open();
  await approvalsPage.requestApproval(lowInvoiceId);
  await approvalsPage.confirmDecision('Approve');
  await expect(approvalsPage.notice).toHaveText(`Invoice ${lowInvoiceId} approved.`);
  await approvalsPage.requestApproval(highInvoiceId);
  await approvalsPage.confirmDecision('Approve');
  await expect(approvalsPage.notice).toHaveText(
    `First approval recorded for invoice ${highInvoiceId}.`
  );

  await expect(approvalsPage.decidedItem(lowInvoiceId)).toBeVisible();
  await expect(approvalsPage.pendingItem(lowInvoiceId)).toHaveCount(0);
  await expect(approvalsPage.pendingItem(highInvoiceId)).toContainText(
    'First approved by demo.manager (demo.manager@example.com)'
  );

  await signInPage.signOut();
  await signInPage.signIn('demo.director', 'ChangeMe123!');
  await expect(page.getByRole('tab', { name: /^Invoices/ })).toHaveAttribute('aria-selected', 'true');
  await expect(page.getByRole('tabpanel', { name: /^Invoices/ })).toBeVisible();
});

test('load and decision errors are shown in visible alerts', async ({ page }) => {
  const signInPage = new SignInPage(page);
  const invoicesPage = new InvoicesPage(page);
  const approvalsPage = new ApprovalsPage(page);
  const customerName = `Denied decision ${Date.now()}`;

  await page.goto('/');
  await signInPage.signIn('demo.requester', 'ChangeMe123!');
  await invoicesPage.createInvoice(customerName, 10_000);
  const invoiceId = await invoicesPage.invoiceIdFor(customerName);
  await invoicesPage.submitInvoice(customerName);
  await signInPage.signOut();

  await page.route('**/api/invoices/projects', (route) =>
    route.fulfill({
      status: 500,
      contentType: 'application/json',
      body: JSON.stringify({ error: 'Projects are unavailable right now.' })
    })
  );
  await signInPage.signIn('demo.manager', 'ChangeMe123!');
  await expect(invoicesPage.alert).toHaveText('Projects are unavailable right now.');
  await expect(invoicesPage.alert).toBeVisible();

  await page.route(`**/api/approvals/${invoiceId}/approve`, (route) =>
    route.fulfill({
      status: 403,
      contentType: 'application/json',
      body: JSON.stringify({ error: 'This invoice is waiting for demo.director.' })
    })
  );
  await approvalsPage.open();
  await approvalsPage.requestApproval(invoiceId);
  await approvalsPage.confirmDecision('Approve');
  await expect(approvalsPage.decisionAlert).toHaveText('This invoice is waiting for demo.director.');
  await expect(approvalsPage.decisionAlert).toBeInViewport();
});

test('invoice table fits a 1280px window without page-wide horizontal scrolling', async ({ page }) => {
  const signInPage = new SignInPage(page);

  await page.setViewportSize({ width: 1280, height: 800 });
  await page.goto('/');
  await signInPage.signIn('demo.admin', 'ChangeMe123!');
  const table = page.getByRole('table', { name: 'Invoices in your scope' });
  await expect(table).toBeVisible();

  const overflow = await table.evaluate((tableElement) => {
    const root = document.documentElement;
    const wrap = tableElement.parentElement!;
    return {
      page: root.scrollWidth - root.clientWidth,
      table: wrap.scrollWidth - wrap.clientWidth
    };
  });
  expect(overflow.page).toBeLessThanOrEqual(0);
  expect(overflow.table).toBeLessThanOrEqual(0);
});
