// Validate organiser identity independently of any client-supplied identity headers.
const keyCache=new Map();
const decode=s=>Uint8Array.from(atob(s.replaceAll('-','+').replaceAll('_','/')),c=>c.charCodeAt(0));
const parse=s=>JSON.parse(new TextDecoder().decode(decode(s)));
export async function accessIdentity(request,env){
 const cookie=request.headers.get('cookie')||'';
 const token=request.headers.get('cf-access-jwt-assertion')||cookie.split(';').map(x=>x.trim()).find(x=>x.startsWith('CF_Authorization='))?.slice(17);
 if(!token)return null;
 const issuer=env.ACCESS_TEAM_DOMAIN?.replace(/\/$/,'');
 if(!/^https:\/\/[a-z0-9-]+\.cloudflareaccess\.com$/.test(issuer||'')||!env.ACCESS_AUD)return null;
 try{
  if(token.length>16384)return null;
  const parts=token.split('.');if(parts.length!==3)return null;
  const header=parse(parts[0]),payload=parse(parts[1]),now=Date.now()/1000;
  if(header.alg!=='RS256'||!header.kid||payload.iss!==issuer||!Number.isFinite(payload.exp)||payload.exp<=now||payload.nbf&&payload.nbf>now+30||!payload.sub||typeof payload.email!=='string'||!payload.email)return null;
  const audiences=Array.isArray(payload.aud)?payload.aud:[payload.aud];if(!audiences.includes(env.ACCESS_AUD))return null;
  let cached=keyCache.get(issuer);
  if(!cached||cached.expires<Date.now()||!cached.keys.some(k=>k.kid===header.kid)){
   const response=await fetch(issuer+'/cdn-cgi/access/certs',{signal:AbortSignal.timeout(5000)});
   if(!response.ok)return null;
   const {keys}=await response.json();if(!Array.isArray(keys))return null;
   cached={keys,expires:Date.now()+300000};keyCache.set(issuer,cached);
  }
  const jwk=cached.keys.find(k=>k.kid===header.kid&&k.kty==='RSA');if(!jwk)return null;
  const key=await crypto.subtle.importKey('jwk',jwk,{name:'RSASSA-PKCS1-v1_5',hash:'SHA-256'},false,['verify']);
  if(!await crypto.subtle.verify('RSASSA-PKCS1-v1_5',key,decode(parts[2]),new TextEncoder().encode(parts[0]+'.'+parts[1])))return null;
  return {id:'cloudflare-access:'+payload.sub,email:payload.email};
 }catch{return null;}
}
export async function dispatch(request,env,worker,ctx){
 const identity=await accessIdentity(request,env),headers=new Headers(request.headers);
 headers.delete('oai-authenticated-user-id');headers.delete('oai-authenticated-user-email');
 if(identity){headers.set('oai-authenticated-user-id',identity.id);headers.set('oai-authenticated-user-email',identity.email);}
 const path=new URL(request.url).pathname;
 if(path==='/organiser-login')return identity?Response.redirect(new URL('/app.html',request.url),302):new Response('Configure Cloudflare Access on /organiser-login and sign in.',{status:401});
 if(path==='/health')return new Response(JSON.stringify({ok:true,brand:env.PRODUCT_BRAND}),{headers:{'content-type':'application/json'}});
 const {DEV_USER,DEV_EMAIL,...safeEnv}=env;
 return worker.fetch(new Request(request,{headers}),safeEnv,ctx);
}
