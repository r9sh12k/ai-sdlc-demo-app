import type { Locator, Page } from '@playwright/test';

export class InvoicesPage {
  readonly createButton: Locator;
  readonly notice: Locator;
  readonly alert: Locator;

  constructor(private readonly page: Page) {
    const panel = page.getByRole('tabpanel', { name: /^Invoices/ });
    this.createButton = page.getByRole('button', { name: 'Create invoice' });
    this.notice = panel.getByRole('status');
    this.alert = panel.getByRole('alert');
  }

  async fillInvoice(
    customerName: string,
    amount: number,
    projectId = 'PRJ-PORTAL'
  ): Promise<void> {
    await this.page.getByLabel('Customer name').fill(customerName);
    await this.page.getByLabel('Amount').fill(String(amount));
    await this.page.getByLabel('Project').selectOption(projectId);
  }

  async createInvoice(
    customerName: string,
    amount: number,
    projectId = 'PRJ-PORTAL'
  ): Promise<void> {
    await this.fillInvoice(customerName, amount, projectId);
    await this.createButton.click();
  }

  submitButtonFor(invoiceId: string): Locator {
    return this.page.getByRole('button', { name: `Submit invoice ${invoiceId} for approval` });
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
    return this.invoiceRow(customerName).getByRole('cell').nth(8);
  }

  invoiceCell(customerName: string, columnIndex: number): Locator {
    return this.invoiceRow(customerName).getByRole('cell').nth(columnIndex);
  }

  async projectOptions(): Promise<string[]> {
    return this.page.getByLabel('Project').getByRole('option').allTextContents();
  }

  invoiceRows() {
    return this.page.getByRole('table', { name: 'Invoices in your scope' }).getByRole('row');
  }

  async open(): Promise<void> {
    await this.page.getByRole('tab', { name: /^Invoices/ }).click();
  }

  private invoiceRow(customerName: string): Locator {
    return this.page.getByRole('row').filter({ hasText: customerName });
  }
}
