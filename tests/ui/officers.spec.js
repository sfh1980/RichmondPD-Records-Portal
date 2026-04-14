const { test, expect } = require("@playwright/test");
const { login, waitForToast } = require("./helpers");

test("deactivate and reactivate an officer", async ({ page }) => {
  await login(page);

  await page.getByRole("button", { name: "OFFICERS" }).click();
  await expect(page.locator("#view-officers")).toBeVisible();

  const activeRow = page.locator("#officers-body tr").first();
  const badgeNumber = (await activeRow.locator(".case-num").textContent()).trim();

  await activeRow.getByRole("button", { name: "DEACTIVATE" }).click();
  const confirmModal = page.locator("#confirm-modal");
  await expect(confirmModal).toBeVisible();
  await confirmModal.getByRole("button", { name: "DEACTIVATE" }).click();
  await waitForToast(page, "Officer deactivated.");

  await expect(page.locator("#officers-body tr", { hasText: badgeNumber })).toHaveCount(0);

  await page.locator("#officers-filter").selectOption("inactive");
  const inactiveRow = page.locator("#officers-body tr", { hasText: badgeNumber }).first();
  await expect(inactiveRow).toContainText("INACTIVE");

  await inactiveRow.getByRole("button", { name: "REACTIVATE" }).click();
  await expect(confirmModal).toBeVisible();
  await confirmModal.getByRole("button", { name: "REACTIVATE" }).click();
  await waitForToast(page, "Officer reactivated.");

  await page.locator("#officers-filter").selectOption("active");
  await expect(page.locator("#officers-body tr", { hasText: badgeNumber }).first()).toContainText("ACTIVE");
});
