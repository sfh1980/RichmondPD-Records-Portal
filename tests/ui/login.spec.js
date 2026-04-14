const { test, expect } = require("@playwright/test");
const { login } = require("./helpers");

test("login loads dashboard and records workspace", async ({ page }) => {
  await login(page);

  await expect(page.locator(".topbar-name")).toContainText("RVA - RECORDS PORTAL");
  await expect(page.locator("#stat-open")).not.toHaveText("-");
  await expect(page.locator("#view-incidents")).toBeVisible();
  await expect(page.locator("#incidents-body tr").first()).toContainText("EDIT");
  await expect(page.getByRole("button", { name: "OFFICERS" })).toBeVisible();
});
