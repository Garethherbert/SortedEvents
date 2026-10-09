using BigDaySorted.Core;
using Android.App;
using Android.OS;
using Android.Content;
namespace BigDaySorted.App;
[IntentFilter(new[] {Intent.ActionView},Categories=new[]{Intent.CategoryDefault,Intent.CategoryBrowsable},DataScheme=ProductBrand.TransferScheme,DataHost="transfer")]
[Activity(Theme="@style/Maui.SplashTheme",MainLauncher=true,Exported=true,LaunchMode=Android.Content.PM.LaunchMode.SingleTop,ConfigurationChanges=Android.Content.PM.ConfigChanges.ScreenSize|Android.Content.PM.ConfigChanges.Orientation|Android.Content.PM.ConfigChanges.UiMode|Android.Content.PM.ConfigChanges.ScreenLayout|Android.Content.PM.ConfigChanges.SmallestScreenSize|Android.Content.PM.ConfigChanges.Density)]
public class MainActivity:MauiAppCompatActivity {
protected override void OnCreate(Bundle? state){base.OnCreate(state);Receive(Intent);}
protected override void OnNewIntent(Intent? intent){base.OnNewIntent(intent);Receive(intent);}
private static void Receive(Intent? intent){if(intent?.Data?.Scheme==ProductBrand.TransferScheme&&intent.Data.Host=="transfer")MainThread.BeginInvokeOnMainThread(async()=>{await Task.Delay(600);await App.ReceiveLinkAsync(intent.Data.ToString()!);});}
}
