const { test, expect } = require("@playwright/test");
const { login, waitForToast } = require("./helpers");

test("edit, archive, and restore an incident", async ({ page }) => {
  await login(page);

  const firstRow = page.locator("#incidents-body tr").first();
  const caseNumber = (await firstRow.locator(".case-num").textContent()).trim();

  await firstRow.getByRole("button", { name: "EDIT" }).click();
  await expect(page.locator("#incident-modal")).toBeVisible();

  const descriptionField = page.locator("#incident-description");
  const originalDescription = await descriptionField.inputValue();
  const updatedDescription = `${originalDescription} [PW]`;

  await descriptionField.fill(updatedDescription);
  await page.getByRole("button", { name: "SAVE CHANGES" }).click();
  await waitForToast(page, "Incident updated.");
  await expect(page.locator("#incident-modal")).toHaveClass(/hidden/);

  const activeRow = page.locator("#incidents-body tr", { hasText: caseNumber }).first();
  await activeRow.getByRole("button", { name: "EDIT" }).click();
  await expect(page.locator("#incident-description")).toHaveValue(updatedDescription);
  await page.getByRole("button", { name: "CANCEL" }).click();

  await activeRow.getByRole("button", { name: "ARCHIVE" }).click();
  const confirmModal = page.locator("#confirm-modal");
  await expect(confirmModal).toBeVisible();
  await confirmModal.getByRole("button", { name: "ARCHIVE" }).click();
  await waitForToast(page, "Incident archived.");
  await expect(page.locator("#incidents-body tr", { hasText: caseNumber })).toHaveCount(0);

  await page.locator("#filter-archived").check();
  const archivedRow = page.locator("#incidents-body tr", { hasText: caseNumber }).first();
  await expect(archivedRow).toContainText("ARCHIVED");

  await archivedRow.getByRole("button", { name: "RESTORE" }).click();
  await expect(confirmModal).toBeVisible();
  await confirmModal.getByRole("button", { name: "RESTORE" }).click();
  await waitForToast(page, "Incident restored.");
  await expect(page.locator("#incidents-body tr", { hasText: caseNumber }).first()).not.toContainText("ARCHIVED");

  await page.locator("#incidents-body tr", { hasText: caseNumber }).first().getByRole("button", { name: "EDIT" }).click();
  await page.locator("#incident-description").fill(originalDescription);
  await page.getByRole("button", { name: "SAVE CHANGES" }).click();
  await waitForToast(page, "Incident updated.");

  await page.locator("#filter-archived").uncheck();
  await expect(page.locator("#incidents-body tr", { hasText: caseNumber }).first()).toBeVisible();
});
