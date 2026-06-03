using System.IO;
using System.Text.Json;
using System.Windows;

namespace Radio.Services;

/// <summary>
/// ضبط حجم خط النظام عبر تحديث موارد <c>AppFonts.xaml</c> ديناميكياً.
/// </summary>
public static class FontScaleService
{
    public const double MinScale = 0.85;
    public const double MaxScale = 1.35;
    public const double Step = 0.05;

    private static readonly string[] ScaledKeys =
    [
        "FontSize.Display", "FontSize.Headline", "FontSize.TitleLarge", "FontSize.TitleMedium",
        "FontSize.TitleSmall", "FontSize.BodyLarge", "FontSize.BodyMedium", "FontSize.BodySmall",
        "FontSize.Label", "FontSize.Caption", "FontSize.Overline",
        "FontSize.Input", "FontSize.Field", "FontSize.Dialog", "FontSize.Button",
        "FontSize.DataGrid", "FontSize.Numeric.Large", "FontSize.Numeric.Medium",
        "FontSize.Clock", "FontSize.Section", "FontSize.Nav", "FontSize.NavCompact",
        "FontSize.NavLabel", "FontSize.Micro", "FontSize.Tiny", "FontSize.Brand",
        "FontSize.MessageTitle",
        "FontSizeBody", "FontSizeInput", "FontSizeTitle", "FontSizeHeadline", "FontSizeCaption",
        "LineHeight.Display", "LineHeight.Headline", "LineHeight.TitleLarge", "LineHeight.TitleMedium",
        "LineHeight.TitleSmall", "LineHeight.BodyLarge", "LineHeight.BodyMedium", "LineHeight.BodySmall",
        "LineHeight.Label", "LineHeight.Caption", "LineHeight.Overline",
    ];

    private static readonly Dictionary<string, double> BaseValues = new(StringComparer.Ordinal);
    private static readonly string PreferencesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Radio",
        "ui-preferences.json");

    public static double CurrentScale { get; private set; } = 1.0;

    public static event Action<double>? ScaleChanged;

    public static void Initialize()
    {
        BaseValues.Clear();
        foreach (var key in ScaledKeys)
        {
            if (Application.Current.TryFindResource(key) is double value)
                BaseValues[key] = value;
        }

        LoadSavedScale();
        Apply(CurrentScale);
    }

    public static void Increase()
    {
        if (CurrentScale + Step <= MaxScale + 0.001)
            Apply(Math.Round(CurrentScale + Step, 2));
    }

    public static void Decrease()
    {
        if (CurrentScale - Step >= MinScale - 0.001)
            Apply(Math.Round(CurrentScale - Step, 2));
    }

    public static void Reset() => Apply(1.0);

    public static bool CanIncrease => CurrentScale < MaxScale - 0.001;
    public static bool CanDecrease => CurrentScale > MinScale + 0.001;

    public static int Percent => (int)Math.Round(CurrentScale * 100);

    public static double GetScaled(string resourceKey)
    {
        if (!BaseValues.TryGetValue(resourceKey, out var baseValue))
            return 12 * CurrentScale;
        return Math.Round(baseValue * CurrentScale, 1);
    }

    public static void Apply(double scale)
    {
        scale = Math.Clamp(Math.Round(scale, 2), MinScale, MaxScale);
        CurrentScale = scale;

        foreach (var (key, baseValue) in BaseValues)
            Application.Current.Resources[key] = Math.Round(baseValue * scale, 1);

        Save();
        ScaleChanged?.Invoke(scale);
    }

    private static void LoadSavedScale()
    {
        try
        {
            if (!File.Exists(PreferencesPath))
                return;

            var json = File.ReadAllText(PreferencesPath);
            var prefs = JsonSerializer.Deserialize<UiPreferences>(json);
            if (prefs?.FontScale is >= MinScale and <= MaxScale)
                CurrentScale = prefs.FontScale;
        }
        catch
        {
            CurrentScale = 1.0;
        }
    }

    private static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(PreferencesPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(new UiPreferences { FontScale = CurrentScale });
            File.WriteAllText(PreferencesPath, json);
        }
        catch
        {
            // تجاهل فشل الحفظ — لا يعطل التطبيق
        }
    }

    private sealed class UiPreferences
    {
        public double FontScale { get; set; } = 1.0;
    }
}
