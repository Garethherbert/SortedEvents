import test from 'node:test';
import assert from 'node:assert/strict';
import {accessIdentity,dispatch} from '../cloudflare/access-auth.mjs';
import {database,bucket} from '../scripts/local-db.mjs';
import {api} from '../worker/api.mjs';
const issuer='https://unit-test.cloudflareaccess.com',aud='test-audience';
const keys=await crypto.subtle.generateKey({name:'RSASSA-PKCS1-v1_5',modulusLength:2048,publicExponent:new Uint8Array([1,0,1]),hash:'SHA-256'},true,['sign','verify']);
const jwk={...await crypto.subtle.exportKey('jwk',keys.publicKey),kid:'test-key'};
const enc=value=>Buffer.from(JSON.stringify(value)).toString('base64url');
async function token(p={}){const input=enc({alg:'RS256',kid:jwk.kid})+'.'+enc({iss:issuer,aud:[aud],sub:'organiser-1',email:'organiser@example.invalid',exp:Date.now()/1000+120,...p});return input+'.'+Buffer.from(await crypto.subtle.sign('RSASSA-PKCS1-v1_5',keys.privateKey,new TextEncoder().encode(input))).toString('base64url');}
test('Cloudflare owner identity requires signed, current, correctly scoped JWT; guest tokens stay independent',async()=>{
 const savedFetch=globalThis.fetch;globalThis.fetch=async url=>{assert.equal(String(url),issuer+'/cdn-cgi/access/certs');return Response.json({keys:[jwk]});};
 try{const env={ACCESS_TEAM_DOMAIN:issuer,ACCESS_AUD:aud,DB:await database(),MEDIA:bucket(),BRAND:'celebrations',DEV_USER:'must-be-discarded'};
 const worker={fetch:(r,e)=>api(r,e)};
 const forged=new Request('https://big.example.invalid/api/events',{headers:{'oai-authenticated-user-id':'organiser-1'}});
 assert.equal((await dispatch(forged,env,worker)).status,401);
 const good=await token(),r=new Request('https://big.example.invalid/api/events',{headers:{cookie:'CF_Authorization='+good}});
 assert.equal((await dispatch(r,env,worker)).status,200);
 assert.equal((await accessIdentity(r,env)).email,'organiser@example.invalid');
 for(const bad of [await token({aud:['wrong']}),await token({exp:1}),await token({iss:'https://other.cloudflareaccess.com'}),good.slice(0,-20)+'A'.repeat(20),'not.a.jwt'])assert.equal(await accessIdentity(new Request(r.url,{headers:{'cf-access-jwt-assertion':bad}}),env),null);
 const unknown=new Request(r.url,{headers:{'x-bds-invite':'invalid'}});assert.equal((await dispatch(unknown,env,worker)).status,403);
 const login=await dispatch(new Request('https://big.example.invalid/organiser-login',{headers:{cookie:'CF_Authorization='+good}}),env,worker);assert.equal(login.status,302);assert.equal(login.headers.get('location'),'https://big.example.invalid/app.html');
 }finally{globalThis.fetch=savedFetch;}
});
