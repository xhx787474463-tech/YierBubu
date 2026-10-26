using System.Globalization;
using System.Text.Json;
namespace YierBubu.Core;

/// Persists daily keystroke totals only: no characters, no history, no per-key data.
public sealed class TypingStats
{
    private readonly string path;
    private readonly Dictionary<DateOnly,long> daily=[];
    private bool dirty;
    public TypingStats(string path)
    {
        this.path=path;
        try
        {
            if(!File.Exists(path))return;
            using var document=JsonDocument.Parse(File.ReadAllText(path));
            if(!document.RootElement.TryGetProperty("daily",out var map))return;
            foreach(var entry in map.EnumerateObject())
                if(DateOnly.TryParseExact(entry.Name,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var day)&&entry.Value.TryGetInt64(out var count)&&count>0)
                    daily[day]=count;
        }
        catch(IOException){}catch(JsonException){}catch(UnauthorizedAccessException){}
    }
    public long On(DateOnly day)=>daily.GetValueOrDefault(day);
    public long Since(DateOnly from)
    {
        long total=0;foreach(var pair in daily)if(pair.Key>=from)total+=pair.Value;return total;
    }
    public long Total
    {
        get{long total=0;foreach(var value in daily.Values)total+=value;return total;}
    }
    /// Weeks start on Monday (ISO 8601).
    public static DateOnly WeekStart(DateOnly day)=>day.AddDays(-((int)day.DayOfWeek+6)%7);
    public static DateOnly MonthStart(DateOnly day)=>new(day.Year,day.Month,1);
    public void Add(DateOnly day,long count=1)
    {
        if(count<=0)return;
        dirty=true;
        daily[day]=Math.Min(daily.GetValueOrDefault(day)+count,1_000_000_000_000);
    }
    public void Prune(DateOnly keepFrom)
    {
        DateOnly[] stale=[.. daily.Keys.Where(day=>day<keepFrom)];
        if(stale.Length==0)return;
        foreach(var day in stale)daily.Remove(day);
        dirty=true;
    }
    public void Flush()
    {
        if(!dirty)return;
        try
        {
            var directory=Path.GetDirectoryName(Path.GetFullPath(path));
            if(!string.IsNullOrEmpty(directory))Directory.CreateDirectory(directory);
            using var stream=File.Create(path);
            using var writer=new Utf8JsonWriter(stream);
            writer.WriteStartObject();
            writer.WritePropertyName("daily");writer.WriteStartObject();
            foreach(var pair in daily.OrderBy(p=>p.Key))
            {
                writer.WritePropertyName(pair.Key.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));
                writer.WriteNumberValue(pair.Value);
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
            dirty=false;
        }
        catch(IOException){}catch(UnauthorizedAccessException){}
    }
}