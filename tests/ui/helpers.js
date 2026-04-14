const { expect } = require("@playwright/test");

async function login(page) {
  await page.goto("/");

  if (await page.locator("#login-screen").isVisible()) {
    await page.locator("#inp-user").fill("admin");
    await page.locator("#inp-pass").fill("Password123!");
    await page.locator("#btn-login").click();
  }

  await expect(page.locator("#app")).toBeVisible();
  await expect(page.locator("#topbar-user")).toContainText("OFFICER: ADMIN");
}

async function waitForToast(page, text) {
  const toast = page.locator("#toast");
  await expect(toast).toBeVisible();
  await expect(toast).toContainText(text);
}

module.exports = {
  login,
  waitForToast
};
