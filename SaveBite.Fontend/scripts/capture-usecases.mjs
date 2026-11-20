import { chromium } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';

const baseURL = process.env.CAPTURE_BASE_URL ?? 'http://127.0.0.1:5173';
const output = new URL('../../docs/screenshots/', import.meta.url);
await mkdir(new URL('guest/', output), { recursive: true });
const browser = await chromium.launch({ headless: true });
try {
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, deviceScaleFactor: 1 });
  const results = [];
  for (const [name, path] of [['01-home', '/'], ['02-sign-in', '/login'], ['03-sign-up', '/register'], ['04-forgot-password', '/forgot-password']]) {
    const failed = [];
    const listener = request => failed.push({ url: request.url(), error: request.failure()?.errorText });
    page.on('requestfailed', listener);
    await page.goto(baseURL + path, { waitUntil: 'networkidle' });
    await page.screenshot({ path: new URL(`guest/${name}.png`, output).pathname, fullPage: true });
    results.push({ name, path, actualUrl: page.url(), failed, text: (await page.locator('body').innerText()).slice(0, 3000) });
    page.off('requestfailed', listener);
  }
  await writeFile(new URL('capture-results.json', output), JSON.stringify(results, null, 2));
  console.log(`Captured ${results.length} public screens.`);
} finally {
  await browser.close();
}
