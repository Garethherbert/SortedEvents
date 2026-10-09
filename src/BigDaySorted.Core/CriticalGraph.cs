using System.Globalization;
using System.Text.Json;
namespace BigDaySorted.Core;

/// <summary>Offline diagram sharing pointer interaction code with the website.</summary>
public static class CriticalGraph
{
    public static string Html(CriticalResult result,string area,double zoom=1)
    {
        var all=result.Nodes.ToDictionary(n=>n.Moment.Id);var selected=new HashSet<Guid>();
        void Include(CriticalNode n){if(!selected.Add(n.Moment.Id))return;if(n.Moment.DependsOn is Guid p&&all.TryGetValue(p,out var parent))Include(parent);}
        foreach(var n in result.Nodes.Where(n=>area==""||n.Moment.Location==area))Include(n);
        var depths=new Dictionary<Guid,int>();var visiting=new HashSet<Guid>();
        int Depth(CriticalNode n){var id=n.Moment.Id;if(depths.TryGetValue(id,out var d))return d;if(!visiting.Add(id))throw new InvalidOperationException("Dependencies form a loop.");d=n.Moment.DependsOn is Guid p&&all.TryGetValue(p,out var parent)?Depth(parent)+1:0;visiting.Remove(id);depths[id]=d;return d;}
        var boxes=new Dictionary<Guid,GraphBox>();
        foreach(var group in result.Nodes.Where(n=>selected.Contains(n.Moment.Id)).OrderBy(n=>n.Moment.Time).GroupBy(Depth))
        {int col=0;foreach(var n in group){boxes[n.Moment.Id]=n.Moment.Graph??new GraphBox {X=32+col*320,Y=40+group.Key*220};col++;}}
        var nodes=result.Nodes.Where(n=>selected.Contains(n.Moment.Id)).Select(n=>new {id=n.Moment.Id.ToString(),x=boxes[n.Moment.Id].X,y=boxes[n.Moment.Id].Y,width=boxes[n.Moment.Id].Width,height=boxes[n.Moment.Id].Height,title=n.Moment.Title,time=n.Moment.Time.ToString(@"hh\:mm"),duration=n.Moment.Duration,who=n.Moment.Who,location=n.Moment.Location,critical=n.IsCritical,context=area!=""&&n.Moment.Location!=area,slack=n.Float}).ToList();
        var edges=result.Nodes.Where(n=>selected.Contains(n.Moment.Id)&&n.Moment.DependsOn is Guid p&&selected.Contains(p)).Select(n=>new {from=n.Moment.DependsOn!.Value.ToString(),to=n.Moment.Id.ToString(),critical=n.IsCritical&&all[n.Moment.DependsOn.Value].IsCritical}).ToList();
        using var resource=typeof(CriticalGraph).Assembly.GetManifestResourceStream("Sorted.GraphInteraction")??throw new InvalidOperationException("Graph interaction resource missing.");using var reader=new StreamReader(resource);var interaction=reader.ReadToEnd().Replace("export function enableGraphInteractions","function enableGraphInteractions");
        var json=JsonSerializer.Serialize(nodes);var edgeJson=JsonSerializer.Serialize(edges);var scale=Math.Clamp(zoom,.5,2).ToString(CultureInfo.InvariantCulture);
        return $$"""
<!doctype html><html><head><meta name="viewport" content="width=device-width,initial-scale=1"><meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline'"><style>
body{margin:0;background:#edf3f7;color:#173345;font:16px Arial;overflow:auto}.graphcanvas{position:relative;transform-origin:top left}.graphcanvas>svg{position:absolute}.graphnode{box-sizing:border-box;position:absolute;display:flex;flex-direction:column;gap:5px;padding:12px 12px 30px;background:white;border:2px solid #657585;border-radius:12px;overflow:auto;font-size:14px}.graphnode strong{font-size:16px}.critical{background:#fff3da;border-color:#a75700}.context{border-style:dashed}.graphnode button{font-size:14px;padding:5px;color:#173345;background:#e2eaf0;border:1px solid #7893a4;border-radius:5px}.graphresize{position:absolute;right:4px;bottom:4px;touch-action:none;cursor:nwse-resize}[data-graph-action=move]{touch-action:none;cursor:grab;align-self:flex-start}.graphcanvas[data-saving] button{pointer-events:none;opacity:.5}#status{padding:8px;font-size:14px}
</style></head><body><div id="status" role="status">Drag Move; use the corner to resize. Arrow keys work on each handle.</div><div id="space"><div class="graphcanvas"><svg><defs><marker id="normal" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="8" markerHeight="8" orient="auto"><path d="M0 0 L10 5 L0 10z" fill="#657585"/></marker><marker id="critical" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="8" markerHeight="8" orient="auto"><path d="M0 0 L10 5 L0 10z" fill="#a75700"/></marker></defs></svg></div></div><script>
{{interaction}}
const nodes={{json}},edges={{edgeJson}},scale={{scale}},root=document.querySelector('.graphcanvas');root.style.transform=`scale(${scale})`;
for(const e of edges){const p=document.createElementNS('http://www.w3.org/2000/svg','path');p.dataset.edgeTo=e.to;p.setAttribute('fill','none');p.setAttribute('stroke',e.critical?'#a75700':'#657585');p.setAttribute('stroke-width',e.critical?'3':'2');p.setAttribute('marker-end',`url(#${e.critical?'critical':'normal'})`);root.querySelector('svg').append(p);}
for(const n of nodes){const card=document.createElement('article');card.className='graphnode'+(n.critical?' critical':'')+(n.context?' context':'');card.dataset.graphId=n.id;const move=document.createElement('button');move.textContent='Move';move.dataset.graphAction='move';move.setAttribute('aria-label','Move '+n.title);card.append(move);for(const [tag,text]of [['span',(n.context?'Context · ':'')+(n.critical?'CRITICAL':'Float: '+n.slack+' min')],['strong',n.title],['span',n.time+' · '+n.duration+' min · '+n.location],['span',n.who||'Unassigned']]){const el=document.createElement(tag);el.textContent=text;card.append(el);}const resize=document.createElement('button');resize.className='graphresize';resize.dataset.graphAction='resize';resize.textContent='↘';resize.setAttribute('aria-label','Resize '+n.title);card.append(resize);root.append(card);}
let acknowledge=null;window.sortedGraphSaved=ok=>{document.querySelector('#status').textContent=ok?'Layout saved':'Could not save layout. Try again.';if(acknowledge){ok?acknowledge.resolve():acknowledge.reject(Error('Not saved'));acknowledge=null;}};
enableGraphInteractions({root,nodes,edges,zoom:()=>scale,onSave:(id,box)=>new Promise((resolve,reject)=>{acknowledge={resolve,reject};document.querySelector('#status').textContent='Saving…';location.href='sortedgraph://save?data='+encodeURIComponent(JSON.stringify({id,box}));})});
</script></body></html>
""";
    }
}
