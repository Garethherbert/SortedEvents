using System.Text.Json;
using KeepMyTravels.Infrastructure;
using KeepMyTravels.Core.Models;
namespace BigDaySorted.Core;
public sealed class EventStore
{
    private readonly string _root;
    private readonly TravelRepository _repository;
    private readonly SemaphoreSlim _lock=new(1,1);
    private static readonly JsonSerializerOptions Options=new(){WriteIndented=true};
    public EventStore(string root){_root=Path.Combine(root,ProductBrand.Name);Directory.CreateDirectory(_root);_repository=new TravelRepository(Path.Combine(_root,"events.db3"));}
    public async Task<List<EventPlan>> LoadAsync(){await _lock.WaitAsync();try{await _repository.InitializeAsync();var result=new List<EventPlan>();foreach(var f in Directory.GetFiles(_root,"*.bigday.json")){var plan=JsonSerializer.Deserialize<EventPlan>(await File.ReadAllTextAsync(f),Options);if(plan is not null)result.Add(plan);}return result.OrderBy(x=>x.Start).ToList();}finally{_lock.Release();}}
    public async Task SaveAsync(EventPlan plan)
    {
        if(string.IsNullOrWhiteSpace(plan.Name)||plan.End.Date<plan.Start.Date)throw new InvalidOperationException("Enter an event name and valid date range.");
        if(plan.Timeline.Any(x=>x.Graph is not null&&!x.Graph.Valid))throw new InvalidOperationException("Check graph card position and size.");
        if(plan.Timeline.Any(x=>x.Day.Date<plan.Start.Date||x.Day.Date>plan.End.Date||x.Time<TimeSpan.Zero||x.Time>=TimeSpan.FromDays(1)||string.IsNullOrWhiteSpace(x.Title)))throw new InvalidOperationException("Check every moment's title, day and time.");
        await _lock.WaitAsync();try{
            await _repository.InitializeAsync();await _repository.UpsertTripAsync(plan.AsTrip());
            foreach(var moment in plan.Timeline)await _repository.UpsertItineraryItemAsync(new(){Id=moment.Id,TripId=plan.Id,Title=moment.Title,StartDateTime=moment.Day.Date+moment.Time,EndDateTime=moment.Day.Date+moment.Time+TimeSpan.FromMinutes(moment.Duration),DayNumber=(moment.Day.Date-plan.Start.Date).Days+1,Location=moment.Location,Notes=moment.Notes});
            foreach(var task in plan.Tasks)await _repository.UpsertChecklistItemAsync(new(){Id=task.Id,TripId=plan.Id,Text=task.Title,IsCompleted=task.Done});
            var path=Path.Combine(_root,plan.Id+".bigday.json");var temp=path+".tmp";await File.WriteAllTextAsync(temp,JsonSerializer.Serialize(plan,Options));File.Move(temp,path,true);
        }finally{_lock.Release();}
    }
    public static string Export(EventPlan p)=>JsonSerializer.Serialize(new {format="BigDaySorted",version=1,@event=new {name=p.Name,type=p.Type,start=p.Start.ToString("yyyy-MM-dd"),end=p.End.ToString("yyyy-MM-dd"),timezone=p.TimeZone,location=p.Location,currency=p.Currency,budget=p.Budget,notes=p.Notes,setup=new{stages=p.Setup.Stages,bandsPerDay=p.Setup.BandsPerDay,guests=p.Setup.Guests},registries=p.Registries.Select(r=>new{title=r.Title,url=r.Url}),timeline=p.Timeline.Select(t=>new{id=t.Id,day=t.Day.ToString("yyyy-MM-dd"),time=t.Time.ToString(@"hh\:mm"),duration=t.Duration,title=t.Title,who=t.Who,track=t.Track,location=t.Location,status=t.Status,notes=t.Notes,dependsOn=t.DependsOn?.ToString()??"",artist=t.Artist,kind=t.Kind,@public=t.Public,graph=t.Graph is null?null:new{x=t.Graph.X,y=t.Graph.Y,width=t.Graph.Width,height=t.Graph.Height}}),tasks=p.Tasks.Select(t=>new{id=t.Id,title=t.Title,who=t.Who,due=t.Due.ToString("yyyy-MM-dd"),done=t.Done,notes=t.Notes}),suppliers=p.Suppliers.Select(s=>new{id=s.Id,name=s.Name,service=s.Service,email=s.Email,phone=s.Phone,cost=s.Cost,paid=s.Paid,due=s.Due.ToString("yyyy-MM-dd"),arrival=s.Arrival,notes=s.Notes})}},Options);
    public static EventPlan Import(string json)
    {
        using var doc=JsonDocument.Parse(json);var root=doc.RootElement;if(root.GetProperty("format").GetString()!="BigDaySorted"||root.GetProperty("version").GetInt32()!=1)throw new InvalidOperationException("Unsupported event archive.");var d=root.GetProperty("event");string Get(JsonElement e,string k)=>e.TryGetProperty(k,out var v)?v.ToString():"";
        var p=EventPlan.Create(Get(d,"name"),Get(d,"type"),DateTime.Parse(Get(d,"start"),System.Globalization.CultureInfo.InvariantCulture),DateTime.Parse(Get(d,"end"),System.Globalization.CultureInfo.InvariantCulture));p.Location=Get(d,"location");p.Currency=Get(d,"currency");p.TimeZone=Get(d,"timezone");p.Notes=Get(d,"notes");p.Budget=d.TryGetProperty("budget",out var budget)?budget.GetDecimal():0;p.Timeline=[];p.Tasks=[];if(d.TryGetProperty("setup",out var setup)){p.Setup.Stages=setup.TryGetProperty("stages",out var stages)?stages.GetInt32():1;p.Setup.Guests=setup.TryGetProperty("guests",out var guests)?guests.GetInt32():0;if(setup.TryGetProperty("bandsPerDay",out var bands))p.Setup.BandsPerDay=bands.EnumerateArray().Select(x=>x.GetInt32()).ToList();}
        if(d.TryGetProperty("timeline",out var timeline))foreach(var t in timeline.EnumerateArray())p.Timeline.Add(new(){Id=Guid.TryParse(Get(t,"id"),out var tid)?tid:Guid.NewGuid(),DependsOn=Guid.TryParse(Get(t,"dependsOn"),out var pid)?pid:null,Day=DateTime.Parse(Get(t,"day"),System.Globalization.CultureInfo.InvariantCulture),Time=TimeSpan.Parse(Get(t,"time"),System.Globalization.CultureInfo.InvariantCulture),Duration=t.GetProperty("duration").GetInt32(),Title=Get(t,"title"),Who=Get(t,"who"),Track=Get(t,"track"),Location=Get(t,"location"),Notes=Get(t,"notes"),Status=Get(t,"status"),Artist=Get(t,"artist"),Kind=Get(t,"kind")==""?"Operations":Get(t,"kind"),Graph=t.TryGetProperty("graph",out var box)&&box.ValueKind==JsonValueKind.Object?GraphBox.Parse(box):null,Public=t.TryGetProperty("public",out var visible)&&visible.ValueKind==JsonValueKind.True});
        if(d.TryGetProperty("tasks",out var tasks))foreach(var t in tasks.EnumerateArray())p.Tasks.Add(new(){Title=Get(t,"title"),Who=Get(t,"who"),Due=DateTime.TryParse(Get(t,"due"),out var due)?due:p.Start,Done=t.TryGetProperty("done",out var done)&&done.GetBoolean(),Notes=Get(t,"notes")});
        if(d.TryGetProperty("suppliers",out var suppliers))foreach(var s in suppliers.EnumerateArray())p.Suppliers.Add(new(){Name=Get(s,"name"),Service=Get(s,"service"),Email=Get(s,"email"),Phone=Get(s,"phone"),Cost=s.GetProperty("cost").GetDecimal(),Paid=s.GetProperty("paid").GetDecimal(),Due=DateTime.TryParse(Get(s,"due"),out var due)?due:p.Start,Arrival=Get(s,"arrival"),Notes=Get(s,"notes")});
        if(d.TryGetProperty("registries",out var links))foreach(var r in links.EnumerateArray())p.Registries.Add(new(){Title=Get(r,"title"),Url=Get(r,"url")});return p;
    }
    public static IEnumerable<string> Conflicts(EventPlan p){for(int i=0;i<p.Timeline.Count;i++)for(int j=i+1;j<p.Timeline.Count;j++){var a=p.Timeline[i];var b=p.Timeline[j];var aStart=a.Day.Date+a.Time;var bStart=b.Day.Date+b.Time;if(a.Kind=="Performance"&&b.Kind=="Performance"&&a.Location.Length>0&&a.Location==b.Location&&aStart<bStart.AddMinutes(b.Duration)&&bStart<aStart.AddMinutes(a.Duration))yield return $"{a.Location}: overlapping performances {a.Title} and {b.Title}";if(a.Who.Length>0&&a.Who==b.Who&&aStart<bStart.AddMinutes(b.Duration)&&bStart<aStart.AddMinutes(a.Duration))yield return $"{a.Who}: {a.Title} overlaps {b.Title}";}}
}
