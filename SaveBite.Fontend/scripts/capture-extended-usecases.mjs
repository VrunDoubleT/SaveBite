import { chromium, expect } from '@playwright/test';
import { mkdir, readFile, writeFile, readdir } from 'node:fs/promises';
import { captureAnnotated } from './screenshot-annotations.mjs';
import { publishReport } from './render-usecase-report.mjs';
const baseURL=process.env.CAPTURE_BASE_URL??'http://127.0.0.1:5175';
const root=new URL('../../docs/screenshots/',import.meta.url);
await mkdir(root,{recursive:true});
let report;
try{report=JSON.parse(await readFile(new URL('capture-results.json',root),'utf8'));}catch{}
if(!['UC-001-050','UC-001-100'].includes(report?.collection))report={collection:'UC-001-050',baseURL,capturedAt:new Date().toISOString(),viewport:{width:1600,height:1100},useCases:[],fixtures:{}};
if(baseURL !== 'http://127.0.0.1:5175') throw Error('Use only the isolated documentation frontend on port 5175.');
let environment;for(let i=0;i<30;i++){try{const response=await fetch(baseURL+'/api/demo-environment');if(response.ok)environment=await response.json();if(environment)break;}catch{}await new Promise(r=>setTimeout(r,1000));}if(!environment)throw Error('The isolated documentation backend is not ready.');
if(environment.database!=='savebite_uc050'||environment.redisDatabase!==8||environment.rabbitMqVirtualHost!=='uc050'||environment.email!=='local'||environment.uploads!=='local')throw Error('Documentation environment isolation check failed.');
report.environment={...environment,bankDirectoryLabels:'Provider international brand names'};
report.collection='UC-001-100';report.capturedAt=new Date().toISOString();
const f=report.fixtures;
const titles=['Login','Forget Password','Verify Password Reset','Register','Verify Registration','View Nearby Stores','View Store Profile',"View Store’s Flash Deals",'View Store Reviews','View Flash Deals By Current Location','View Flash Deal Details','Logout','Change Password','View Profile','Edit Profile','Update Avatar','View Addresses','Add New Address','Update Address','Delete Address','Set Default Address','View Notifications','Mark Notification as Read','Delete Notification','Mark All Notifications as Read','View Items in Cart','Update Item Quantity','Remove Item from Cart','Add Flash Deal to Cart','Checkout Order','Payment','View Order History','View Order Details','Cancel Order','View Customer Payment History','View Payment Details','View Refund History','Request Refund','Provide Bank Information','Confirm Refund via Email','Withdraw Refund Request','View Refund Details','Add Feedback','View Feedback History','Edit Feedback','Delete Feedback','View Trust Score and Level','Track Level Progress','View Trust Score History','View Level Info & Benefits'];
titles.push('View Penalty History','Track Penalty Recovery Progress','Register Shop','View Shop Registration Status','Edit and Resubmit Shop Registration','Cancel Shop Registration','View Received Staff Invitations','Accept Staff Invitation','Decline Staff Invitation','Follow Shop','Unfollow Shop','Manage Shop Notification Settings','View Followed Shops','View Flash Deals for Shop','View Flash Deal Details for Shop','View Shop Orders','Search Order by ID','View Order Details for Shop','Confirm Order','Reject Order','Update Preparation Status','Complete Order','Generate Remaining Payment QR','View Shop and Staff Info','Leave Shop','View Feedback for Shop','Reply Feedback','Edit Reply','Delete Reply','View Restock Tasks','Make Resale / Restock Decision','View Shop Profile','Edit Business Info','Edit Shop Images and Logo','View Products','View Product Detail','Create New Product','Update Product Info','Delete Product','Update Product Images','Add Attributes Values','Update Variant','Update Product Status','View Product Flash Deal History','Create Flash Deal','Update Flash Deal','Cancel Flash Deal','Unhide Flash Deal','Hide Flash Deal','View Sent Staff Invitations');
const id=n=>'UC-'+String(n).padStart(3,'0');
const demoId=(kind,n)=>`c0500000-0000-0000-${String(kind).padStart(4,'0')}-${String(n).padStart(12,'0')}`;
const shopId='00000000-0000-0000-0000-000000000022';
const browser=await chromium.launch();
let page,context,current;
async function save(){report.useCases.sort((a,b)=>a.id.localeCompare(b.id));await publishReport(report,root);}
async function open(path){await page.goto(baseURL+path,{waitUntil:'domcontentloaded'});await page.locator('main').first().waitFor({state:'visible'});await page.waitForLoadState('networkidle',{timeout:6000}).catch(()=>{});}
const btn=name=>page.getByRole('button',{name,exact:true});
const text=name=>page.getByRole('textbox',{name,exact:true});
const heading=name=>page.getByRole('heading',{name,exact:true});
async function api(path,method='GET',data){const token=await page.evaluate(()=>localStorage.getItem('savebite-access-token'));const r=await page.request.fetch(baseURL+'/api'+path,{method,data,headers:token?{Authorization:'Bearer '+token}:{}});const body=await r.json().catch(()=>({}));if(!r.ok())throw Error(`${method} ${path}: HTTP ${r.status()} ${body.message??''}`);return body.data??body;}
async function login(email='customer@gmail.com',password='SaveBite@123'){await open('/login');await text('Email').fill(email);await text('Password').fill(password);await btn('Sign in').click();await page.waitForURL(u=>u.pathname!='/login',{waitUntil:'domcontentloaded'});await page.waitForLoadState('networkidle',{timeout:6000}).catch(()=>{});}
async function shot(title,specs){
 await page.waitForLoadState('networkidle',{timeout:4000}).catch(()=>{});await ready();await page.evaluate(()=>document.activeElement?.blur());
 for(const [target]of specs)await target.waitFor({state:'visible'});
 if(specs.length===1)await specs[0][0].scrollIntoViewIfNeeded();
 let boxes=await Promise.all(specs.map(async([t])=>t.boundingBox()));
 let min=Math.min(...boxes.map(b=>b.y)),max=Math.max(...boxes.map(b=>b.y+b.height));
 if(!listMode && current.id!=='UC-084' && max-min>950){const height=Math.min(2600,Math.ceil(max-min+260));await page.setViewportSize({width:1600,height});}
 if(current.id==='UC-016'&&current.frames.length===1){const actionBox=await btn('Save changes').boundingBox();await page.evaluate(({y,height})=>window.scrollBy(0,y-(innerHeight-height)/2),actionBox);}else if(current.id==='UC-015'&&current.frames.length===1){const formBox=await page.locator('[aria-labelledby="edit-profile-heading"]').boundingBox();await page.evaluate(({y,height})=>window.scrollBy(0,y-(innerHeight-height)/2),formBox);}else if(current.id==='UC-013'&&current.frames.length===2){const formBox=await page.locator('#change-password-form').boundingBox();await page.evaluate(({y,height})=>window.scrollBy(0,y-(innerHeight-height)/2),formBox);}else if(current.id==='UC-010'&&current.frames.length>0){await page.setViewportSize(report.viewport);const filtersBox=await page.locator('#home-filters').boundingBox();await page.evaluate(y=>window.scrollBy(0,y-100),filtersBox.y);}else if(current.id==='UC-006'&&current.frames.length>0){const mapBox=await page.locator('[aria-label="Nearby stores map"]').boundingBox();await page.evaluate(y=>window.scrollBy(0,y-110),mapBox.y);}else if(listMode||[32,35,37,44,47,48,49,50].includes(Number(current.id.slice(3))))await page.evaluate(()=>window.scrollTo(0,0));else if([52,65,68,75,77,78,79,86,92].includes(Number(current.id.slice(3)))&&current.frames.length===0)await page.evaluate(y=>window.scrollBy(0,y-innerHeight/2),min);else await page.evaluate(y=>window.scrollBy(0,y-90),min);
 await page.locator('main img').evaluateAll(imgs=>Promise.race([Promise.all(imgs.map(img=>img.decode().catch(()=>{}))),new Promise(resolve=>setTimeout(resolve,12000))]));
 if(current.id==='UC-081'){await page.evaluate(()=>{const spacer=document.createElement('div');spacer.style.height=innerHeight+'px';document.querySelector('main').append(spacer);});await specs.at(-1)[0].evaluate(el=>el.scrollIntoView({block:'center',behavior:'instant'}));}
 const marks=specs.map(([target,description],i)=>({clip:true,number:current.frames.reduce((n,frame)=>n+frame.marks.length,0)+i+1,target,description}));
 const file=`${current.id}/${String(current.frames.length+1).padStart(2,'0')}.png`;
 const measured=await captureAnnotated(page,new URL(file,root).pathname,marks);
 current.frames.push({title,file,url:page.url(),viewport:page.viewportSize(),marks:measured});
 console.log(`${current.id}: ${title}`);
 await page.setViewportSize(report.viewport);
 await save();
}
async function clickShot(title,target,description){await shot(title,[[target,description]]);await target.click();}
async function result(title,target,description){await target.waitFor({state:'visible'});await page.waitForLoadState('networkidle',{timeout:5000}).catch(()=>{});await shot(title,[[target,description]]);}
async function otp(email){
 for(let attempt=0;attempt<40;attempt++){
  const files=(await readdir('/tmp/savebite-demo-mail')).sort().reverse();
  for(const name of files.slice(0,30)){
   let s=await readFile('/tmp/savebite-demo-mail/'+name,'utf8');
   if(!s.includes(email))continue;
   s=s.replace(/=\r?\n/g,'').replace(/=([A-F\d]{2})/gi,(_,h)=>String.fromCharCode(parseInt(h,16)));
   const body=s.slice(s.indexOf('\r\n\r\n')+4);
   const match=body.match(/>\s*(\d{6})\s*</)||body.match(/\b(\d{6})\b/);
   if(match)return match[1];
  }
  await new Promise(r=>setTimeout(r,500));
 }
 throw Error('The local inbox did not receive a verification code.');
}
async function location(){await open('/');if(await btn('Use my location').isVisible())await btn('Use my location').click();await expect(page.getByRole('heading',{name:/Today.s flash deals/i})).toBeVisible({timeout:20000});}
async function store(){await open('/shops/'+shopId);}
async function deal(){if(!f.dealPath){await store();f.dealPath=await page.getByRole('link',{name:/^View deal:/}).first().getAttribute('href');}await open(f.dealPath);}
async function ensureCart(){
 if(!f.variants){const data=await api('/shops/'+shopId+'/flash-deals?page=1&pageSize=8');const entries=data.items??data.page?.items??data;const deals=Array.isArray(entries)?entries:[];f.variants=[];for(const d of deals.slice(0,3)){const detail=await api('/flash-deals/'+d.id);for(const v of detail.variants??[])if(v.availableQuantity>3)f.variants.push(v.id);}}
 if(!f.variants?.length){await deal();const data=await api(new URL(page.url()).pathname);f.variants=data.variants.filter(v=>v.availableQuantity>3).map(v=>v.id);}
 const cart=await api('/cart/page');if(!cart.totalQuantity)for(const v of f.variants.slice(0,3))await api('/cart/items','POST',{flashDealVariantId:v,quantity:2});
}
async function notificationPanel(){await open('/account/profile');await page.getByRole('button',{name:/^Notifications/}).first().click();await page.getByRole('dialog',{name:'Notifications',exact:true}).waitFor({state:'visible'});}
async function overview(){await open('/account/trust-score/overview');await expect(page.getByText("Couldn't load trust information",{exact:true})).toHaveCount(0);}
const nav=name=>page.getByRole('link',{name,exact:true});
async function accountList(path,name,cards,filter=false){
 await open(path);await cards.first().waitFor({state:'visible'});
 await page.evaluate(()=>window.scrollTo(0,0));
 const boxes=await cards.evaluateAll(els=>els.map(el=>{const r=el.getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height};}));
 await page.evaluate(boxes=>{const first=boxes[0],last=boxes.at(-1);const el=document.createElement('div');el.dataset.captureList='true';Object.assign(el.style,{position:'absolute',pointerEvents:'none',left:first.x+'px',top:(first.y+scrollY)+'px',width:first.width+'px',height:(Math.min(last.y+last.height,innerHeight-35)-first.y)+'px'});document.body.append(el);},boxes);
 const list=page.locator('[data-capture-list]');
 const specs=[[nav(name),`Click ${name} in the account navigation.`]];
 if(filter)specs.push([page.getByRole('combobox').first(),'Choose a payment status filter.']);
 specs.push([list,'Review the records in the list.']);
 await shot(name+' list',specs);
}
async function middleClick(title,targets,description){
 await targets.first().waitFor({state:'visible'});
 const target=targets.nth(Math.min(1,(await targets.count())-1));
 await target.evaluate(el=>el.scrollIntoView({block:'center',behavior:'instant'}));
 const href=await target.getAttribute('href');await clickShot(title,target,description);if(href)await page.waitForURL(u=>u.pathname===new URL(href,baseURL).pathname,{waitUntil:'domcontentloaded'});
 await page.waitForFunction(()=>![...document.querySelectorAll('main .animate-pulse.bg-neutral-200,main [role="status"][aria-label^="Loading"]')].some(el=>el.getBoundingClientRect().height>0),{},{timeout:30000});
 await page.locator('main img').evaluateAll(imgs=>Promise.race([Promise.all(imgs.map(img=>img.decode().catch(()=>{}))),new Promise(resolve=>setTimeout(resolve,12000))]));
}
async function reviewFields(dialog,editing=false){
 await dialog.locator('textarea').first().waitFor({state:'visible'});
 const stars=dialog.getByRole('button',{name:'Rate 5 out of 5',exact:true});
 for(const star of await stars.all())await star.click();
 for(const comment of await dialog.locator('textarea').all())await comment.fill('Fresh food, careful packaging, and excellent value.');
 await dialog.locator('input[type="file"]').first().setInputFiles({name:'food-review.png',mimeType:'image/png',buffer:await imageFixture()});
 await shot(editing?'Update your feedback':'Write your feedback',[
 [stars.first().locator('..'),'Choose your star rating.'],
 [dialog.locator('textarea').first(),'Enter your feedback.'],
 [dialog.getByRole('button',{name:/Add photo/}).first().locator('..'),'Choose photos to attach to your feedback.'],
 [dialog.locator('button[type="submit"]'),editing?'Save your updated feedback.':'Submit your feedback.']]);
 await dialog.locator('button[type="submit"]').click();await expect(dialog).toBeHidden();
}
async function trustShot(n){
 const tab=n===49?'Score & value history':n===50?'Levels & benefits':'Overview';
 await open('/account/trust-score/'+(n===49?'history':n===50?'levels':'overview'));
 await page.getByRole('tab',{name:new RegExp(tab.replace('&','\\&'))}).click();
 await page.waitForLoadState('networkidle',{timeout:6000}).catch(()=>{});
 const specs=[[nav('Trust Score & Level'),'Click Trust Score & Level in the account navigation.'],[page.getByRole('tab',{name:new RegExp(tab.replace('&','\\&'))}),'Select the '+tab+' tab.']];
 if(n===47){specs.push([page.locator('[aria-label="Your membership"]'),'Review your current membership level, trust score, and account standing.']);specs.push([heading('Gold benefits').locator('..'),'Review the benefits of your current level.']);}
 if(n===48)specs.push([heading('Your next level').locator('..'),'Review your progress and the requirements for the next level.']);
 if(n===49)specs.push([page.locator('[aria-label="Score and value changes"]'),'Review the history of your trust score and accumulated order value.']);
 if(n===50){const cards=page.locator('.trust-panel > div > section');await cards.first().waitFor({state:'visible'});const second=await cards.nth(1).boundingBox();await page.setViewportSize({width:1600,height:Math.ceil(second.y+second.height+12)});await cards.evaluateAll(els=>{const boxes=els.slice(0,2).map(el=>el.getBoundingClientRect());const x=Math.min(...boxes.map(r=>r.x)),y=Math.min(...boxes.map(r=>r.y));const region=document.createElement('div');region.dataset.captureLevels='true';Object.assign(region.style,{position:'absolute',pointerEvents:'none',left:(x+scrollX)+'px',top:(y+scrollY)+'px',width:(Math.max(...boxes.map(r=>r.right))-x)+'px',height:(Math.max(...boxes.map(r=>r.bottom))-y)+'px'});document.body.append(region);});specs.push([page.locator('[data-capture-levels]'),'Review the requirements and benefits of the first two membership levels.']);}
 await shot(n===47?'Your trust score and level':n===48?'Your next level':n===49?'Score and value history':'First two membership levels',specs);
}

// Extended capture recipes
const extId=(kind,n)=>`e1000000-0000-0000-${String(kind).padStart(4,'0')}-${String(n).padStart(12,'0')}`;
const staffBase='/staff/shops/'+shopId;
async function ready(){await page.waitForFunction(()=>![...document.querySelectorAll('main .animate-pulse.bg-neutral-200,main [aria-label^="Loading"]')].some(e=>e.getBoundingClientRect().height>0),{},{timeout:40000});}
async function go(path){await open(path);await ready();}
async function region(targets,key='captureRegion'){
 await page.evaluate(()=>window.scrollTo(0,0));
 await targets.first().waitFor({state:'visible'});
 await targets.evaluateAll((els,key)=>{const boxes=els.map(el=>el.getBoundingClientRect());const x=Math.min(...boxes.map(b=>b.x)),y=Math.min(...boxes.map(b=>b.y));const right=Math.max(...boxes.map(b=>b.right)),bottom=Math.min(innerHeight-30,Math.max(...boxes.map(b=>b.bottom)));const el=document.createElement('div');el.setAttribute('data-'+key,'');Object.assign(el.style,{position:'absolute',pointerEvents:'none',left:(x+scrollX)+'px',top:(y+scrollY)+'px',width:(right-x)+'px',height:(bottom-y)+'px'});document.body.append(el);},key);
 return page.locator('[data-'+key+']').last();
}
async function listScreen(title,navName,items,extra=[]){
 listMode=true;const target=await region(items);const navigation=page.locator('aside a, nav a').filter({hasText:navName}).first();
 await shot(title,[...(await navigation.isVisible()?[[navigation,`Click ${navName} in the navigation.`]]:[]),...extra,[target,'Review the records in the list.']]);listMode=false;
}
async function detail(title){await ready();await result(title,page.locator('main').first(),'Review the complete details and available actions.');}
async function fillAndShot(title,fields,action){
 for(const [loc,value]of fields)if(value!==null)await loc.fill(value);
 await shot(title,[...fields.map(([loc,,description])=>[loc,description]),[action,'Click '+(await action.innerText()).trim()+' to save your changes.']]);
 await action.click();await ready();
}
async function dialogSubmit(title,fields,button){const d=page.getByRole('dialog').last();await d.waitFor({state:'visible'});await fillAndShot(title,fields(d),d.getByRole('button',{name:button,exact:true}));await expect(d).toBeHidden({timeout:30000});}
async function run(n){switch(n){
 case 51:await go('/account/trust-score/penalties');await listScreen('Penalty history','Trust Score & Level',page.locator('[aria-label="Penalty history"]'),[[page.getByRole('tab',{name:/Penalties/}),'Select the Penalties tab.']]);break;
 case 52:await go('/account/trust-score/penalties');await middleClick('Open penalty recovery',btn('View recovery'),'Click View recovery for the penalty you want to inspect.');await result('Penalty recovery progress',page.getByRole('dialog',{name:'Penalty recovery',exact:true}),'Review the recovery requirements, completed orders, and remaining progress.');break;
 case 57:await go('/account/staff-invitations');await listScreen('Received staff invitations','Staff Invitations',page.locator('main article,main section').filter({has:page.getByRole('button',{name:/Accept invitation/})}));break;
 case 58:case 59:await go('/account/staff-invitations');const invitation=page.getByRole('button',{name:n===58?/^Accept invitation/:/^Decline invitation/}).first();await clickShot(n===58?'Accept a staff invitation':'Decline a staff invitation',invitation,n===58?'Click Accept to join the selected shop as a staff member.':'Click Decline for the invitation you do not want to accept.');await ready();await expect(invitation).toHaveCount(0).catch(()=>{});await detail('Updated invitation status');break;
 case 60:await store();await ready();if(await btn('Unfollow store').isVisible())await btn('Unfollow store').click();await clickShot('Follow a shop',btn('Follow store'),'Click Follow store to follow the shop.');await expect(btn('Unfollow store')).toBeVisible();break;
 case 61:await store();await ready();if(await btn('Follow store').isVisible())await btn('Follow store').click();await clickShot('Unfollow a shop',btn('Unfollow store'),'Click Unfollow store to stop following this shop.');await expect(btn('Follow store')).toBeVisible();break;
 case 62:await store();await ready();if(await btn('Follow store').isVisible())await btn('Follow store').click();const notification=page.getByRole('button',{name:/^Turn (on|off) deal notifications/});await clickShot('Change shop deal notifications',notification,'Click the notification icon to turn deal notifications on or off for this shop.');await ready();break;
 case 63:await go('/account/followed-shops');await listScreen('Followed shops','Followed shops',page.locator('main article,main section').filter({has:page.getByRole('link',{name:/Green Kitchen/})}));break;
 case 64:await go(staffBase+'/flash-deals');await listScreen('Shop flash deals','Flash Deals',page.locator('table'));break;
 case 65:await go(staffBase+'/flash-deals');await middleClick('Open a flash deal',btn('View'),'Click View for the flash deal you want to inspect.');await detail('Shop flash deal details');break;
 case 66:await go(staffBase+'/orders');await listScreen('Shop orders','Orders',page.locator('main a').filter({hasText:'View details'}).locator('xpath=ancestor::article[1]'));break;
 case 67:await go(staffBase+'/orders');const search=page.getByPlaceholder('Pickup code or exact order ID');await search.fill(extId(5,3));await ready();await page.waitForTimeout(700);await shot('Find an order by ID',[[page.locator('a[href="'+staffBase+'/orders"]').first(),'Click Orders in the navigation.'],[search,'Enter the order ID or pickup code.'],[page.locator('main').getByText('710003',{exact:true}).locator('xpath=ancestor::article[1]'),'Review the matching order.']]);break;
 case 68:await go(staffBase+'/orders');await middleClick('Open a shop order',page.getByRole('link',{name:'View details',exact:true}),'Click View details for the order you want to inspect.');await detail('Shop order details');break;
 case 69:await go(staffBase+'/orders/'+extId(5,1));await clickShot('Confirm a pending order',btn('Confirm order'),'Click Confirm order to accept the pending order.');await expect(btn('Start preparing')).toBeVisible();await detail('Confirmed order');break;
 case 70:await go(staffBase+'/orders/'+extId(5,2));await clickShot('Reject a pending order',btn('Reject order'),'Click Reject order for the order you cannot fulfill.');await fillAndShot('Provide a rejection reason',[[page.locator('textarea'),'The selected items are no longer available.','Enter the reason for rejecting the order.']],btn('Confirm rejection'));await detail('Rejected order');break;
 case 71:await go(staffBase+'/orders/'+extId(5,4));await clickShot('Start order preparation',btn('Start preparing'),'Click Start preparing when you begin preparing the order.');await expect(btn('Mark ready for pickup')).toBeVisible();await clickShot('Mark the order ready',btn('Mark ready for pickup'),'Click Mark ready for pickup when preparation is complete.');await detail('Order ready for pickup');break;
 case 72:await go(staffBase+'/orders/'+extId(5,7));const checks=page.getByRole('checkbox');for(const c of await checks.all())await c.check();await shot('Verify pickup and complete the order',[...await Promise.all((await checks.all()).map(async(c,i)=>[c.locator('..'),i===0?'Verify the customer pickup code.':'Confirm that you have handed over the ordered items.'])),[btn('Complete order'),'Click Complete order to finish the pickup.']]);await btn('Complete order').click();await detail('Completed order');break;
 case 73:await go(staffBase+'/orders/'+extId(5,8));const paymentButton=page.getByRole('button',{name:/^(Generate|Show|Regenerate) remaining payment QR$/});await clickShot('Open remaining balance collection',paymentButton,'Generate the remaining payment request or open its QR view.');const showPayment=btn('Show remaining payment QR');if(!await page.getByRole('dialog').last().isVisible()){await expect(showPayment).toBeVisible();await clickShot('Show the payment request',showPayment,'Click Show remaining payment QR to display the payment request.');}await result('Remaining balance payment request',page.getByRole('dialog').last(),'Review the remaining balance and payment instructions. The documentation environment uses a simulated payment request.');break;
 case 74:await go(staffBase+'/info');await shot('Shop and staff information',[[page.getByRole('link',{name:/Shop.*Staff Info|Shop info/i}).first(),'Click Shop & Staff Info in the navigation.'],[page.locator('main section').first(),'Review the shop profile, location, and opening hours.'],[page.locator('main section').last(),'Review your staff account details.']]);break;
 case 75:await go('/account/workspace');await middleClick('Leave a shop',btn('Leave shop'),'Click Leave shop for the shop you want to leave.');const leave=page.getByRole('dialog',{name:'Leave shop',exact:true});await clickShot('Confirm leaving the shop',leave.getByRole('button',{name:'Leave shop',exact:true}),'Confirm that you want to leave the shop.');await expect(leave).toBeHidden();break;
 case 76:await go(staffBase+'/reviews');await listScreen('Customer feedback for the shop','Reviews',page.locator('main article'));break;
 case 77:await go(staffBase+'/reviews');await middleClick('Reply to customer feedback',btn('Reply to customer'),'Click Reply to customer for the feedback you want to answer.');await dialogSubmit('Write your response',d=>[[d.locator('textarea'),'Thank you for your feedback. We look forward to serving you again.','Enter your response to the customer.']],'Send response');break;
 case 78:await go(staffBase+'/reviews');await middleClick('Edit a response',btn('Edit'),'Click Edit for your response.');await dialogSubmit('Update your response',d=>[[d.locator('textarea'),'Thank you for your feedback. We have shared your comments with our kitchen team.','Update your response to the customer.']],'Save changes');break;
 case 79:await go(staffBase+'/reviews');await middleClick('Delete a response',btn('Delete'),'Click Delete for the response you want to remove.');const deletion=page.getByRole('dialog').last();await clickShot('Confirm response deletion',deletion.getByRole('button',{name:'Delete response',exact:true}),'Confirm that you want to delete this response.');await expect(deletion).toBeHidden();break;
 case 80:await go(staffBase+'/restock');await listScreen('Restock tasks','Restock',page.locator('main article'));break;
 case 81:await go(staffBase+'/restock');const pendingTask=page.locator('main article').filter({has:page.getByRole('link',{name:'Order 730001',exact:true})});const taskHref=await pendingTask.getByRole('link',{name:/^Order /}).getAttribute('href');const task=page.locator('main article').filter({has:page.locator('a[href="'+taskHref+'"]')});const quantities=task.getByRole('spinbutton');for(const input of await quantities.all())await input.fill('1');await shot('Choose quantities to return to sale',[[task.locator('input').first(),'Enter the quantity that can be sold again. The remainder is marked unsellable.'],[task.getByRole('button',{name:'Confirm return quantities',exact:true}),'Click Confirm return quantities to save the decision.']]);await task.getByRole('button',{name:'Confirm return quantities',exact:true}).click();await expect(task.getByRole('spinbutton')).toHaveCount(0);break;
 default:await runOwnerOrApplication(n);
}}

let listMode=false;
const field=(name)=>page.getByLabel(name).first();
async function photo(){return {name:'documentation-photo.png',mimeType:'image/png',buffer:await imageFixture()};}
async function mapLoaded(){await expect(page.locator('.leaflet-tile-loaded').first()).toBeVisible({timeout:45000});await page.waitForFunction(()=>[...document.querySelectorAll('.leaflet-tile')].filter(t=>t.getBoundingClientRect().width>0).every(t=>t.complete&&t.naturalWidth>0),{},{timeout:30000});}
async function locationFlow(owner){
 const gps=btn('Use current location');await gps.click();await expect(gps).toBeEnabled({timeout:25000});await expect(field(owner?'Street address':/^Address/)).not.toHaveValue('',{timeout:20000});await mapLoaded();
 const pin=page.getByTitle('Drag to correct your address location',{exact:true});await pin.scrollIntoViewIfNeeded();const b=await pin.boundingBox();await page.mouse.move(b.x+b.width/2,b.y+b.height/2);await page.mouse.down();await page.mouse.move(b.x+b.width/2+35,b.y+b.height/2-18,{steps:15});await page.mouse.up();await expect(gps).toBeEnabled({timeout:25000});await expect(field(owner?'Street address':/^Address/)).not.toHaveValue('',{timeout:20000});await mapLoaded();
 // Keep the provider response, transliterated to English labels for the English guide.
 const labels=owner?['Street address','Ward / Commune','District','City / Province']:[/^Address/,'Ward','District','City'];for(const label of labels){const input=field(label);const value=await input.inputValue();const english=value.normalize('NFD').replace(/[\u0300-\u036f]/g,'').replace(/Đ/g,'D').replace(/đ/g,'d').replace(/^Phuong /,'').replace(/^Quan /,'').replace(/^Thanh pho /,'');if(value!==english)await input.fill(english+(String(label).includes('Ward')?' Ward':String(label).includes('District')?' District':''));}
 const addressFields=owner?field('Street address').locator('xpath=ancestor::div[contains(@class,"ring-1")][1]'):field(/^Address/).locator('..').locator('..');await shot('Locate your shop and review its address',[[gps,'Click Use current location and allow location access in your browser.'],[pin,'Drag the location pin on the map to the exact position of your shop.'],[addressFields,'Review the detected street address, ward, district, and city after adjusting the pin.']]);
}
async function openingHours(){await page.getByRole('combobox',{name:'Opening time: hour',exact:true}).selectOption('08');await page.getByRole('combobox',{name:'Opening time: minute',exact:true}).selectOption('00');await page.getByRole('combobox',{name:'Closing time: hour',exact:true}).selectOption('23');await page.getByRole('combobox',{name:'Closing time: minute',exact:true}).selectOption('00');}
async function applicationForm(editing){
 if(!editing)await page.route('**/maps/styles/**',route=>route.abort());
 if(!editing){
 await field(/Shop name/).fill('Sunrise Community Bakery');await field(/Business license number/).fill('1000000100');await page.getByPlaceholder('Tell customers what your shop sells.').fill('Fresh bread, pastries, and carefully prepared meals for local collection.');
 await shot('Enter shop information',[[field(/Shop name/),'Enter your shop name.'],[field(/Business license number/),'Enter your business license number.'],[page.getByPlaceholder('Tell customers what your shop sells.'),'Describe your shop and the products it offers.']]);
 const uploads=page.locator('input[type="file"]');await uploads.nth(0).setInputFiles(await photo());await uploads.nth(1).setInputFiles(await photo());
 await shot('Choose shop images',[[uploads.nth(0).locator('..'),'Choose a cover image for your shop.'],[uploads.nth(1).locator('..'),'Choose your shop logo.']]);
 await locationFlow(false);
 await openingHours();await shot('Set opening hours',[[page.getByRole('combobox',{name:'Opening time: hour',exact:true}).locator('xpath=ancestor::section[1]'),'Set your opening and closing times.']]);
 await page.getByRole('combobox',{name:'Document type',exact:true}).selectOption('BusinessLicense');await page.locator('input[accept*="application/pdf"]').setInputFiles(await photo());
 await shot('Add supporting documents',[[page.getByRole('combobox',{name:'Document type',exact:true}),'Choose the document type.'],[page.locator('input[accept*="application/pdf"]').locator('..'),'Choose the supporting document to upload.']]);
 await field(/^Client ID/).fill('documentation-client');await field(/^API Key/).fill('documentation-api-key');await field(/^Checksum Key/).fill('documentation-checksum-key');
 }else{
 await page.getByPlaceholder('Tell customers what your shop sells.').fill('Freshly prepared meals, baked goods, and clear collection instructions for customers.');
 await shot('Update the requested shop information',[[field(/Shop name/),'Check and update your shop name.'],[page.getByPlaceholder('Tell customers what your shop sells.'),'Update the information requested by the reviewer.']]);
 }
 await page.getByRole('combobox',{name:'Document type',exact:true}).selectOption('FoodSafetyCertificate');await page.locator('input[accept*="application/pdf"]').setInputFiles(await photo());
 await shot('Attach the food safety certificate',[[page.getByRole('combobox',{name:'Document type',exact:true}),'Select the food safety certificate document type.'],[page.locator('input[accept*="application/pdf"]').locator('..'),'Choose the food safety certificate to attach.']]);
 const paymentMarks=[[field(/^Client ID/),'Enter your payment provider client ID, or keep the saved configuration when editing.'],[field(/^API Key/),'Enter your payment provider API key.'],[field(/^Checksum Key/),'Enter your payment provider checksum key.']];
 if(editing)await shot('Verify payment credentials',[...paymentMarks,[btn('Verify PayOS credentials'),'Click Verify PayOS credentials.']]);
 await btn('Verify PayOS credentials').click();await expect(btn('PayOS verified')).toBeVisible({timeout:30000});
 const action=btn(editing?'Save and resubmit':'Submit application');
 if(editing)await clickShot('Resubmit your application',action,'Click Save and resubmit to send the revised application for review.');
 else {await shot('Verify payment credentials and submit your application',[...paymentMarks,[btn('PayOS verified'),'Click Verify PayOS credentials and wait for successful verification.'],[action,'Click Submit application to send your shop registration for review.']]);await action.click();}
 await page.waitForURL(u=>u.pathname==='/account/shop-application',{waitUntil:'domcontentloaded'});await ready();await expect(page.getByText('Under review',{exact:true}).first()).toBeVisible();
}
async function productPage(tab='info'){
 if(!f.managementProductId){const products=await api('/products?page=1&pageSize=10');f.managementProductId=products.items[1].id;}
 await go('/store-owner/products/'+f.managementProductId+(tab==='info'?'':'?tab='+tab));
}
async function prices(){for(const i of await page.getByRole('spinbutton',{name:/^Original price /}).all())await i.fill('50000');for(const i of await page.getByRole('spinbutton',{name:/^Deal price /}).all())await i.fill('30000');}
async function createProduct(){
 await go('/store-owner/products');await clickShot('Start creating a product',page.getByRole('link',{name:'Add product',exact:true}),'Click Add product.');await page.waitForURL('**/products/new');await ready();
 await field(/Product name/).fill('Garden Vegetable Meal Box');await field(/^Category/).selectOption({label:'Vegetarian'});await field('Description').fill('A freshly prepared vegetable meal with steamed rice, seasonal greens, and a light sauce.');
 await shot('Enter product information',[[field(/Product name/),'Enter your product name.'],[field(/^Category/),'Choose a category.'],[field('Description'),'Enter the product description.']]);
 await page.getByLabel('Add product images',{exact:true}).setInputFiles(await photo());await shot('Choose product images',[[page.getByLabel('Add product images',{exact:true}).locator('..'),'Choose the product images to upload.']]);
 await page.getByRole('button',{name:/^Add attribute/}).click();await page.getByPlaceholder('E.g. Size').fill('Portion');await page.getByPlaceholder('Enter a value, e.g. Small').fill('Regular');await prices();
 await shot('Set attributes and variant prices',[[page.getByPlaceholder('E.g. Size'),'Enter an attribute name.'],[page.getByPlaceholder('Enter a value, e.g. Small'),'Enter the attribute value.'],[page.getByRole('spinbutton',{name:/^Original price /}).first(),'Enter the original price.'],[page.getByRole('spinbutton',{name:/^Deal price /}).first(),'Enter a lower deal price.'],[btn('Create product'),'Click Create product to save the product.']]);
 const response=page.waitForResponse(r=>r.request().method()==='POST'&&new URL(r.url()).pathname==='/api/products');await btn('Create product').click();const r=await response;if(!r.ok())throw Error('Create product '+r.status()+' '+await r.text());f.createdProductId=(await r.json()).data.id;await go('/store-owner/products/'+f.createdProductId);
}
async function runOwnerOrApplication(n){switch(n){
 case 53:await go('/account/shop-application');await clickShot('Start a shop application',btn('Start application'),'Click Start application to register your shop.');await ready();await applicationForm(false);break;
 case 54:await go('/account/shop-application');await shot('Shop registration status',[[nav('Shop Application'),'Click Shop Application in the account navigation.'],[page.locator('[aria-label="Latest shop application"]'),'Review your application status, submitted information, and review progress.']]);break;
 case 55:await go('/account/shop-application');await clickShot('Edit an application that needs revision',btn('Edit and resubmit'),'Review the feedback and click Edit and resubmit.');await ready();await applicationForm(true);break;
 case 56:await go('/account/shop-application');await clickShot('Cancel a pending application',btn('Cancel application'),'Click Cancel application for your pending registration.');const cancel=page.getByRole('dialog',{name:'Cancel application?',exact:true});await clickShot('Confirm application cancellation',cancel.getByRole('button',{name:'Confirm cancellation',exact:true}),'Confirm that you want to cancel the application.');await expect(cancel).toBeHidden();break;
 case 82:await go('/store-owner/profile');await shot('Shop profile',[[page.getByRole('link',{name:'Shop Profile',exact:true}),'Click Shop Profile in the navigation.'],[page.locator('main section').first(),'Review your shop profile, images, address, and operating hours.']]);break;
 case 83:await go('/store-owner/profile');await page.route('**/maps/styles/**',r=>r.abort());await clickShot('Edit business information',btn('Edit business info'),'Click Edit business info.');await field('Description').fill('Freshly prepared meals, friendly service, and carefully packed food ready for local collection.');await shot('Update your business information',[[field(/Shop name/),'Enter your shop name.'],[field('Description'),'Update the description of your shop.']]);await locationFlow(true);await clickShot('Save business information',btn('Save changes'),'Click Save changes to update your business information.');await expect(btn('Edit business info')).toBeVisible();break;
 case 84:await go('/store-owner/profile');await page.route('**/maps/styles/**',r=>r.abort());await clickShot('Edit shop images',btn('Edit business info'),'Click Edit business info to change your shop images.');const images=page.locator('input[type="file"]');await images.nth(0).setInputFiles(await photo());await images.nth(1).setInputFiles(await photo());await mapLoaded();await shot('Choose and save shop images',[[images.nth(0).locator('..'),'Choose a new shop cover image.'],[images.nth(1).locator('..'),'Choose a new shop logo.'],[btn('Save changes'),'Click Save changes to upload and save your shop images.']]);await btn('Save changes').click();await expect(btn('Edit business info')).toBeVisible({timeout:30000});break;
 case 85:await go('/store-owner/products');await listScreen('Products','Products',page.locator('table'));break;
 case 86:await go('/store-owner/products');await middleClick('Open product details',page.locator('a[href="/store-owner/products/00000000-0000-0000-0000-000000000053"]').filter({hasText:'View'}),'Click View for the product you want to inspect.');f.managementProductId=new URL(page.url()).pathname.split('/').at(-1);for(const [tab,title,description]of [['Information','Product information','Review the product name, category, and description.'],['Images','Product and attribute images','Review the product gallery and attribute value images.'],['Variants','Product variants','Review the attribute values, variant prices, and availability.'],['Flash deal history','Product flash deal history','Review past and current flash deals for this product.']]){const t=page.getByRole('tab',{name:tab,exact:true});await t.click();await ready();await shot(title,[[t,'Select the '+tab+' tab.'],[page.getByRole('tabpanel'),description]]);}break;
 case 87:await createProduct();break;
 case 88:await productPage();await fillAndShot('Update product information',[[field(/Product name/),null,'Update your product name.'],[field(/^Category/),null,'Choose the appropriate category.'],[field('Description'),'A freshly prepared meal with quality ingredients, carefully packed for convenient pickup.','Update the product description.']],btn('Save details'));await expect(btn('Save details')).toBeEnabled();break;
 case 89:await go('/store-owner/products/e2000000-0000-0000-0002-000000000001');await clickShot('Delete a product',btn('Delete product'),'Click Delete product for the product you want to remove.');const d=page.getByRole('dialog').last();await clickShot('Confirm product deletion',d.getByRole('button',{name:'Delete product',exact:true}),'Confirm that you want to delete this product.');await expect(d).toBeHidden();break;
 case 90:f.managementProductId='00000000-0000-0000-0000-000000000053';await productPage('images');await page.getByLabel('Add product images',{exact:true}).setInputFiles(await photo());await shot('Update product gallery images',[[page.getByRole('tab',{name:'Images',exact:true}),'Select the Images tab.'],[page.getByLabel('Add product images',{exact:true}).locator('..'),'Choose images to add to the product gallery.'],[btn('Save product images'),'Click Save product images to upload and save your changes.']]);const gallerySaved=page.waitForResponse(r=>r.request().method()==='PUT'&&r.url().endsWith('/images'));await btn('Save product images').click();if(!(await gallerySaved).ok())throw Error('Gallery save failed');await expect(btn('Save product images')).toBeEnabled({timeout:30000});const valueImage=page.getByLabel(/^Choose image for /).first();await valueImage.setInputFiles(await photo());await shot('Update an attribute value image',[[heading('Attribute value images').locator('xpath=ancestor::section[1]'),'Choose an image for the attribute value and review its preview.'],[btn('Save image'),'Click Save image to save the selected attribute image.']]);const valueSaved=page.waitForResponse(r=>r.request().method()==='PUT'&&r.url().includes('/image'));await btn('Save image').click();if(!(await valueSaved).ok())throw Error('Attribute image save failed');await expect(btn('Save image')).toHaveCount(0);break;
 case 91:await productPage('variants');await clickShot('Add attribute values',btn('Add value'),'Select Variants and click Add value.');await page.getByRole('button',{name:/^Add value/}).last().click();await page.getByPlaceholder('Enter a value, e.g. Small').fill('Family portion '+Date.now().toString().slice(-4));await prices();await shot('Create a new attribute value and variant',[[page.getByPlaceholder('Enter a value, e.g. Small'),'Enter the new attribute value.'],[page.getByRole('spinbutton',{name:/^Original price /}).first(),'Enter the original price of the new variant.'],[page.getByRole('spinbutton',{name:/^Deal price /}).first(),'Enter its discounted price.'],[btn('Save new values and variants'),'Click Save new values and variants.']]);const savedValues=page.waitForResponse(r=>r.request().method()==='POST'&&r.url().includes('/attribute-values'));await btn('Save new values and variants').click();if(!(await savedValues).ok())throw Error('Attribute values were not saved');await expect(page.getByRole('spinbutton',{name:/^Original price /})).toHaveCount(0);break;
 case 92:await productPage('variants');await middleClick('Edit a product variant',btn('Edit'),'Click Edit for the variant you want to update.');await dialogSubmit('Update variant prices and status',d=>[[d.getByLabel(/Original price/),'55000','Update the original price.'],[d.getByLabel(/Deal price/),'33000','Update the discounted price.'],[d.getByLabel('Active for sale'),null,'Choose whether this variant is active for sale.']],'Save variant');break;
 case 93:await productPage();await clickShot('Change product visibility',btn('Hide product'),'Click Hide product to stop offering this product for sale.');await expect(btn('Activate product')).toBeVisible();await result('Updated product status',btn('Activate product'),'Check that the product is hidden and can be activated again.');await btn('Activate product').click();await expect(btn('Hide product')).toBeVisible();break;
 case 94:await go('/store-owner/products');const historyProduct=page.locator('tbody tr').filter({hasText:'Roasted Sweet Potato'});await historyProduct.getByRole('link',{name:/^View /}).click();await ready();await page.getByRole('tab',{name:'Flash deal history',exact:true}).click();await ready();await listScreen('Product flash deal history','Products',page.getByRole('tabpanel'),[[page.getByRole('tab',{name:'Flash deal history',exact:true}),'Select the Flash deal history tab.']]);break;
 case 95:await createFlashDeal();break;
 case 96:if(!f.newFlashDealId)throw Error('Complete UC-095 first.');await go('/store-owner/flash-deals/'+f.newFlashDealId+'/edit');const stock=page.getByPlaceholder('50',{exact:true}).first();await stock.fill('35');await shot('Update flash deal stock',[[page.getByPlaceholder('25000').first(),'Review or update the selected variant deal price.'],[stock,'Update the quantity offered in this flash deal.'],[page.getByRole('button',{name:/^(Active|Hidden)$/}).last(),'Choose whether the selected variant is active or hidden.'],[btn('Save Flash Deal'),'Click Save Flash Deal to save the updated configuration.']]);await btn('Save Flash Deal').click();await page.waitForURL(u=>u.pathname==='/store-owner/flash-deals',{waitUntil:'domcontentloaded'});break;
 case 97:if(!f.newFlashDealId)throw Error('Complete UC-095 first.');await go('/store-owner/flash-deals');const row=page.locator('tbody tr').filter({hasText:f.newFlashDealProductName});await clickShot('Cancel a flash deal',row.getByTitle('Cancel deal'),'Click Cancel deal for the flash deal you want to end.');const cancelled=page.waitForResponse(r=>r.request().method()==='POST'&&r.url().includes('/cancel'));await clickShot('Confirm flash deal cancellation',btn('Yes, Cancel Deal'),'Confirm that you want to cancel the selected flash deal.');if(!(await cancelled).ok())throw Error('Flash deal cancellation failed');await ready();break;
 case 98:case 99:await toggleDeal(n===98);break;
 case 100:await go('/store-owner/staff');await shot('Open sent staff invitations',[[page.locator('aside a').filter({hasText:'Staff Management'}).first(),'Click Staff Management in the navigation to open the staff list.'],[btn('View Invitations'),'Click View Invitations to open the sent invitation list.']]);await btn('View Invitations').click();await page.waitForURL(u=>u.pathname==='/store-owner/staff/invitations',{waitUntil:'domcontentloaded'});await ready();await expect(btn('Revoke').first()).toBeVisible({timeout:30000});listMode=true;await shot('Sent staff invitations',[[page.locator('table'),'Review the sent invitations, their status, sent dates, and available actions.']]);listMode=false;break;
 default:throw Error('Missing capture recipe '+n);
}}
async function createFlashDeal(){
 await go('/store-owner/flash-deals');await clickShot('Create a flash deal',btn('Create Flash Deal'),'Click Create Flash Deal.');await ready();
 await page.getByRole('button',{name:'Select a product to put on flash deal...',exact:true}).click();const candidates=await api('/flash-deals/eligible-products');const selected=candidates.find(p=>!p.hasActiveDeal&&p.variants.length>0);if(!selected)throw Error('No eligible product with variants');f.newFlashDealProductName=selected.name;const option=page.getByText(selected.name,{exact:true}).last();await option.click();
 await shot('Select the flash deal product',[[heading('Product Information').locator('..'),'Choose the product to offer in this flash deal.']]);
 await btn('60m').click();const scheduleMarks=[[page.getByRole('button',{name:/Sell Immediately/}).locator('..'),'Choose one selling mode: sell immediately or schedule a later start.'],[btn('60m').locator('..'),'Choose how long customers can order the deal.'],[page.getByRole('radio',{name:'Same as deal end',exact:true}).locator('xpath=ancestor::fieldset[1]'),'Choose one pickup option: use the deal end time or select a later pickup time.']];
 const stock=page.getByPlaceholder('50',{exact:true}).first();await stock.fill('30');await shot('Set the schedule, variants, prices, and quantities',[...scheduleMarks,[page.getByRole('checkbox').first(),'Select the variants to include in the flash deal.'],[page.getByPlaceholder('25000').first(),'Set the deal price for the selected variant.'],[stock,'Enter the quantity available for sale.'],[btn('Save Flash Deal'),'Click Save Flash Deal to create the deal.']]);
 const response=page.waitForResponse(r=>r.request().method()==='POST'&&new URL(r.url()).pathname==='/api/flash-deals');await btn('Save Flash Deal').click();const r=await response;if(!r.ok())throw Error('Create flash deal '+r.status()+' '+await r.text());f.newFlashDealId=(await r.json()).data.id;await page.waitForURL(u=>u.pathname==='/store-owner/flash-deals',{waitUntil:'domcontentloaded'});
}
async function toggleDeal(unhide){
 const target='00000000-0000-0000-0000-000000000060';
 await go('/store-owner/flash-deals');
 // Select a middle row and prepare the opposite state through the real API.
 if(!f.newFlashDealId)throw Error('Complete UC-095 first.');const rows=page.locator('tbody tr');let row=rows.filter({hasText:f.newFlashDealProductName}).first();
 if(unhide&&!await row.getByRole('button',{name:'Unhide Deal',exact:true}).isVisible()){
  await row.getByRole('button',{name:'Hide',exact:true}).click();await btn('Yes, Hide Deal').click();await ready();
 }
 await clickShot(unhide?'Unhide a flash deal':'Hide a flash deal',row.getByRole('button',{name:unhide?'Unhide Deal':'Hide',exact:true}),unhide?'Click Unhide Deal to make the deal visible to customers.':'Click Hide to temporarily hide the deal from customers.');
 const visibility=page.waitForResponse(r=>['POST','DELETE'].includes(r.request().method())&&r.url().includes('/hide')).catch(()=>null);await clickShot('Confirm flash deal visibility',btn(unhide?'Yes, Unhide Deal':'Yes, Hide Deal'),unhide?'Confirm that you want to unhide this deal.':'Confirm that you want to hide this deal.');const visibilityResponse=await visibility;if(!visibilityResponse?.ok())throw Error('Visibility update failed '+(visibilityResponse?await visibilityResponse.text():'no response'));await ready();
}

async function imageFixture(){return Buffer.from(await page.evaluate(()=>{const c=document.createElement('canvas');c.width=c.height=320;const x=c.getContext('2d');x.fillStyle='#d1fae5';x.fillRect(0,0,320,320);x.fillStyle='#065f46';x.font='bold 30px sans-serif';x.textAlign='center';x.fillText('Demo photo',160,170);return c.toDataURL().split(',')[1];}),'base64');}
async function addressForm(label){for(const [name,value]of Object.entries({label,addressLine:'45 Nguyen Van Cu Extension',ward:'An Binh Ward',district:'Ninh Kieu District',city:'Can Tho',latitude:'10.0131',longitude:'105.7319'}))await page.locator(`input[name="${name}"]`).fill(value);}
const selected=process.argv.find(s=>s.startsWith('--only='))?.split('=')[1].split(',').map(Number);
const order=selected??Array.from({length:50},(_,i)=>i+51);
try{
 for(const n of order){
  if(!selected&&report.useCases.some(u=>u.id===id(n)&&u.status==='passed'))continue;
  context=await browser.newContext({ignoreHTTPSErrors:true,viewport:report.viewport,reducedMotion:'reduce',geolocation:{latitude:10.0131,longitude:105.7319,accuracy:10},permissions:['geolocation']});page=await context.newPage();page.setDefaultTimeout(15000);
  // Use the provider's own international brand names in the English document.
  // All bank IDs and selectable records come from the original directory.
  await page.route('https://api.vietqr.io/v2/banks', async route=>{const response=await route.fetch();const directory=await response.json();directory.data=directory.data.map(bank=>({...bank,name:bank.shortName||bank.code||bank.name}));await route.fulfill({response,json:directory});});
  listMode=false;current={id:id(n),title:titles[n-1],actor:n>=82?'Store Owner':n>=64&&n<=81?'Staff':'Customer',precondition:n<=5?'The authentication page is available.':'You are signed in with the required role and the demo records for this flow are available.',status:'running',frames:[],checks:[]};
  report.useCases=report.useCases.filter(u=>u.id!==current.id);report.useCases.push(current);await mkdir(new URL(current.id+'/',root),{recursive:true});page.on('response',r=>{if(r.url().startsWith(baseURL+'/api/') && !['GET','OPTIONS'].includes(r.request().method())) current.checks.push({method:r.request().method(),path:new URL(r.url()).pathname,status:r.status()});});
  try{await login(n>=82?'owner@gmail.com':n>=64&&n<=81&&![75,80].includes(n)?'customer.locked@gmail.com':[75,80].includes(n)?'customer@gmail.com':n===53?'documentation.location3@savebite.test':n===54?'documentation.applicant5@savebite.test':n===55?'documentation.applicant2@savebite.test':n===56?'documentation.applicant3@savebite.test':'customer@gmail.com');

  await run(n);const failed=current.checks.filter(c=>c.status>=400);if(failed.length)throw Error('A mutation failed: '+JSON.stringify(failed));current.status='passed';}catch(error){current.status='failed';current.error=error.message;await page.screenshot({path:new URL(current.id+'/debug.png',root).pathname}).catch(()=>{});console.log(current.id+' FAILED: '+error.message.slice(0,300));}
  await save();await page.unrouteAll({behavior:"ignoreErrors"});await context.close();
 }
}finally{await save();await browser.close();}
console.log(JSON.stringify(report.useCases.map(u=>({id:u.id,status:u.status,frames:u.frames.length}))));
