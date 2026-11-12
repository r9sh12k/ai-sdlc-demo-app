import type { Locator, Page } from '@playwright/test';

export class ApprovalsPage {
  readonly decisionAlert: Locator;
  readonly permissionDialog: Locator;
  readonly permissionMessage: Locator;
  readonly confirmationDialog: Locator;
  readonly reasonErrorDialog: Locator;
  readonly approvalItems: Locator;
  readonly notice: Locator;
  readonly pendingList: Locator;
  readonly decidedList: Locator;

  constructor(private readonly page: Page) {
    const panel = page.getByRole('tabpanel', { name: /^Approvals/ });
    this.decisionAlert = panel.getByRole('alert');
    this.notice = panel.getByRole('status');
    this.pendingList = panel.getByRole('list', { name: 'Pending' });
    this.decidedList = panel.getByRole('list', { name: 'Decided' });
    this.permissionDialog = page.getByRole('dialog', { name: 'Permission denied' });
    this.permissionMessage = this.permissionDialog.getByRole('status');
    this.confirmationDialog = page.getByRole('dialog', { name: /Confirm/ });
    this.reasonErrorDialog = page.getByRole('dialog', { name: 'Rejection reason required' });
    this.approvalItems = panel.getByRole('listitem');
  }

  pendingItem(invoiceId: string): Locator {
    return this.pendingList.getByRole('listitem').filter({ hasText: invoiceId });
  }

  decidedItem(invoiceId: string): Locator {
    return this.decidedList.getByRole('listitem').filter({ hasText: invoiceId });
  }

  async open(): Promise<void> {
    await this.page.getByRole('tab', { name: /^Approvals/ }).click();
  }

  async requestApproval(invoiceId: string): Promise<void> {
    await this.page.getByRole('button', { name: `Approve invoice ${invoiceId}` }).click();
  }

  async requestRejection(invoiceId: string, reason: string): Promise<void> {
    const reasonInput = this.page.getByLabel(`Rejection reason for ${invoiceId}`);
    if (reason) await reasonInput.fill(reason);
    await this.page.getByRole('button', { name: `Reject invoice ${invoiceId}` }).click();
  }

  async confirmDecision(action: 'Approve' | 'Reject'): Promise<void> {
    await this.confirmationDialog.getByRole('button', { name: action }).click();
  }

  async cancelDecision(): Promise<void> {
    await this.confirmationDialog.getByRole('button', { name: 'Cancel' }).click();
  }

  async dismissReasonError(): Promise<void> {
    await this.reasonErrorDialog.getByRole('button', { name: 'Close' }).click();
  }

  async dismissPermissionDialog(): Promise<void> {
    await this.permissionDialog.getByRole('button', { name: 'Close' }).click();
  }
}
