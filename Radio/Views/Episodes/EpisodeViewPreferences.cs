using System.Text.Json;

namespace Radio.Views.Episodes;

public class EpisodeViewPreferences
{
    public string ViewMode { get; set; } = "Cards";
    public int SortIndex { get; set; } = 0;
    public string? ProgramFilter { get; set; }
    public string? StatusFilter { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? DatePreset { get; set; }
    public string SearchText { get; set; } = string.Empty;

    private static readonly string FilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RadioApp", "preferences.json");

    public static EpisodeViewPreferences Load()
    {
        try
        {
            if (System.IO.File.Exists(FilePath))
            {
                var json = System.IO.File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<EpisodeViewPreferences>(json) ?? new EpisodeViewPreferences();
            }
        }
        catch { }
        return new EpisodeViewPreferences();
    }

    public void Save()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(FilePath);
            if (dir != null) System.IO.Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(FilePath, json);
        }
        catch { }
    }
}
