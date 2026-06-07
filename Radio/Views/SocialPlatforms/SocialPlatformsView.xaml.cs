using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Radio.Messaging;
using Radio.Services;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.SocialPlatforms;

public partial class SocialPlatformsView : UserControl
{
    private readonly IPlatformService _service;
    private readonly UserSession _session;
    private readonly DialogHelper _dialogHelper;
    private List<SocialMediaPlatformDto> _allItems = [];

    public SocialPlatformsView(IPlatformService service, UserSession session, DialogHelper dialogHelper)
    {
        InitializeComponent();
        _service = service;
        _session = session;
        _dialogHelper = dialogHelper;
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
        var dialog = new SocialPlatformFormDialog(null, _service, _session);
        if (await _dialogHelper.ShowDialogAsync(dialog) == true)
        {
            MessageService.Current.ShowSuccess(Messages.Actioned("إضافة", "المنصة"));
            await LoadDataAsync();
        }
    }

    private async void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not SocialMediaPlatformDto dto) return;

        var dialog = new SocialPlatformFormDialog(dto, _service, _session);
        if (await _dialogHelper.ShowDialogAsync(dialog) == true)
        {
            MessageService.Current.ShowSuccess(Messages.Updated("المنصة", dto.Name));
            await LoadDataAsync();
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
                MessageService.Current.ShowSuccess(Messages.Deleted("المنصة", dto.Name));
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
