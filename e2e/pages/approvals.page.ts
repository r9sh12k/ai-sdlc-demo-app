import type { Locator, Page } from '@playwright/test';

export class ApprovalsPage {
  readonly decisionAlert: Locator;
  readonly permissionDialog: Locator;
  readonly permissionMessage: Locator;

  constructor(private readonly page: Page) {
    this.decisionAlert = page.getByRole('alert');
    this.permissionDialog = page.getByRole('dialog', { name: 'Permission denied' });
    this.permissionMessage = this.permissionDialog.getByRole('status');
  }

  async open(): Promise<void> {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
  }

  async approveInvoice(invoiceId: string): Promise<void> {
    await this.page.getByRole('button', { name: `Approve invoice ${invoiceId}` }).click();
  }

  async dismissPermissionDialog(): Promise<void> {
    await this.permissionDialog.getByRole('button', { name: 'Close' }).click();
  }
}
