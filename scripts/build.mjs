import {mkdir,readFile,writeFile,cp,rm} from 'node:fs/promises';
import {brands,livePlans,renderLivePricing} from '../brands/config.mjs';
const args=Object.fromEntries(process.argv.slice(2).map(x=>x.replace(/^--/,'').split('=')));
const selected=args.brand||JSON.parse(await readFile('.openai/brand.json','utf8').catch(()=>'{"brand":"celebrations"}')).brand;
const brand=brands[selected];if(!brand)throw Error('Unknown brand.');const output=args.output||'dist';if(!['dist','dist-live'].includes(output))throw Error('Unsupported output folder.');
await rm(output,{recursive:true,force:true});await mkdir(output+'/server',{recursive:true});
const assets={};for(const name of ['index.html','app.html','style.css','app.mjs','favicon.svg','logo.svg','planning.mjs','graph-interaction.mjs']){
 let value=await readFile('web/'+name,'utf8');
 if(name==='index.html'){value=await readFile('brands/'+selected+'/index.html','utf8');if(selected==='live')value=renderLivePricing(value);}
 if(name==='logo.svg'||name==='favicon.svg')value=await readFile('brands/'+selected+'/logo.svg','utf8');
 if(name==='app.html')value=value.replaceAll('BigDaySorted',brand.name).replaceAll('BigDay<em>Sorted.</em>',selected==='live'?'LiveDays<em>Sorted.</em>':'BigDay<em>Sorted.</em>').replaceAll('Big day? Sorted.',brand.tagline);
 if(name==='style.css')value+='\n'+await readFile('brands/'+selected+'/theme.css','utf8');
 assets['/'+name]=value;
}
let source=await readFile('worker/api.mjs','utf8');source=source.replace(/^import .*?;\n/gm,'');
const domain=(await readFile('worker/domain.mjs','utf8')).replaceAll('export ','');
const branding=(await readFile('brands/config.mjs','utf8')).replaceAll('export ','');
const handler=`\nconst siteBrand=${JSON.stringify(brand)};const assets=${JSON.stringify(assets)};\nexport default {async fetch(request,env){const p=new URL(request.url).pathname;if(p.startsWith('/api/'))return api(request,{...env,BRAND:siteBrand.key});const name=p==='/'?'/index.html':p;const data=assets[name];if(!data)return new Response('Not found',{status:404});const type=name.endsWith('.css')?'text/css':name.endsWith('.mjs')?'text/javascript':name.endsWith('.svg')?'image/svg+xml':'text/html';return new Response(data,{headers:{'content-type':type+'; charset=utf-8','x-content-type-options':'nosniff','referrer-policy':'no-referrer','content-security-policy':"default-src 'self'; img-src 'self' blob: data:; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'self'"}});}};`;
await writeFile(output+'/server/index.js',domain+'\n'+branding+'\n'+source.replace('export async function api','async function api')+handler);
console.log('Built '+brand.name+' website and shared organiser.');
