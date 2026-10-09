import test from 'node:test';import assert from 'node:assert/strict';import {readFile} from 'node:fs/promises';import {execFileSync} from 'node:child_process';import {JSDOM} from 'jsdom';
test('native brand preparation preserves shared source and separates install identities and deep links',async()=>{
 const identities=[];
 const shared=await readFile('src/BigDaySorted.App/MainPage.cs','utf8');
 for(const brand of ['BigDaySorted','LiveDaysSorted']){
  execFileSync(process.execPath,['scripts/prepare-native-brand.mjs',brand]);const root=`.native-build/${brand}/src/BigDaySorted.App`;
  assert.equal(await readFile(`${root}/MainPage.cs`,'utf8'),shared);assert.equal(await readFile(`.native-build/${brand}/web/graph-interaction.mjs`,'utf8'),await readFile('web/graph-interaction.mjs','utf8'));
  for(const p of ['BigDaySorted.App.csproj','Platforms/Android/AndroidManifest.xml','Platforms/Windows/Package.appxmanifest','Platforms/iOS/Info.plist','Platforms/MacCatalyst/Info.plist','Resources/AppIcon/appicon.svg']){
   const xml=new JSDOM(await readFile(`${root}/${p}`,'utf8'),{contentType:'text/xml'});assert.equal(xml.window.document.querySelector('parsererror'),null,p);xml.window.close();
  }
  const manifest=await readFile(`${root}/Platforms/Windows/Package.appxmanifest`,'utf8');assert.ok(manifest.includes(`Name="${brand}"`));identities.push(manifest.match(/PhoneProductId="([^"]+)"/)[1]);
  const plist=await readFile(`${root}/Platforms/iOS/Info.plist`,'utf8');assert.ok(plist.includes(`<string>${brand.toLowerCase()}</string>`));assert.ok(plist.includes('com.keepmytravels.bigdaysorted.event'));
  assert.equal(await readFile(`${root}/Resources/AppIcon/appicon.svg`,'utf8'),await readFile(`brands/${brand==='LiveDaysSorted'?'live':'celebrations'}/logo.svg`,'utf8'));
 }
 assert.notEqual(identities[0],identities[1]);assert.equal(await readFile('src/BigDaySorted.App/MainPage.cs','utf8'),shared);
});
