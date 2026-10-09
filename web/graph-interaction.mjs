// Shared pointer/keyboard interaction for the website and offline native WebView.
export function enableGraphInteractions({root,nodes,edges,zoom=()=>1,onSave=async()=>{}}){
 const map=new Map(nodes.map(n=>[n.id,n]));let active=null,busy=false;
 const clamp=(v,min,max)=>Math.max(min,Math.min(max,Math.round(v)));
 const redraw=()=>{
  for(const n of nodes){const el=[...root.querySelectorAll('[data-graph-id]')].find(e=>e.dataset.graphId===n.id);if(!el)continue;Object.assign(el.style,{left:n.x+'px',top:n.y+'px',width:n.width+'px',height:n.height+'px'});}
  const width=Math.max(360,...nodes.map(n=>n.x+n.width+32)),height=Math.max(230,...nodes.map(n=>n.y+n.height+40));root.style.width=width+'px';root.style.height=height+'px';const svg=root.querySelector('svg');svg.setAttribute('width',width);svg.setAttribute('height',height);
  for(const e of edges){const a=map.get(e.from),b=map.get(e.to);if(!a||!b)continue;const x1=a.x+a.width/2,y1=a.y+a.height,x2=b.x+b.width/2,y2=b.y;const p=[...svg.querySelectorAll('[data-edge-to]')].find(p=>p.dataset.edgeTo===e.to);p?.setAttribute('d',`M ${x1} ${y1} C ${x1} ${y1+30}, ${x2} ${y2-30}, ${x2} ${y2}`);}
  const space=root.parentElement;space.style.width=width*zoom()+'px';space.style.height=height*zoom()+'px';
 };
 const commit=async(n,old)=>{busy=true;root.dataset.saving='true';try{await onSave(n.id,{x:n.x,y:n.y,width:n.width,height:n.height});}catch(e){Object.assign(n,old);redraw();}finally{busy=false;delete root.dataset.saving;}};
 const move=(n,kind,dx,dy)=>{if(kind==='resize'){n.width=clamp(n.width+dx,240,1000);n.height=clamp(n.height+dy,140,700);}else{n.x=clamp(n.x+dx,0,20000);n.y=clamp(n.y+dy,0,20000);}redraw();};
 root.addEventListener('pointerdown',e=>{const handle=e.target.closest('[data-graph-action]');if(!handle||busy||e.button!==0)return;const card=handle.closest('[data-graph-id]'),n=map.get(card?.dataset.graphId);if(!n)return;e.preventDefault();active={n,kind:handle.dataset.graphAction,old:{x:n.x,y:n.y,width:n.width,height:n.height},startX:e.clientX,startY:e.clientY,pointer:e.pointerId,handle};handle.setPointerCapture?.(e.pointerId);});
 root.addEventListener('pointermove',e=>{if(!active||active.pointer!==e.pointerId)return;e.preventDefault();const dx=(e.clientX-active.startX)/zoom(),dy=(e.clientY-active.startY)/zoom();Object.assign(active.n,active.old);move(active.n,active.kind,dx,dy);});
 root.addEventListener('pointerup',e=>{if(!active||active.pointer!==e.pointerId)return;const a=active;active=null;a.handle.releasePointerCapture?.(e.pointerId);void commit(a.n,a.old);});
 root.addEventListener('pointercancel',()=>{if(active){Object.assign(active.n,active.old);active=null;redraw();}});
 root.addEventListener('keydown',e=>{const handle=e.target.closest('[data-graph-action]');if(!handle||busy||!['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(e.key))return;const n=map.get(handle.closest('[data-graph-id]')?.dataset.graphId);if(!n)return;e.preventDefault();const old={x:n.x,y:n.y,width:n.width,height:n.height},step=e.shiftKey?40:10;move(n,handle.dataset.graphAction,e.key==='ArrowLeft'?-step:e.key==='ArrowRight'?step:0,e.key==='ArrowUp'?-step:e.key==='ArrowDown'?step:0);void commit(n,old);});
 redraw();return {redraw};
}
