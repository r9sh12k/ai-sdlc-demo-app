import type { Locator, Page } from '@playwright/test';

export class SignInPage {
  readonly signOutButton: Locator;

  constructor(private readonly page: Page) {
    this.signOutButton = page.getByRole('button', { name: 'Sign out' });
  }

  async signIn(username: string, password: string): Promise<void> {
    await this.page.getByLabel('Username').fill(username);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
  }

  async signOut(): Promise<void> {
    await this.signOutButton.click();
  }
}
