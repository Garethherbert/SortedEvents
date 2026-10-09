import {readFile,writeFile} from 'node:fs/promises';
import {checkSettings} from './cloudflare-common.mjs';
// Non-secret configuration is supplied over stdin, never API-token arguments.
let input='';for await(const chunk of process.stdin)input+=chunk;
const c=checkSettings(JSON.parse(input));
let previous={};try{previous=JSON.parse(await readFile('cloudflare/deploy.config.json','utf8'))}catch{}
for(const [p,b]of Object.entries(c.products)){const old=previous.products?.[p];if(previous.accountId===c.accountId&&old?.databaseName===b.databaseName)b.databaseId=old.databaseId;}
await writeFile('cloudflare/deploy.config.json',JSON.stringify(c,null,2));
console.log('Saved domain and Cloudflare settings. No secrets were written.');
