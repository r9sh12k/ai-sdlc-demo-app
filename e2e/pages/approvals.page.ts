import type { Locator, Page } from '@playwright/test';

export class ApprovalsPage {
  readonly decisionAlert: Locator;

  constructor(private readonly page: Page) {
    this.decisionAlert = page.getByRole('alert');
  }

  async open(): Promise<void> {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
  }

  async approveInvoice(invoiceId: string): Promise<void> {
    await this.page.getByRole('button', { name: `Approve invoice ${invoiceId}` }).click();
  }
}
