using BigDaySorted.Core;
namespace BigDaySorted.App;
public sealed class App : Application
{
    public App(){UserAppTheme=AppTheme.Light;Resources=new ResourceDictionary { ["Primary"]=Color.FromArgb(ProductBrand.Accent) };
        var ink=Color.FromArgb("#142d39");var muted=Color.FromArgb("#526773");
        Resources.Add(new Style(typeof(Entry)){Setters={new Setter{Property=Entry.TextColorProperty,Value=ink},new Setter{Property=Entry.PlaceholderColorProperty,Value=muted},new Setter{Property=Entry.BackgroundColorProperty,Value=Colors.White},new Setter{Property=Entry.FontSizeProperty,Value=16}}});
        Resources.Add(new Style(typeof(Editor)){Setters={new Setter{Property=Editor.TextColorProperty,Value=ink},new Setter{Property=Editor.PlaceholderColorProperty,Value=muted},new Setter{Property=Editor.BackgroundColorProperty,Value=Colors.White},new Setter{Property=Editor.FontSizeProperty,Value=16}}});
        Resources.Add(new Style(typeof(Picker)){Setters={new Setter{Property=Picker.TextColorProperty,Value=ink},new Setter{Property=Picker.TitleColorProperty,Value=muted},new Setter{Property=Picker.BackgroundColorProperty,Value=Colors.White},new Setter{Property=Picker.FontSizeProperty,Value=16}}});
        Resources.Add(new Style(typeof(DatePicker)){Setters={new Setter{Property=DatePicker.TextColorProperty,Value=ink},new Setter{Property=DatePicker.BackgroundColorProperty,Value=Colors.White},new Setter{Property=DatePicker.FontSizeProperty,Value=16}}});
        Resources.Add(new Style(typeof(TimePicker)){Setters={new Setter{Property=TimePicker.TextColorProperty,Value=ink},new Setter{Property=TimePicker.BackgroundColorProperty,Value=Colors.White},new Setter{Property=TimePicker.FontSizeProperty,Value=16}}});
    }
    public static Task ReceiveLinkAsync(string link)=>MainThread.InvokeOnMainThreadAsync(async()=>{if(Current?.Windows.FirstOrDefault()?.Page is NavigationPage nav && nav.RootPage is MainPage main)await main.ReceiveTransferAsync(link);});
    protected override Window CreateWindow(IActivationState? state)=>new(new NavigationPage(new MainPage()){BarBackgroundColor=Color.FromArgb(ProductBrand.Header),BarTextColor=Colors.White});
}
