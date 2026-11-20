import { chromium } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';

const baseURL = process.env.CAPTURE_BASE_URL ?? 'http://127.0.0.1:9999';
const output = new URL('../../docs/screenshots/admin/', import.meta.url);
await mkdir(output, { recursive: true });
const browser = await chromium.launch();
const results = [];
try {
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, deviceScaleFactor: 1 });
  await page.goto(baseURL + '/login');
  await page.getByRole('textbox', { name: 'Email', exact: true }).fill(process.env.CAPTURE_ADMIN_EMAIL ?? 'admin@gmail.com');
  await page.getByRole('textbox', { name: 'Password', exact: true }).fill(process.env.CAPTURE_ADMIN_PASSWORD ?? 'SaveBite@123');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.waitForURL(url => url.pathname.startsWith('/admin'));
  for (const [name, path] of [
    ['01-overview', '/admin/analytics/overview'],
    ['02-users', '/admin/users'],
    ['03-categories', '/admin/categories'],
    ['04-products', '/admin/products'],
    ['05-stores', '/admin/stores'],
    ['06-shop-applications', '/admin/shop-applications'],
    ['07-feedback', '/admin/feedback'],
    ['08-trust', '/admin/trust'],
    ['09-fee-schedules', '/admin/commission-fee-schedules'],
    ['10-fee-statements', '/admin/platform-fees'],
    ['11-payos-settings', '/admin/platform-payos-config'],
    ['12-audit-logs', '/admin/audit-logs'],
    ['13-reviews', '/admin/reviews'],
  ]) {
    const errors = [];
    const listener = response => { if (response.url().includes('/api/') && response.status() >= 400) errors.push({ url: response.url(), status: response.status() }); };
    page.on('response', listener);
    await page.goto(baseURL + path, { waitUntil: 'networkidle' });
    await page.evaluate(() => document.fonts.ready);
    const actualPath = new URL(page.url()).pathname;
    if (actualPath !== path && !(path === '/admin/trust' && actualPath.startsWith(path))) throw new Error(`Unexpected redirect: ${path} -> ${actualPath}`);
    await page.screenshot({ path: new URL(name + '.png', output).pathname, fullPage: true });
    results.push({ name, path, actualPath, errors, text: (await page.locator('body').innerText()).slice(0, 5000) });
    page.off('response', listener);
    console.log(name + ': ' + errors.length + ' API errors');
  }
} finally {
  await writeFile(new URL('capture-results.json', output), JSON.stringify(results, null, 2));
  await browser.close();
}
