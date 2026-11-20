// Shared capture overlay: measured from live elements, never hard-coded coordinates.
export async function captureAnnotated(page, path, marks) {
  await page.evaluate(async () => {
    await document.fonts.ready;
    await Promise.all(document.getAnimations().filter(a => a.effect?.getTiming().iterations !== Infinity).map(a => a.finished.catch(() => {})));
  });
  for (const mark of marks) await mark.target.waitFor({ state: 'visible' });
  if (marks.length === 1) await marks[0].target.scrollIntoViewIfNeeded();
  const targets = [];
  for (const mark of marks) {
    let rect = await mark.target.evaluate(async el => {
      const read = () => { const r = el.getBoundingClientRect(); return { x: r.x, y: r.y, width: r.width, height: r.height }; };
      let prev = read();
      for (let i = 0; i < 120; i++) {
        await new Promise(requestAnimationFrame);
        const next = read();
        if (Object.keys(prev).every(k => Math.abs(prev[k] - next[k]) < 0.25)) return next;
        prev = next;
      }
      throw new Error('Annotation target layout did not stabilize');
    });
    const vp = page.viewportSize();
    if(mark.clip) {
      const right=Math.min(rect.x+rect.width,vp.width-8), bottom=Math.min(rect.y+rect.height,vp.height-8);
      rect={x:Math.max(8,rect.x),y:Math.max(8,rect.y),width:right-Math.max(8,rect.x),height:bottom-Math.max(8,rect.y)};
    }
    if (rect.width < 1 || rect.height < 1 || rect.x < 5 || rect.y < 5 || rect.x + rect.width > vp.width - 5 || rect.y + rect.height > vp.height - 5)
      throw new Error(`Annotation ${mark.number} is outside the screenshot: ${JSON.stringify(rect)}`);
    targets.push({ number: mark.number, description: mark.description, rect });
  }
  const placements = await page.evaluate(targets => {
    document.querySelector('[data-capture-overlay]')?.remove();
    const ns = 'http://www.w3.org/2000/svg';
    const svg = document.createElementNS(ns, 'svg');
    svg.setAttribute('data-capture-overlay', '');
    svg.setAttribute('aria-hidden', 'true');
    Object.assign(svg.style, { position: 'fixed', inset: '0', width: '100vw', height: '100vh', pointerEvents: 'none', zIndex: '2147483647', overflow: 'visible' });
    const add = (tag, attrs, text) => {
      const el = document.createElementNS(ns, tag);
      for (const [k, v] of Object.entries(attrs)) el.setAttribute(k, String(v));
      if (text) el.textContent = text;
      svg.append(el);
    };
    const obstacles = [];
    // Background controls are inert and covered by a native modal. Only its
    // contents can obstruct a badge while it is open.
    const visibleRoot = document.querySelector('dialog[open]') ?? document.querySelector('div.fixed.inset-0.z-50') ?? document.body;
    const walker = document.createTreeWalker(visibleRoot, NodeFilter.SHOW_TEXT);
    while (walker.nextNode()) {
      const node = walker.currentNode;
      if (!node.textContent.trim() || node.parentElement.closest('script,style,svg')) continue;
      const range = document.createRange();
      range.selectNodeContents(node);
      for (const rect of range.getClientRects()) if (rect.width && rect.height) obstacles.push(rect);
    }
    for (const el of visibleRoot.querySelectorAll('button,input,textarea,a,select')) {
      const rect = el.getBoundingClientRect();
      if (rect.width && rect.height) obstacles.push(rect);
    }
    const placed = [];
    const intersects = (a, b) => a.x < b.x + b.width && a.x + a.width > b.x && a.y < b.y + b.height && a.y + a.height > b.y;
    for (const { number, rect: r } of targets) {
      add('rect', { x: r.x - 4, y: r.y - 4, width: r.width + 8, height: r.height + 8, rx: 6, fill: 'none', stroke: '#dc2626', 'stroke-width': 3 });
      // Choose free space around the control; avoid visible text and nearby
      // controls, including Forgot password above the Sign in button.
      const candidates = [
        { x: r.x + r.width - 12, y: r.y - 23 },
        { x: r.x + r.width + 23, y: r.y + Math.min(r.height / 2, 23) },
        { x: r.x - 23, y: r.y + Math.min(r.height / 2, 23) },
        { x: r.x + r.width - 12, y: r.y + r.height + 23 },
        { x: r.x + 20, y: r.y - 23 },
        { x: r.x + r.width / 2, y: r.y - 23 },
        { x: r.x + r.width / 2, y: r.y + r.height + 23 },
      ];
      const spot = candidates.find(p => {
        const box = { x: p.x - 18, y: p.y - 18, width: 36, height: 36 };
        return box.x >= 2 && box.y >= 2 && box.x + 36 <= innerWidth - 2 && box.y + 36 <= innerHeight - 2
          && !obstacles.some(o => intersects(box, o)) && !placed.some(o => intersects(box, o.box));
      });
      if (!spot) throw new Error(`No unobstructed position for annotation ${number}`);
      const { x: cx, y: cy } = spot;
      placed.push({ number, x: cx, y: cy, box: { x: cx - 18, y: cy - 18, width: 36, height: 36 } });
      const nearestX = Math.max(r.x - 4, Math.min(cx, r.x + r.width + 4));
      const nearestY = Math.max(r.y - 4, Math.min(cy, r.y + r.height + 4));
      add('line', { x1: cx, y1: cy, x2: nearestX, y2: nearestY, stroke: '#dc2626', 'stroke-width': 2 });
      add('circle', { cx, cy, r: 16, fill: '#b91c1c', stroke: 'white', 'stroke-width': 2 });
      add('text', { x: cx, y: cy + 1, fill: 'white', 'font-size': 17, 'font-weight': 700, 'font-family': 'Arial, sans-serif', 'text-anchor': 'middle', 'dominant-baseline': 'central' }, String(number));
    }
    // Native modal dialogs live in the browser top layer. Keep the overlay
    // inside the open dialog so its backdrop cannot cover the annotations.
    (document.querySelector('dialog[open]') ?? document.body).append(svg);
    return placed;
  }, targets);
  try {
    await page.screenshot({ path, fullPage: false, animations: 'disabled', caret: 'hide' });
  } finally {
    await page.evaluate(() => document.querySelector('[data-capture-overlay]')?.remove());
  }
  return targets.map((target, i) => ({ ...target, badge: { x: placements[i].x, y: placements[i].y } }));
}
