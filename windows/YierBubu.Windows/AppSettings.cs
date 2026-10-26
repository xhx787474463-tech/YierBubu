using System.Text.Json;
namespace YierBubu.Windows;

public sealed class AppSettings
{
    public double Scale {get;set;}=1;
    public bool ClickThrough {get;set;}
    public double? Left {get;set;}
    public double? Top {get;set;}
    public static string Root=>System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"YierBubu");
    public static string FilePath=>System.IO.Path.Combine(Root,"settings.json");
    public static AppSettings Load()
    {
        try
        {
            if(!File.Exists(FilePath))return new();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath))??new();
        }
        catch(IOException){}catch(JsonException){}catch(UnauthorizedAccessException){}
        return new();
    }
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(FilePath,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));
        }
        catch(IOException){}catch(UnauthorizedAccessException){}
    }
}