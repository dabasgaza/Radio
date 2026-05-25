using System.Text.Json;

namespace Radio.Views.Episodes;

public class EpisodeDraft
{
    public int? ProgramId { get; set; }
    public string? ProgramName { get; set; }
    public string? EpisodeName { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public TimeSpan? BroadcastTime { get; set; }
    public string? SpecialNotes { get; set; }
    public List<DraftGuestItem> Guests { get; set; } = [];
    public List<DraftCorrespondentItem> Correspondents { get; set; } = [];
    public List<DraftEmployeeItem> Employees { get; set; } = [];
    public DateTime SavedAt { get; set; }
}

public class DraftGuestItem
{
    public int GuestId { get; set; }
    public string? FullName { get; set; }
    public string? Topic { get; set; }
    public TimeSpan? HostingTime { get; set; }
}

public class DraftCorrespondentItem
{
    public int CorrespondentId { get; set; }
    public string? FullName { get; set; }
    public string? Topic { get; set; }
    public TimeSpan? HostingTime { get; set; }
}

public class DraftEmployeeItem
{
    public int EmployeeId { get; set; }
    public string? FullName { get; set; }
    public string? StaffRoleName { get; set; }
}

public class EpisodeDraftService
{
    private const string DraftFileName = "new_episode_draft.json";
    private static readonly string DraftDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RadioApp", "Drafts");
    private static readonly string DraftPath = System.IO.Path.Combine(DraftDir, DraftFileName);

    public async Task SaveDraftAsync(EpisodeDraft draft)
    {
        try
        {
            System.IO.Directory.CreateDirectory(DraftDir);
            draft.SavedAt = DateTime.Now;
            var json = JsonSerializer.Serialize(draft, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(DraftPath, json);
        }
        catch { }
    }

    public async Task<EpisodeDraft?> LoadDraftAsync()
    {
        try
        {
            if (!System.IO.File.Exists(DraftPath)) return null;
            var json = await System.IO.File.ReadAllTextAsync(DraftPath);
            return JsonSerializer.Deserialize<EpisodeDraft>(json);
        }
        catch
        {
            return null;
        }
    }

    public void DeleteDraft()
    {
        try
        {
            if (System.IO.File.Exists(DraftPath))
                System.IO.File.Delete(DraftPath);
        }
        catch { }
    }

    public bool HasDraft() => System.IO.File.Exists(DraftPath);
}
