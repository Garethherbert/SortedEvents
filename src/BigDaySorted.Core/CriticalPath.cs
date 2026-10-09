namespace BigDaySorted.Core;
public sealed record CriticalNode(Moment Moment,int Earliest,int Float){public bool IsCritical=>Float==0;}
public sealed record CriticalResult(List<CriticalNode> Nodes,int Length,List<string> Warnings);
public static class CriticalPath
{
    public static CriticalResult Calculate(EventPlan plan,DateTime day)
    {
        var nodes=plan.Timeline.Where(x=>x.Day.Date==day.Date).ToList();var byId=nodes.ToDictionary(x=>x.Id);var states=new Dictionary<Guid,int>();var earliest=new Dictionary<Guid,int>();var ordered=new List<Moment>();var warnings=new List<string>();
        void Visit(Moment n){if(states.GetValueOrDefault(n.Id)==1)throw new InvalidOperationException("Dependencies form a loop. Edit Depends on to break the loop.");if(states.GetValueOrDefault(n.Id)==2)return;states[n.Id]=1;Moment? parent=null;if(n.DependsOn is Guid parentId&&!byId.TryGetValue(parentId,out parent))warnings.Add(n.Title+": dependency is outside this day or missing; excluded from this day's calculation.");if(parent is not null)Visit(parent);earliest[n.Id]=parent is null?0:earliest[parent.Id]+parent.Duration;states[n.Id]=2;ordered.Add(n);}
        foreach(var n in nodes)Visit(n);var length=nodes.Select(n=>earliest[n.Id]+n.Duration).DefaultIfEmpty(0).Max();var latest=nodes.ToDictionary(x=>x.Id,x=>length-x.Duration);
        foreach(var n in ordered.AsEnumerable().Reverse())if(n.DependsOn is Guid pid&&byId.TryGetValue(pid,out var parent))latest[pid]=Math.Min(latest[pid],latest[n.Id]-parent.Duration);
        foreach(var n in nodes)if(n.DependsOn is Guid pid&&byId.TryGetValue(pid,out var parent)&&n.Time<parent.Time.Add(TimeSpan.FromMinutes(parent.Duration)))warnings.Add(n.Title+" starts before "+parent.Title+" finishes.");
        return new(nodes.Select(n=>new CriticalNode(n,earliest[n.Id],Math.Max(0,latest[n.Id]-earliest[n.Id]))).ToList(),length,warnings);
    }
    public static void MoveBefore(EventPlan plan,DateTime day,string area,Guid source,Guid target)
    {
        var items=plan.Timeline.Where(x=>x.Day.Date==day.Date&&(area==""||x.Location==area)).OrderBy(x=>x.Time).ToList();var item=items.FirstOrDefault(x=>x.Id==source);if(item is null||!items.Any(x=>x.Id==target)||source==target)return;
        var clock=items.Min(x=>x.Time);items.Remove(item);items.Insert(items.FindIndex(x=>x.Id==target),item);if(clock.TotalMinutes+items.Sum(x=>x.Duration)>1440)throw new InvalidOperationException("These moments do not fit before midnight. Choose one stage/area or shorten durations.");
        foreach(var n in items){n.Time=clock;clock=clock.Add(TimeSpan.FromMinutes(n.Duration));}
    }
}
