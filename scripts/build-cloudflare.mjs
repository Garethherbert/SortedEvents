import {mkdir,readFile,writeFile} from 'node:fs/promises';
import {settings,checkSettings,products,brandKey,command} from './cloudflare-common.mjs';
const c=checkSettings(await settings());await mkdir('cloudflare/generated',{recursive:true});
for(const p of products){const b=c.products[p],key=brandKey(p),output=key==='live'?'dist-live':'dist';
 if(!b.databaseId)throw Error('Provision Cloudflare resources first.');
 command('node',['scripts/build.mjs','--brand='+key,'--output='+output]);
 const big='https://'+c.products.BigDaySorted.domains[0],live='https://'+c.products.LiveDaysSorted.domains[0];let worker=await readFile(output+'/server/index.js','utf8');worker=worker.replaceAll('https://bigdaysorted.garethdherbert.chatgpt.site',big).replaceAll('https://livedayssorted.garethdherbert.chatgpt.site',live);
 // Guest links remain on app.html; organiser entry points go through Access.
 worker=worker.replaceAll('href=\\"/app.html\\"','href=\\"/organiser-login\\"');await writeFile(output+'/server/index.js',worker);
 const slug=p.toLowerCase();await writeFile('cloudflare/generated/'+slug+'.mjs',`import worker from '../../${output}/server/index.js';\nimport {dispatch} from '../access-auth.mjs';\nexport default {fetch(request,env,ctx){return dispatch(request,env,worker,ctx);}};\n`);
 const config={name:slug,main:'generated/'+slug+'.mjs',account_id:c.accountId,compatibility_date:'2026-10-09',workers_dev:false,preview_urls:false,routes:b.domains.map(pattern=>({pattern,custom_domain:true})),vars:{ACCESS_TEAM_DOMAIN:c.accessTeamDomain,ACCESS_AUD:b.accessAud,PRODUCT_BRAND:p},d1_databases:[{binding:'DB',database_name:b.databaseName,database_id:b.databaseId,migrations_dir:'../drizzle'}],r2_buckets:[{binding:'MEDIA',bucket_name:b.bucketName}]};await writeFile('cloudflare/wrangler-'+slug+'.json',JSON.stringify(config,null,2));
 console.log('Cloudflare build ready: '+p+' on '+b.domains.join(', '));
}
