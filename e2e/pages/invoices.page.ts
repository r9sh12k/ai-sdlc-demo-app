import type { Locator, Page } from '@playwright/test';

export class InvoicesPage {
  constructor(private readonly page: Page) {}

  async createInvoice(customerName: string, amount: number): Promise<void> {
    await this.page.getByLabel('Customer name').fill(customerName);
    await this.page.getByLabel('Amount').fill(String(amount));
    await this.page.getByRole('button', { name: 'Create invoice' }).click();
  }

  async invoiceIdFor(customerName: string): Promise<string> {
    const invoiceRow = this.invoiceRow(customerName);
    await invoiceRow.waitFor({ state: 'visible' });
    return (await invoiceRow.getByRole('cell').nth(0).innerText()).trim();
  }

  async submitInvoice(customerName: string): Promise<void> {
    const invoiceId = await this.invoiceIdFor(customerName);
    await this.invoiceRow(customerName)
      .getByRole('button', { name: `Submit invoice ${invoiceId} for approval` })
      .click();
  }

  statusFor(customerName: string): Locator {
    return this.invoiceRow(customerName).getByRole('cell').nth(3);
  }

  async open(): Promise<void> {
    await this.page.getByRole('tab', { name: 'Invoices' }).click();
  }

  private invoiceRow(customerName: string): Locator {
    return this.page.getByRole('row').filter({ hasText: customerName });
  }
}
