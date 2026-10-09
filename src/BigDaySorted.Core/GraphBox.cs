using System.Text.Json;
namespace BigDaySorted.Core;
public sealed class GraphBox
{
    public int X {get;set;}
    public int Y {get;set;}
    public int Width {get;set;}=280;
    public int Height {get;set;}=180;
    public bool Valid=>X is >=0 and <=20000&&Y is >=0 and <=20000&&Width is >=240 and <=1000&&Height is >=140 and <=700;
    public static GraphBox Parse(JsonElement json)
    {
        var box=new GraphBox {X=json.GetProperty("x").GetInt32(),Y=json.GetProperty("y").GetInt32(),Width=json.GetProperty("width").GetInt32(),Height=json.GetProperty("height").GetInt32()};
        if(!box.Valid)throw new InvalidDataException("Check graph card position and size.");return box;
    }
}
