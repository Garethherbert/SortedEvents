import {settings,checkSettings,products,wrangler} from './cloudflare-common.mjs';
const c=checkSettings(await settings());const dry=process.argv.includes('--dry-run');
for(const p of products){const path='cloudflare/wrangler-'+p.toLowerCase()+'.json';
 if(!dry)wrangler(['d1','migrations','apply',c.products[p].databaseName,'--remote','--config',path]);
 wrangler(['deploy','--config',path,...(dry?['--dry-run']:[])]);
}
console.log(dry?'Both Worker bundles passed dry-run packaging.':'Both websites and their /api endpoints deployed.');
