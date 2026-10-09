using BigDaySorted.Core;
using Foundation;
using UIKit;
namespace BigDaySorted.App;
[Register("AppDelegate")]
public class AppDelegate:MauiUIApplicationDelegate {
protected override MauiApp CreateMauiApp()=>MauiProgram.CreateMauiApp();
public override bool OpenUrl(UIApplication application,NSUrl url,NSDictionary options){var value=url.ToString();if(ProductBrand.IsTransferLink(value)){_ = App.ReceiveLinkAsync(value);return true;}return base.OpenUrl(application,url,options);}
}
