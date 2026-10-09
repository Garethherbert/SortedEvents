// Planning model, not invoices: public provider list prices as researched 9 Oct 2026.
const fx=.8; // Deliberately fixed scenario: GBP per USD, not a live exchange rate.
const workers=(requests,cpuMs=5)=>(5+Math.max(0,requests-10e6)/1e6*.30+Math.max(0,requests*cpuMs-30e6)/1e6*.02)*fx;
const r2=(gb,writes,reads)=>(Math.max(0,gb-10)*.015+Math.max(0,writes-1e6)/1e6*4.5+Math.max(0,reads-10e6)/1e6*.36)*fx;
for(const scenario of [{name:'100 events, 250 invitees each; modest usage',accounts:100,requests:2.5e6,gb:1000,writes:.2e6,reads:1e6,emails:50000},{name:'1,000 events, 250 invitees each; modest usage',accounts:1000,requests:25e6,gb:10000,writes:2e6,reads:10e6,emails:500000},{name:'1,000 events; heavy gallery use',accounts:1000,requests:100e6,gb:20000,writes:5e6,reads:50e6,emails:500000}]){
 const email=(20+Math.max(0,scenario.emails-50000)/1000*.9)*fx;const total=workers(scenario.requests)+r2(scenario.gb,scenario.writes,scenario.reads)+email;
 console.log(JSON.stringify({...scenario,workersGBP:+workers(scenario.requests).toFixed(2),r2GBP:+r2(scenario.gb,scenario.writes,scenario.reads).toFixed(2),emailGBP:+email.toFixed(2),totalGBP:+total.toFixed(2),perAccountGBP:+(total/scenario.accounts).toFixed(2)}));
}
for(const p of [{name:'Old Business',price:19.99,gb:100},{name:'Recommended Personal',price:9.99,gb:10},{name:'Recommended Premium',price:19.99,gb:20},{name:'Recommended Business',price:49.99,gb:50}]){
 const fee=p.price*.022+.2,storage=p.gb*.015*fx,netVat=p.price/1.2;
 console.log(JSON.stringify({name:p.name,netVatGBP:+netVat.toFixed(2),paymentAndBillingGBP:+fee.toFixed(2),fullQuotaStorageGBP:+storage.toFixed(2),beforeOtherCostsGBP:+(netVat-fee-storage).toFixed(2),after15minSupportGBP:+(netVat-fee-storage-7.5).toFixed(2)}));
}
