import {writeFile} from 'node:fs/promises';
import {settings,checkSettings,products} from './cloudflare-common.mjs';
const c=checkSettings(await settings()),token=process.env.CLOUDFLARE_API_TOKEN;
if(!token)throw Error('Set CLOUDFLARE_API_TOKEN with D1 and R2 edit permissions.');
async function call(path,method='GET',body){const r=await fetch('https://api.cloudflare.com/client/v4/accounts/'+c.accountId+path,{method,headers:{authorization:'Bearer '+token,'content-type':'application/json'},...(body?{body:JSON.stringify(body)}:{}),signal:AbortSignal.timeout(30000)});const data=await r.json();if(!r.ok||!data.success)throw Error('Cloudflare '+path+': '+(data.errors||[]).map(x=>x.message).join('; '));return data;}
const databases=[];for(let page=1;;page++){const data=await call('/d1/database?per_page=100&page='+page);databases.push(...data.result);if(data.result.length<100)break;}
const buckets=[];let cursor='';do{const data=await call('/r2/buckets?per_page=1000'+(cursor?'&cursor='+encodeURIComponent(cursor):''));buckets.push(...(data.result.buckets||[]));cursor=data.result_info?.cursor||'';}while(cursor);
for(const p of products){const b=c.products[p];let db=databases.find(d=>d.name===b.databaseName);if(!db){db=(await call('/d1/database','POST',{name:b.databaseName})).result;databases.push(db);}if(!db.uuid)throw Error('Cloudflare did not return a database ID.');b.databaseId=db.uuid;
 // Checkpoint after each resource, so a retry reuses what was created.
 await writeFile('cloudflare/deploy.config.json',JSON.stringify(c,null,2));
 if(!buckets.some(x=>x.name===b.bucketName)){const bucket=(await call('/r2/buckets','POST',{name:b.bucketName})).result;buckets.push(bucket);}
 console.log(p+': database and private media bucket ready.');
}
