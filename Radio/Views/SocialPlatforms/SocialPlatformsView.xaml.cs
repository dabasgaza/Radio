using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.SocialPlatforms;

public partial class SocialPlatformsView : UserControl
{
    private readonly IPlatformService _service;
    private readonly UserSession _session;
    private List<SocialMediaPlatformDto> _allItems = [];

    public SocialPlatformsView(IPlatformService service, UserSession session)
    {
        InitializeComponent();
        _service = service;
        _session = session;
        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        ProgressBar.Visibility = Visibility.Visible;
        try
        {
            _allItems = await _service.GetAllActiveAsync();
            ApplySearchFilter();
            UpdateStats();
        }
        finally
        {
            ProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private void ApplySearchFilter()
    {
        var search = TxtSearch.Text?.Trim() ?? string.Empty;
        DataGrid.ItemsSource = string.IsNullOrEmpty(search)
            ? _allItems
            : _allItems.Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void UpdateStats()
    {
        TxtTotal.Text = _allItems.Count.ToString();
    }

    private async void BtnAddNew_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = Window.GetWindow(this) as ModernMainWindow;
        if (mainWindow != null) await mainWindow.ShowOverlay();

        try
        {
            var dialog = new SocialPlatformFormDialog(null, _service, _session)
            {
                Owner = mainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                await LoadDataAsync();
            }
        }
        finally
        {
            if (mainWindow != null) await mainWindow.HideOverlay();
        }
    }

    private async void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not SocialMediaPlatformDto dto) return;

        var mainWindow = Window.GetWindow(this) as ModernMainWindow;
        if (mainWindow != null) await mainWindow.ShowOverlay();

        try
        {
            var dialog = new SocialPlatformFormDialog(dto, _service, _session)
            {
                Owner = mainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                await LoadDataAsync();
            }
        }
        finally
        {
            if (mainWindow != null) await mainWindow.HideOverlay();
        }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not SocialMediaPlatformDto dto) return;

        var confirm = await MessageService.Current.ShowConfirmationAsync(
            $"هل أنت متأكد من حذف المنصة \"{dto.Name}\"?",
            "تأكيد الحذف");

        if (confirm)
        {
            var response = await _service.DeleteAsync(dto.SocialMediaPlatformId, _session);
            if (response.IsSuccess)
            {
                await LoadDataAsync();
                MessageService.Current.ShowSuccess("تم حذف المنصة بنجاح.");
            }
            else
            {
                MessageService.Current.ShowError(response.ErrorMessage ?? "تعذر حذف المنصة.");
            }
        }
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplySearchFilter();
    }
}
