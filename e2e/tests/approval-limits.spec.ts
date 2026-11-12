import { expect, test } from '@playwright/test';
import { ApprovalsPage } from '../pages/approvals.page';
import { InvoicesPage } from '../pages/invoices.page';
import { SignInPage } from '../pages/sign-in.page';

test('project allocation and invoice lists follow requester and organizational scope', async ({ page }) => {
  const signInPage = new SignInPage(page);
  const invoicesPage = new InvoicesPage(page);
  const portalCustomer = `Portal scope ${Date.now()}`;
  const erpCustomer = `ERP scope ${Date.now()}`;

  await page.goto('/');
  await signInPage.signIn('demo.requester', 'ChangeMe123!');
  expect(await invoicesPage.projectOptions()).toEqual([
    'Select a project',
    'PRJ-PORTAL',
    'PRJ-MOBILE'
  ]);
  await invoicesPage.createInvoice(portalCustomer, 10_000);
  await invoicesPage.submitInvoice(portalCustomer);
  await expect(invoicesPage.invoiceRows().filter({ hasText: portalCustomer })).toHaveCount(1);
  await expect(invoicesPage.statusFor(portalCustomer)).toHaveText('Pending Approval');
  await expect(invoicesPage.invoiceCell(portalCustomer, 3)).toHaveText('PRJ-PORTAL');
  await expect(invoicesPage.invoiceCell(portalCustomer, 4))
    .toHaveText('demo.requester (demo.requester@example.com)');
  await expect(invoicesPage.invoiceCell(portalCustomer, 5))
    .toHaveText('demo.manager (demo.manager@example.com)');
  await expect(invoicesPage.invoiceCell(portalCustomer, 6)).toHaveText('Not applicable');
  await expect(invoicesPage.invoiceCell(portalCustomer, 7))
    .toHaveText('demo.manager (demo.manager@example.com)');
  for (const column of [
    'Project',
    'Requested By',
    'Approval 1',
    'Approval 2',
    'Waiting on',
    'Status',
    'Rejection reason'
  ]) {
    await expect(page.getByRole('columnheader', { name: column })).toBeVisible();
  }
  await expect(page.getByRole('tab', { name: 'Approvals' })).toBeHidden();

  await signInPage.signOut();
  await signInPage.signIn('demo.requester2', 'ChangeMe123!');
  expect(await invoicesPage.projectOptions()).toEqual([
    'Select a project',
    'PRJ-ERP'
  ]);
  await expect(invoicesPage.invoiceRows().filter({ hasText: portalCustomer })).toHaveCount(0);
  await invoicesPage.createInvoice(erpCustomer, 30_000, 'PRJ-ERP');
  await expect(invoicesPage.invoiceRows().filter({ hasText: erpCustomer })).toHaveCount(1);

  await signInPage.signOut();
  await signInPage.signIn('demo.manager', 'ChangeMe123!');
  await expect(invoicesPage.invoiceRows().filter({ hasText: portalCustomer })).toHaveCount(1);
  await expect(invoicesPage.invoiceRows().filter({ hasText: erpCustomer })).toHaveCount(0);
});

test('only routed users can act and high-value approval requires a distinct second approver', async ({ page }) => {
  const signInPage = new SignInPage(page);
  const invoicesPage = new InvoicesPage(page);
  const approvalsPage = new ApprovalsPage(page);
  const lowCustomer = `BU routed ${Date.now()}`;
  const highCustomer = `Two step ${Date.now()}`;

  await page.goto('/');
  await signInPage.signIn('demo.requester', 'ChangeMe123!');
  await invoicesPage.createInvoice(lowCustomer, 75_000);
  const lowInvoiceId = await invoicesPage.invoiceIdFor(lowCustomer);
  await invoicesPage.submitInvoice(lowCustomer);
  await invoicesPage.createInvoice(highCustomer, 600_000);
  const highInvoiceId = await invoicesPage.invoiceIdFor(highCustomer);
  await invoicesPage.submitInvoice(highCustomer);

  await signInPage.signOut();
  await signInPage.signIn('demo.manager', 'ChangeMe123!');
  await approvalsPage.open();
  await expect(
    page.getByRole('listitem').filter({ hasText: lowInvoiceId })
  ).toContainText('Waiting on: demo.director (demo.director@example.com)');
  await expect(
    page.getByRole('button', { name: `Approve invoice ${lowInvoiceId}` })
  ).toHaveCount(0);

  await approvalsPage.requestApproval(highInvoiceId);
  await expect(approvalsPage.confirmationDialog).toBeVisible();
  await expect(approvalsPage.confirmationDialog).toContainText(
    `Are you sure you want to approve invoice ${highInvoiceId}?`
  );
  await approvalsPage.cancelDecision();
  await invoicesPage.open();
  await expect(invoicesPage.statusFor(highCustomer)).toHaveText('Pending Approval');

  await approvalsPage.open();
  await approvalsPage.requestApproval(highInvoiceId);
  await approvalsPage.confirmDecision('Approve');
  await expect(
    page.getByRole('listitem').filter({ hasText: highInvoiceId })
  ).toContainText('Waiting on: demo.director (demo.director@example.com)');
  await expect(
    page.getByRole('button', { name: `Approve invoice ${highInvoiceId}` })
  ).toHaveCount(0);

  await signInPage.signOut();
  await signInPage.signIn('demo.director', 'ChangeMe123!');
  await approvalsPage.open();
  await approvalsPage.requestApproval(lowInvoiceId);
  await approvalsPage.confirmDecision('Approve');
  await approvalsPage.requestApproval(highInvoiceId);
  await approvalsPage.confirmDecision('Approve');
  await invoicesPage.open();
  await expect(invoicesPage.statusFor(lowCustomer)).toHaveText('Approved');
  await expect(invoicesPage.statusFor(highCustomer)).toHaveText('Approved');
});

test('Admin can reject any pending invoice with a reason and both decisions require confirmation', async ({ page }) => {
  const signInPage = new SignInPage(page);
  const invoicesPage = new InvoicesPage(page);
  const approvalsPage = new ApprovalsPage(page);
  const customerName = `Admin rejection ${Date.now()}`;
  const rejectionReason = 'Duplicate invoice.';

  await page.goto('/');
  await signInPage.signIn('demo.requester', 'ChangeMe123!');
  await invoicesPage.createInvoice(customerName, 10_000);
  const invoiceId = await invoicesPage.invoiceIdFor(customerName);
  await invoicesPage.submitInvoice(customerName);

  await signInPage.signOut();
  await signInPage.signIn('demo.admin', 'ChangeMe123!');
  await approvalsPage.open();
  await approvalsPage.requestRejection(invoiceId, '');
  await expect(approvalsPage.reasonErrorDialog).toBeVisible();
  await approvalsPage.dismissReasonError();

  await approvalsPage.requestRejection(invoiceId, rejectionReason);
  await expect(approvalsPage.confirmationDialog).toBeVisible();
  await approvalsPage.cancelDecision();
  await invoicesPage.open();
  await expect(invoicesPage.statusFor(customerName)).toHaveText('Pending Approval');

  await approvalsPage.open();
  await approvalsPage.requestRejection(invoiceId, rejectionReason);
  await approvalsPage.confirmDecision('Reject');
  await invoicesPage.open();
  await expect(invoicesPage.statusFor(customerName)).toHaveText('Rejected');
  await expect(invoicesPage.invoiceCell(customerName, 9)).toHaveText(
    `demo.admin (demo.admin@example.com): ${rejectionReason}`
  );
});
