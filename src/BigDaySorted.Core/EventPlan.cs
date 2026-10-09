using KeepMyTravels.Core.Models;
namespace BigDaySorted.Core;
public sealed class EventPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Our big day";
    public string Type { get; set; } = "Wedding";
    public DateTime Start { get; set; } = DateTime.Today.AddMonths(3);
    public DateTime End { get; set; } = DateTime.Today.AddMonths(3);
    public string TimeZone { get; set; } = "Europe/London";
    public string Location { get; set; } = "";
    public string Currency { get; set; } = "GBP";
    public decimal Budget { get; set; }
    public string Notes { get; set; } = "";
    public SetupOptions Setup {get;set;}=new();
    public List<Moment> Timeline { get; set; } = [];
    public List<EventTask> Tasks { get; set; } = [];
    public List<Supplier> Suppliers { get; set; } = [];
    public List<RegistryLink> Registries { get; set; } = [];
    public static readonly string[] Types = ["Wedding","Anniversary","Birthday","Concert","Festival","Charity fundraiser","Awards night","Corporate event","Graduation","Community celebration"];
    public Trip AsTrip() => new() { Id=Id, Name=Name, Destination=Location, StartDate=Start, EndDate=End, Budget=Budget, CurrencyCode=Currency, Notes=Notes };
    public static EventPlan Create(string name,string type,DateTime start,DateTime end,SetupOptions? setup=null)
    {
        var plan=new EventPlan {Name=name,Type=type,Start=start.Date,End=end.Date,Setup=setup??new SetupOptions {Stages=type=="Festival"?2:1}};if(end<start||(end.Date-start.Date).Days>29)throw new ArgumentException("Choose an event covering 1–30 days.");
        if(type is "Concert" or "Festival") {
            for(var day=start.Date;day<=end.Date;day=day.AddDays(1)) {
                void Add(string title,int hour,int minute,int duration,string who,string track,string stage,string artist="",string kind="Operations",bool visible=false) => plan.Timeline.Add(new(){Day=day,Time=new TimeSpan(hour,minute,0),Duration=duration,Title=title,Who=who,Track=track,Location=stage,Artist=artist,Kind=kind,Public=visible});
                Add("Daily crew briefing & weather review",8,0,30,"Production lead","Operations","Production office");
                Add("Stage, power and backline checks",9,0,60,"Technical lead","Technical","Main stage");
                Add("Access, welfare and emergency-route walk-through",11,0,30,"Operations lead","Operations","Site");
                Add("Gates open",14,0,30,"Front of house","Guests","Entrance",visible:true);
                var stages=Enumerable.Range(0,Math.Clamp(plan.Setup.Stages,1,5)).Select(j=>j==0?"Main stage":$"Stage {j+1}").ToArray();var dayIndex=(day-start.Date).Days;var bands=plan.Setup.BandsPerDay.ElementAtOrDefault(dayIndex);if(bands==0)bands=type=="Festival"?6:3;if(bands<1||bands>12)throw new ArgumentException("Choose 1–12 bands per day.");var step=480/(int)Math.Ceiling((double)bands/stages.Length);
                for(int b=0;b<bands;b++){
                    var j=b%stages.Length;var position=b/stages.Length;var artist=$"Day {dayIndex+1} · {stages[j]} {(b>=bands-stages.Length?"headliner":"band "+(b+1))}";var manager=stages[j]+" manager";var clock=900+position*step+j*5;
                    var sound=new Moment {Day=day,Time=TimeSpan.FromMinutes(570+position*25),Duration=20,Title=artist+" — soundcheck",Who=manager,Track="Technical",Location=stages[j],Artist=artist,Kind="Soundcheck"};plan.Timeline.Add(sound);
                    var change=new Moment {Day=day,Time=TimeSpan.FromMinutes(clock-15),Duration=15,Title=artist+" — changeover",Who=manager,Track="Technical",Location=stages[j],Artist=artist,Kind="Changeover",DependsOn=sound.Id};plan.Timeline.Add(change);
                    plan.Timeline.Add(new(){Day=day,Time=TimeSpan.FromMinutes(clock),Duration=Math.Min(60,step-15),Title=artist,Who=manager,Track="Entertainment",Location=stages[j],Artist=artist,Kind="Performance",Public=true,DependsOn=change.Id});
                }
                Add("Audience exit, welfare check and site sweep",22,30,30,"Operations lead","Operations","Site");
                Add(day==end.Date?"Final pack down and venue handover":"Overnight handover & next-day readiness",23,0,30,"Production lead","Operations","Production office");
                foreach(var title in new[]{"Confirm artist arrivals, hospitality and dressing rooms","Confirm soundchecks, backline and technical riders","Confirm crew shifts, breaks and overnight handover","Confirm gates, transport, welfare and accessibility arrangements","Review weather contingency, noise curfew and escalation contacts"})plan.Tasks.Add(new(){Title=$"Day {(day-start.Date).Days+1}: {title}",Who="Production lead",Due=day});
            }
            return plan;
        }
        string[] titles=type=="Wedding"?["Venue access & supplier check-in","Flowers, tables & sound check","Guests arrive","Ceremony","Group photographs","Meal service","Speeches","Evening guests & first dance","Pack down"]:type is "Concert" or "Festival"?["Crew briefing & access","Stage and technical checks","Safety and accessibility walk-through","Doors / gates open","First performance","Headline performance","Audience exit & site sweep"]:["Venue access and setup","Supplier check-in","Welcome guests","Main celebration","Food and refreshments","Venue handover"];
        for(int i=0;i<titles.Length;i++)plan.Timeline.Add(new(){Day=start.Date,Time=TimeSpan.FromHours(8+i*1.5),Title=titles[i],Who="Organiser",Track=i<2?"Setup":"Main event"});
        foreach(var title in new[]{"Confirm venue access","Confirm supplier arrivals","Check final guest and meal numbers","Prepare contingency contacts","Download final running order"})plan.Tasks.Add(new(){Title=title,Who="Organiser",Due=start.Date});
        return plan;
    }
}
public sealed class Moment { public Guid Id {get;set;}=Guid.NewGuid(); public DateTime Day {get;set;} public TimeSpan Time {get;set;} public int Duration {get;set;}=30; public string Title {get;set;}=""; public string Who {get;set;}=""; public string Track {get;set;}="Main event"; public string Location {get;set;}=""; public string Status {get;set;}="Planned"; public string Notes {get;set;}=""; public Guid? DependsOn {get;set;} public string Artist {get;set;}=""; public string Kind {get;set;}="Operations"; public bool Public {get;set;} public GraphBox? Graph {get;set;} }
public sealed class EventTask { public Guid Id {get;set;}=Guid.NewGuid(); public string Title {get;set;}=""; public string Who {get;set;}=""; public DateTime Due {get;set;} public bool Done {get;set;} public string Notes {get;set;}=""; }
public sealed class Supplier { public Guid Id {get;set;}=Guid.NewGuid(); public string Name {get;set;}=""; public string Service {get;set;}=""; public string Email {get;set;}=""; public string Phone {get;set;}=""; public decimal Cost {get;set;} public decimal Paid {get;set;} public DateTime Due {get;set;} public string Arrival {get;set;}=""; public string Notes {get;set;}=""; }
public sealed class RegistryLink { public string Title {get;set;}=""; public string Url {get;set;}=""; }

public sealed class SetupOptions {public int Stages {get;set;}=1;public List<int> BandsPerDay {get;set;}=[];public int Guests {get;set;}}
