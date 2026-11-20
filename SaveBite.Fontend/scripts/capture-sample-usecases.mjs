import { chromium, expect } from '@playwright/test';
import { mkdir, rm } from 'node:fs/promises';
import { publishReport } from './render-usecase-report.mjs';
import { captureAnnotated } from './screenshot-annotations.mjs';

const baseURL = process.env.CAPTURE_BASE_URL ?? 'http://127.0.0.1:5173';
const root = new URL('../../docs/screenshots/', import.meta.url);
await mkdir(root, { recursive: true });
await rm(new URL('UC-001/02.png', root), { force: true });
const browser = await chromium.launch();
const report = { baseURL, capturedAt: new Date().toISOString(), viewport: { width: 1600, height: 1100 }, useCases: [] };
const escaped = s => String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
let current;
async function start(id, title, actor, precondition) {
  current = { id, title, actor, precondition, status: 'running', frames: [], checks: [] };
  report.useCases.push(current);
  await mkdir(new URL(id + '/', root), { recursive: true });
}
async function frame(page, title, marks) {
  const file = `${current.id}/${String(current.frames.length + 1).padStart(2, '0')}.png`;
  const targets = await captureAnnotated(page, new URL(file, root).pathname, marks);
  current.frames.push({ title, file, url: page.url(), marks: targets });
  console.log(`${current.id} ${title}`);
}
function mark(number, target, description) { return { number, target, description }; }
async function open(page, path) { await page.goto(baseURL + path, { waitUntil: 'networkidle' }); }
async function login(page, email) {
  await open(page, '/login');
  await page.getByRole('textbox', { name: 'Email', exact: true }).fill(email);
  await page.getByRole('textbox', { name: 'Password', exact: true }).fill(process.env.CAPTURE_DEMO_PASSWORD ?? 'SaveBite@123');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.waitForURL(url => url.pathname !== '/login');
  await page.waitForLoadState('networkidle');
}
async function api(page, path) {
  const token = await page.evaluate(() => localStorage.getItem('savebite-access-token'));
  const response = await page.request.get(baseURL + '/api' + path, { headers: { Authorization: `Bearer ${token}` } });
  expect(response.ok(), `GET ${path}`).toBeTruthy();
  return (await response.json()).data;
}
async function perform(page, method, path, action) {
  const responsePromise = page.waitForResponse(r => r.request().method() === method && new URL(r.url()).pathname === '/api' + path);
  await action();
  const response = await responsePromise;
  expect(response.ok(), `${method} ${path}`).toBeTruthy();
  current.checks.push({ method, path, status: response.status() });
  return response;
}
async function publish() { await publishReport(report, root); }

try {
  const context = await browser.newContext({ viewport: report.viewport, reducedMotion: 'reduce' });
  const page = await context.newPage();
  page.setDefaultTimeout(20000);
  await start('UC-001', 'Login', 'Customer', 'Your account is verified and active.');
  await open(page, '/login');
  await page.getByRole('textbox', { name: 'Email', exact: true }).fill('customer@gmail.com');
  await page.getByRole('textbox', { name: 'Password', exact: true }).fill(process.env.CAPTURE_DEMO_PASSWORD ?? 'SaveBite@123');
  await frame(page, 'Enter credentials and sign in', [
    mark(1, page.getByRole('textbox', { name: 'Email', exact: true }), 'Enter your email.'),
    mark(2, page.getByRole('textbox', { name: 'Password', exact: true }), 'Enter your password.'),
    mark(3, page.getByRole('button', { name: 'Sign in', exact: true }), 'Click Sign in to submit the credentials.'),
  ]);
  await perform(page, 'POST', '/auth/sessions', () => page.getByRole('button', { name: 'Sign in', exact: true }).click());
  await page.waitForURL(url => url.pathname !== '/login');
  await page.waitForLoadState('networkidle');
  expect((await api(page, '/auth/me')).email).toBe('customer@gmail.com');
  current.postcondition = 'You are signed in and redirected to the home page.';
  current.status = 'passed';
  await publish();

  await start('UC-015', 'Edit Profile', 'Customer', 'You are signed in and the Profile page is open.');
  await open(page, '/account/profile');
  const before = await api(page, '/auth/me');
  await frame(page, 'Open the profile editor', [mark(1, page.getByRole('button', { name: 'Edit profile', exact: true }), 'Click Edit profile on the personal profile card.')]);
  await page.getByRole('button', { name: 'Edit profile', exact: true }).click();
  const newName = before.fullName === 'Alex Morgan' ? 'Alex Taylor' : 'Alex Morgan';
  const newPhone = before.phone === '0912345678' ? '0987654321' : '0912345678';
  await page.getByRole('textbox', { name: 'Full name', exact: true }).fill(newName);
  await page.getByRole('textbox', { name: 'Phone number', exact: true }).fill(newPhone);
  await frame(page, 'Update personal information', [
    mark(2, page.getByRole('textbox', { name: 'Full name', exact: true }), 'Enter your full name.'),
    mark(3, page.getByRole('textbox', { name: 'Phone number', exact: true }), 'Enter your phone number.'),
    mark(4, page.getByRole('button', { name: 'Save changes', exact: true }), 'Click Save changes to save the profile.'),
  ]);
  await perform(page, 'PUT', '/users/me', () => page.getByRole('button', { name: 'Save changes', exact: true }).click());
  await expect(page.getByRole('button', { name: 'Edit profile', exact: true })).toBeVisible();
  await page.reload({ waitUntil: 'networkidle' });
  await expect(page.locator('#profile-heading')).toHaveText(newName);
  const after = await api(page, '/auth/me');
  expect(after.fullName).toBe(newName); expect(after.phone).toBe(newPhone);
  await frame(page, 'Profile changes remain saved after reloading', [mark(5, page.locator('[aria-labelledby="profile-heading"]'), 'Confirm that the profile shows the new name and phone number. Both values remain saved after reloading the page.')]);
  current.checks.push({ method: 'GET', path: '/auth/me (verified name and phone after reload)', status: 200 });
  current.previousDemoProfile = { fullName: before.fullName, phone: before.phone };
  current.status = 'passed';
  await publish();

  await start('UC-033', 'View Order Details', 'Customer', 'You are signed in and have orders in your order history.');
  await open(page, '/account/orders');
  const details = page.locator('a[href="/orders/00000000-0000-0000-0000-000000000202"]');
  await expect(details).toBeVisible();
  const detailPath = await details.getAttribute('href');
  await frame(page, 'Select an order from the order history', [mark(1, details, 'Click View details for the order you want to view.')]);
  await details.click();
  await page.waitForURL(url => url.pathname === detailPath);
  await page.waitForLoadState('networkidle');
  const order = await api(page, detailPath);
  expect(order.id).toBe(detailPath.split('/').at(-1));
  await frame(page, 'Review the selected order', [mark(2, page.getByRole('heading', { name: 'Order details', exact: true }), 'Review the details of the selected order.')]);
  const summary = page.locator('[aria-label="Payment summary"]');
  await frame(page, 'Review the payment summary on the order details page', [mark(3, summary, 'Review the order total, amount paid, amount due, and payment method in Payment summary.')]);
  current.checks.push({ method: 'GET', path: detailPath + ' (verified selected order ID)', status: 200 });
  current.status = 'passed';
  await publish();
  await context.close();

  const adminContext = await browser.newContext({ viewport: report.viewport, reducedMotion: 'reduce' });
  const admin = await adminContext.newPage();
  admin.setDefaultTimeout(20000);
  await login(admin, 'admin@gmail.com');
  await start('UC-133', 'Create Category', 'Admin', 'You are signed in as an administrator and the Food Categories page is open.');
  await open(admin, '/admin/categories');
  const coverage = await api(admin, '/categories?page=1&pageSize=10');
  expect(coverage.totalItems).toBeGreaterThanOrEqual(32);
  expect(coverage.items.length).toBe(10);
  report.dataCoverage = { categories: coverage.totalItems, categoryRowsPerPage: coverage.items.length };
  await frame(admin, 'Open the category creation form', [mark(1, admin.getByRole('button', { name: 'Add Category', exact: true }), 'Click Add Category on the Food Categories page.')]);
  await admin.getByRole('button', { name: 'Add Category', exact: true }).click();
  const modal = admin.getByRole('dialog', { name: 'Create Category', exact: true });
  const categoryName = 'Value Meal Bundles';
  const categoryDescription = 'Freshly prepared meal bundles at reduced prices to help prevent food waste.';
  await modal.getByPlaceholder('Enter category name').fill(categoryName);
  await modal.getByPlaceholder('Brief description (optional)').fill(categoryDescription);
  await expect(modal.getByRole('checkbox', { name: 'Active on store' })).toBeChecked();
  await frame(admin, 'Complete the Create Category dialog', [
    mark(2, modal.getByPlaceholder('Enter category name'), 'Enter a category name.'),
    mark(3, modal.getByPlaceholder('Brief description (optional)'), 'Enter a description for the category.'),
    mark(4, modal.locator('.flex.items-center.gap-3.pt-2'), 'Keep Active on store selected to activate the category.'),
    mark(5, modal.getByRole('button', { name: 'Save Category', exact: true }), 'Click Save Category to create the category.'),
  ]);
  await perform(admin, 'POST', '/categories', () => modal.getByRole('button', { name: 'Save Category', exact: true }).click());
  await expect(modal).toBeHidden();
  const createdRow = admin.getByRole('row').filter({ hasText: categoryName });
  await expect(createdRow).toBeVisible();
  const matching = await api(admin, '/categories?search=' + encodeURIComponent(categoryName));
  const created = matching.items.find(x => x.name === categoryName);
  expect(created).toBeTruthy(); expect(created.isActive).toBe(true);
  await frame(admin, 'Confirm the new category in the populated list', [mark(6, createdRow, 'The dialog closes and the new category appears in the full list with its name, description, and Active status.')]);
  current.createdCategoryId = created.id;
  current.status = 'passed';
  await publish();

  await start('UC-134', 'Update Category', 'Admin', 'You are signed in as an administrator and the category you want to update is visible in the list.');
  await frame(admin, 'Select the category to edit', [mark(1, createdRow.getByRole('button', { name: 'Edit category', exact: true }), 'Click the Edit category icon for the category you want to update.')]);
  await createdRow.getByRole('button', { name: 'Edit category', exact: true }).click();
  const editModal = admin.getByRole('dialog', { name: 'Edit Category', exact: true });
  const updatedName = 'Lunch and Dinner Bundles';
  const updatedDescription = 'Affordable lunch and dinner bundles freshly prepared by nearby stores.';
  await editModal.getByPlaceholder('Enter category name').fill(updatedName);
  await editModal.getByPlaceholder('Brief description (optional)').fill(updatedDescription);
  await frame(admin, 'Update the fields in the Edit Category dialog', [
    mark(2, editModal.getByPlaceholder('Enter category name'), 'Enter the updated category name.'),
    mark(3, editModal.getByPlaceholder('Brief description (optional)'), 'Enter the updated category description.'),
    mark(4, editModal.getByRole('button', { name: 'Save Category', exact: true }), 'Click Save Category to save the changes. Keep the category active.'),
  ]);
  await perform(admin, 'PUT', '/categories/' + created.id, () => editModal.getByRole('button', { name: 'Save Category', exact: true }).click());
  await expect(editModal).toBeHidden();
  await admin.reload({ waitUntil: 'networkidle' });
  const updatedRow = admin.getByRole('row').filter({ hasText: updatedName });
  await expect(updatedRow).toBeVisible();
  const updated = (await api(admin, '/categories?search=' + encodeURIComponent(updatedName))).items.find(x => x.id === created.id);
  expect(updated.name).toBe(updatedName); expect(updated.description).toBe(updatedDescription);
  await frame(admin, 'Confirm the category changes after reloading', [mark(5, updatedRow, 'Confirm that the full list displays the updated name and description. The category retains its original ID and the changes remain saved after reloading.')]);
  current.checks.push({ method: 'GET', path: '/categories (verified same ID and updated fields after reload)', status: 200 });
  current.status = 'passed';
  await publish();
  await adminContext.close();
} catch (error) {
  if (current) { current.status = 'failed'; current.error = error.message; }
  throw error;
} finally {
  await publish();
  await browser.close();
}
