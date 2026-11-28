const { chromium, firefox } = require('playwright');
const fs = require('node:fs/promises');
(async () => {
  await fs.mkdir('browser-report', { recursive: true });
  const results = [];
  for (const [name, type] of Object.entries({ chromium, firefox })) {
    let browser;
    const errors = [];
    try {
      browser = await type.launch({ headless: true,
        ...(name === 'chromium' ? {args:['--use-angle=swiftshader','--enable-unsafe-swiftshader','--disable-dev-shm-usage']} : {firefoxUserPrefs:{'webgl.force-enabled':true}}) });
      const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
      let transferred = 0;
      page.on('response', async response => { transferred += Number(response.headers()['content-length'] || 0); });
      page.on('pageerror', error => errors.push(error.message));
      await page.goto('http://127.0.0.1:8765', { waitUntil:'domcontentloaded' });
      await page.waitForFunction(() => {
        const bar = document.querySelector('#unity-loading-bar');
        const canvas = document.querySelector('#unity-canvas');
        return canvas && canvas.width > 0 && bar && getComputedStyle(bar).display === 'none';
      }, null, { timeout: 240000 });
      const canvas = page.locator('#unity-canvas');
      await canvas.screenshot({ path: 'browser-report/' + name + '-menu.png' });
      await canvas.click({ position: { x:640, y:672 } });
      await page.waitForTimeout(300);
      await canvas.screenshot({ path: 'browser-report/' + name + '-credits.png' });
      await canvas.click({ position: { x:640, y:585 } });
      await canvas.click({ position: { x:640, y:303 } });
      await page.keyboard.press('Space');
      await page.waitForTimeout(500);
      await page.keyboard.press('Escape');
      await canvas.screenshot({ path:'browser-report/' + name + '-pause.png' });
      await page.keyboard.press('Escape');
      await page.waitForTimeout(1200);
      if (errors.length) throw new Error(errors.join('\n'));
      results.push({browser:name,status:'PASS',scope:'Actual WebGL startup, canvas captures and input smoke only; visual/state/audio acceptance is not asserted.',transferred_content_length_bytes:transferred});
    } catch (error) {
      results.push({browser:name,status:'FAIL',error:String(error)});
    } finally { if(browser) await browser.close(); }
  }
  await fs.writeFile('browser-report/results.json',JSON.stringify(results,null,2));
  if(results.some(r=>r.status!=='PASS')) process.exitCode=1;
})().catch(error=>{console.error(error);process.exitCode=1;});
