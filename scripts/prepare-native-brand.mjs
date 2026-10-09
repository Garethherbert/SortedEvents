import {cp,mkdir,readFile,writeFile,rm} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const brand=process.argv[2]||'BigDaySorted';
if(!['BigDaySorted','LiveDaysSorted'].includes(brand))throw Error('Choose BigDaySorted or LiveDaysSorted.');
const destination=path.join(root,'.native-build',brand);
await rm(destination,{recursive:true,force:true});await mkdir(destination,{recursive:true});
await cp(path.join(root,'src'),path.join(destination,'src'),{recursive:true,filter:p=>!['bin','obj'].includes(path.basename(p))});
await cp(path.join(root,'Directory.Build.props'),path.join(destination,'Directory.Build.props'));
await mkdir(path.join(destination,'web'),{recursive:true});await cp(path.join(root,'web','graph-interaction.mjs'),path.join(destination,'web','graph-interaction.mjs'));
const app=path.join(destination,'src','BigDaySorted.App');
const config=JSON.parse(await readFile(path.join(root,'cloudflare/deploy.config.json'),'utf8').catch(()=>'{\"products\":{}}'));
const coreBrand=path.join(destination,'src/BigDaySorted.Core/ProductBrand.cs');let brandText=await readFile(coreBrand,'utf8');for(const name of ['BigDaySorted','LiveDaysSorted']){const domain=config.products?.[name]?.domains?.[0];if(domain){if(!/^[a-z0-9.-]+$/.test(domain))throw Error('Invalid native website hostname.');brandText=brandText.replaceAll('https://'+name.toLowerCase()+'.garethdherbert.chatgpt.site','https://'+domain)}}await writeFile(coreBrand,brandText);

const live=brand==='LiveDaysSorted';
for(const relative of ['Platforms/Android/AndroidManifest.xml','Platforms/Windows/Package.appxmanifest','Platforms/Windows/app.manifest','Platforms/iOS/Info.plist','Platforms/MacCatalyst/Info.plist']){
 const filename=path.join(app,relative);let text=await readFile(filename,'utf8');
 text=text.replaceAll('0.1.1.2','0.1.4.5');
 if(live){text=text.replaceAll('BigDaySorted','LiveDaysSorted');
 // The .bigday document format remains a shared identifier; only URL schemes differ.
 text=text.replace('<string>bigdaysorted</string>','<string>livedayssorted</string>');
 text=text.replaceAll('1bb65eef-f68f-4d97-8d81-1b23d7a10d21','ab331886-1a70-4293-83a6-28365a3e55ab');
 if(relative.endsWith('Info.plist'))text=text.replace('<key>UTExportedTypeDeclarations</key>','<key>UTImportedTypeDeclarations</key>').replace('<string>Owner</string>','<string>Alternate</string>');
 }
 await writeFile(filename,text);
}
const logo=await readFile(path.join(root,'brands',live?'live':'celebrations','logo.svg'),'utf8');
for(const relative of ['Resources/Images/brandmark.svg','Resources/AppIcon/appicon.svg','Resources/Splash/splash.svg'])await writeFile(path.join(app,relative),logo);
if(live){const csproj=path.join(app,'BigDaySorted.App.csproj');let text=await readFile(csproj,'utf8');text=text.replace('<PropertyGroup>','<PropertyGroup><NativeBrandPrepared>true</NativeBrandPrepared>').replace('Color="#12323e"','Color="#201938"').replace('<AppBrand Condition="\'$(AppBrand)\'==\'\'">BigDaySorted</AppBrand>','<AppBrand Condition="\'$(AppBrand)\'==\'\'">LiveDaysSorted</AppBrand>');await writeFile(csproj,text);}
await writeFile(path.join(destination,'brand.json'),JSON.stringify({brand,applicationId:`com.keepmytravels.${brand.toLowerCase()}`,archiveFormat:'BigDaySorted',archiveVersion:1},null,2));
console.log(destination);
