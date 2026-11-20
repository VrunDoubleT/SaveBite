import { readFile, writeFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { chromium, expect } from '@playwright/test';
const root=new URL('../../docs/screenshots/',import.meta.url);
const report=JSON.parse(await readFile(new URL('capture-results.json',root),'utf8'));
const total=report.collection==='UC-001-100'?100:50;
expect(report.useCases).toHaveLength(total);
const files=[];
for(let i=0;i<total;i++){
 const uc=report.useCases[i];expect(uc.id).toBe('UC-'+String(i+1).padStart(3,'0'));expect(uc.status,uc.id).toBe('passed');expect(uc.frames.length,uc.id).toBeGreaterThan(0);
 const numbers=[];
 for(const frame of uc.frames){
  const data=await readFile(new URL(frame.file,root));
  expect(data.subarray(1,4).toString()).toBe('PNG');
  const width=data.readUInt32BE(16),height=data.readUInt32BE(20);
  expect({width,height}).toEqual(frame.viewport);
  for(const m of frame.marks){numbers.push(m.number);expect(m.description.length).toBeGreaterThan(5);expect(m.rect.x).toBeGreaterThanOrEqual(5);expect(m.rect.y).toBeGreaterThanOrEqual(5);expect(m.rect.x+m.rect.width).toBeLessThanOrEqual(width-5);expect(m.rect.y+m.rect.height).toBeLessThanOrEqual(height-5);}
  files.push({file:frame.file,width,height,sha256:createHash('sha256').update(data).digest('hex')});
 }
 expect(numbers).toEqual(Array.from({length:numbers.length},(_,j)=>j+1));
 const descriptions=[uc.precondition,...uc.frames.flatMap(f=>f.marks.map(m=>m.description))].join(' ');
 expect(descriptions).not.toMatch(/[ăâđêôơưĂÂĐÊÔƠƯ\u1ea0-\u1ef9]/);
 expect(descriptions).not.toMatch(/customer@gmail|guide\.\d+|Alex Morgan|Value Meal Bundles/);
 expect(uc.checks.filter(c=>c.status>=400),uc.id).toHaveLength(0);
}
expect(report.useCases[0].frames).toHaveLength(1);
const orderInfo=report.useCases[32].frames[1].marks[0].rect;
expect(orderInfo.width).toBeGreaterThan(1000);expect(orderInfo.height).toBeGreaterThan(700);
const browser=await chromium.launch();
try{
 const page=await browser.newPage();
 await page.goto(new URL('index.html',root).href);
 await page.locator('img').evaluateAll(imgs=>imgs.forEach(img=>img.loading='eager'));
 await page.waitForFunction(()=>[...document.images].every(img=>img.complete&&img.naturalWidth>0));
 expect(await page.locator('[data-usecase]').count()).toBe(total);
 expect(await page.locator('[data-usecase-description]').count()).toBe(total);
 expect(await page.locator('img').count()).toBe(files.length);
 const ordered=await page.locator('[data-usecase]').evaluateAll(sections=>sections.every(s=>{const desc=s.querySelector('[data-usecase-description]');return desc&&[...s.querySelectorAll('img')].every(img=>Boolean(img.compareDocumentPosition(desc)&Node.DOCUMENT_POSITION_FOLLOWING));}));
 expect(ordered).toBeTruthy();
}finally{await browser.close();}
const validation={validatedAt:new Date().toISOString(),useCases:total,screenshots:files.length,allPassed:true,checks:['All use cases have captures','One login screenshot','Continuous marker numbering','Visible target bounds','Image dimensions match recorded viewports','English generic instructions','All screenshots before the description within each use case','No failed recorded mutations','All gallery images load','UC-033 frame 2 surrounds complete order information'],files};
await writeFile(new URL('validation-results.json',root),JSON.stringify(validation,null,2));
console.log(`Validated ${total} use cases and ${files.length} annotated screenshots.`);
