using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Admin;

public partial class SocialPlatformsView : UserControl
{
    private readonly IPlatformService _service;
    private readonly UserSession _session;
    private List<SocialMediaPlatformDto> _allItems = [];
    private bool _isEditing;
    private int _editingId;

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

    private void ShowForm(string title, SocialMediaPlatformDto? dto = null)
    {
        TxtFormTitle.Text = title;
        TxtName.Text = dto?.Name ?? string.Empty;
        TxtIcon.Text = dto?.Icon ?? string.Empty;
        _isEditing = dto != null;
        _editingId = dto?.SocialMediaPlatformId ?? 0;
        FormBorder.Visibility = Visibility.Visible;
        TxtName.Focus();
    }

    private void HideForm()
    {
        FormBorder.Visibility = Visibility.Collapsed;
        TxtName.Clear();
        TxtIcon.Clear();
        _isEditing = false;
        _editingId = 0;
    }

    private async void BtnAddNew_Click(object sender, RoutedEventArgs e)
    {
        ShowForm("إضافة منصة جديدة");
    }

    private async void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not SocialMediaPlatformDto dto) return;
        ShowForm("تعديل بيانات المنصة", dto);
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageService.Current.ShowWarning("يرجى إدخال اسم المنصة.");
            return;
        }

        var dto = new SocialMediaPlatformDto(
            _isEditing ? _editingId : 0,
            TxtName.Text.Trim(),
            string.IsNullOrWhiteSpace(TxtIcon.Text) ? null : TxtIcon.Text.Trim());

        if (_isEditing)
        {
            var response = await _service.UpdateAsync(dto, _session);
            if (response.IsSuccess)
            {
                HideForm();
                await LoadDataAsync();
                MessageService.Current.ShowSuccess("تم تحديث المنصة بنجاح.");
            }
            else
            {
                MessageService.Current.ShowError(response.ErrorMessage);
            }
        }
        else
        {
            var response = await _service.CreateAsync(dto, _session);
            if (response.IsSuccess)
            {
                HideForm();
                await LoadDataAsync();
                MessageService.Current.ShowSuccess("تم إضافة المنصة بنجاح.");
            }
            else
            {
                MessageService.Current.ShowError(response.ErrorMessage);
            }
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        HideForm();
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not SocialMediaPlatformDto dto) return;

        var confirm = await MessageService.Current.ShowConfirmationAsync($"هل أنت متأكد من حذف المنصة \"{dto.Name}\"?",
            "تأكيد");

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
                MessageService.Current.ShowError(response.ErrorMessage);
            }
        }
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplySearchFilter();
    }
}