// Duration-based critical path. Clock times are reported separately; waiting gaps
// are not tasks and therefore do not extend this dependency calculation.
export function criticalPath(items,day){
 const nodes=items.filter(x=>x.day===day),byId=new Map(nodes.map(x=>[x.id,x]));
 const state=new Map(),earliest=new Map(),order=[],warnings=[];
 const visit=n=>{if(state.get(n.id)===1)throw Error('Dependencies form a loop. Edit “Depends on” to break the loop.');if(state.get(n.id)===2)return;state.set(n.id,1);const parent=byId.get(n.dependsOn);if(n.dependsOn&&!parent)warnings.push(`${n.title}: dependency is outside this day or missing; excluded from this day’s calculation.`);if(parent)visit(parent);earliest.set(n.id,parent?earliest.get(parent.id)+Number(parent.duration):0);state.set(n.id,2);order.push(n);};
 nodes.forEach(visit);const length=Math.max(0,...nodes.map(n=>earliest.get(n.id)+Number(n.duration)));const latest=new Map(nodes.map(n=>[n.id,length-Number(n.duration)]));
 for(const n of order.slice().reverse()){const parent=byId.get(n.dependsOn);if(parent)latest.set(parent.id,Math.min(latest.get(parent.id),latest.get(n.id)-Number(parent.duration)));}
 const result=nodes.map(n=>({...n,earliest:earliest.get(n.id),slack:Math.max(0,latest.get(n.id)-earliest.get(n.id)),critical:Math.abs(latest.get(n.id)-earliest.get(n.id))<.01}));
 for(const n of nodes){const parent=byId.get(n.dependsOn);if(parent&&minutes(n.time)<minutes(parent.time)+Number(parent.duration))warnings.push(`${n.title} starts before ${parent.title} finishes.`);}
 return {nodes:result,length,warnings};
}
export const minutes=t=>Number(t.slice(0,2))*60+Number(t.slice(3,5));
export function reorderDay(timeline,day,stage,sourceId,targetId){
 const items=timeline.filter(x=>x.day===day&&(!stage||x.location===stage)).sort((a,b)=>a.time.localeCompare(b.time));
 const from=items.findIndex(x=>x.id===sourceId),to=items.findIndex(x=>x.id===targetId);if(from<0||to<0||from===to)return timeline;
 const start=Math.min(...items.map(x=>minutes(x.time)));const [item]=items.splice(from,1);items.splice(items.findIndex(x=>x.id===targetId),0,item);
 if(start+items.reduce((s,x)=>s+Number(x.duration),0)>1440)throw Error('These moments do not fit before midnight. Choose one stage/area or shorten the durations.');
 let clock=start;const times=new Map();for(const x of items){times.set(x.id,String(Math.floor(clock/60)).padStart(2,'0')+':'+String(clock%60).padStart(2,'0'));clock+=Number(x.duration);}return timeline.map(x=>times.has(x.id)?{...x,time:times.get(x.id)}:x);
}

// Dependency depth determines the row; independent activities sit side by side.
// Area filtering retains same-day ancestors as context rather than hiding edges.
export function criticalGraphLayout(nodes,area=''){
 const byId=new Map(nodes.map(n=>[n.id,n])),visible=new Set(),depths=new Map(),visiting=new Set();
 const include=n=>{if(visible.has(n.id))return;visible.add(n.id);const parent=byId.get(n.dependsOn);if(parent)include(parent);};
 nodes.filter(n=>!area||n.location===area).forEach(include);
 const depth=n=>{if(depths.has(n.id))return depths.get(n.id);if(visiting.has(n.id))throw Error('Dependencies form a loop.');visiting.add(n.id);const parent=byId.get(n.dependsOn);const d=parent?depth(parent)+1:0;visiting.delete(n.id);depths.set(n.id,d);return d;};
 const layers=new Map();for(const n of nodes.filter(n=>visible.has(n.id)).sort((a,b)=>a.time.localeCompare(b.time))){const d=depth(n);if(!layers.has(d))layers.set(d,[]);layers.get(d).push(n);}
 const positioned=[];for(const [level,row]of layers)row.forEach((n,i)=>positioned.push({...n,x:n.graph?.x??32+i*320,y:n.graph?.y??40+level*220,width:n.graph?.width??280,height:n.graph?.height??180,context:!!area&&n.location!==area}));
 const positions=new Map(positioned.map(n=>[n.id,n]));const edges=[];for(const n of positioned){const p=positions.get(n.dependsOn);if(p)edges.push({from:p.id,to:n.id,critical:p.critical&&n.critical,x1:p.x+p.width/2,y1:p.y+p.height,x2:n.x+n.width/2,y2:n.y});}
 return {nodes:positioned,edges,width:Math.max(360,...positioned.map(n=>n.x+n.width+32)),height:Math.max(230,...positioned.map(n=>n.y+n.height+40))};
}
