import { expect, test } from '@playwright/test';
import { ApprovalsPage } from '../pages/approvals.page';
import { InvoicesPage } from '../pages/invoices.page';
import { SignInPage } from '../pages/sign-in.page';

test('a Manager cannot approve an invoice above 50,000', async ({ page }) => {
  const signInPage = new SignInPage(page);
  const invoicesPage = new InvoicesPage(page);
  const approvalsPage = new ApprovalsPage(page);
  const customerName = `Approval limit ${Date.now()}`;

  await page.goto('/');
  await signInPage.signIn('demo.manager', 'ChangeMe123!');
  await invoicesPage.createInvoice(customerName, 75_000);
  const invoiceId = await invoicesPage.invoiceIdFor(customerName);
  await invoicesPage.submitInvoice(customerName);
  await expect(invoicesPage.statusFor(customerName)).toHaveText('Pending Approval');

  await approvalsPage.open();
  await approvalsPage.approveInvoice(invoiceId);
  await expect(approvalsPage.decisionAlert).toHaveText(
    'Managers may approve invoices up to 50,000.00.'
  );

  await invoicesPage.open();
  await expect(invoicesPage.statusFor(customerName)).toHaveText('Pending Approval');

  await signInPage.signOut();
  await expect(signInPage.signOutButton).toBeHidden();
});
