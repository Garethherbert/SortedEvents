import {readFile} from 'node:fs/promises';
import {spawnSync} from 'node:child_process';
export const products=['BigDaySorted','LiveDaysSorted'];
export const brandKey=p=>p==='BigDaySorted'?'celebrations':'live';
export async function settings(){return JSON.parse(await readFile('cloudflare/deploy.config.json','utf8').catch(()=>{throw Error('Run scripts/Setup-Cloudflare.ps1 first.')}));}
export function checkSettings(c){
 if(!/^[a-f0-9]{32}$/i.test(c.accountId||''))throw Error('Enter a valid Cloudflare account ID.');
 if(!/^https:\/\/[a-z0-9-]+\.cloudflareaccess\.com$/.test(c.accessTeamDomain||''))throw Error('Enter https://YOUR-TEAM.cloudflareaccess.com.');
 const all=[];for(const p of products){const b=c.products?.[p];if(!b||!Array.isArray(b.domains)||!b.domains.length||!b.accessAud||!b.databaseName||!b.bucketName)throw Error('Missing settings for '+p);
  for(const domain of b.domains){if(!/^(?=.{1,253}$)([a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z]{2,63}$/.test(domain))throw Error('Use a hostname without https:// or a path: '+domain);all.push(domain);}
  if(!/^[a-z0-9-]{3,63}$/.test(b.bucketName)||!/^[a-z0-9-]{3,63}$/.test(b.databaseName))throw Error('Use lowercase resource names.');
 }if(new Set(all).size!==all.length)throw Error('Each hostname can belong to only one brand.');return c;
}
export function command(executable,args){const r=spawnSync(executable,args,{stdio:'inherit',shell:process.platform==='win32'&&['npm','npx'].includes(executable)});if(r.error)throw r.error;if(r.status!==0)throw Error(executable+' failed with exit '+r.status);}
export const wrangler=args=>command('npx',['--no-install','wrangler',...args]);
