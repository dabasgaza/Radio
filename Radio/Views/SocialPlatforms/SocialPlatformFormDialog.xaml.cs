using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;

namespace Radio.Views.SocialPlatforms;

public partial class SocialPlatformFormDialog
{
    private readonly IPlatformService _service;
    private readonly UserSession _session;
    private readonly SocialMediaPlatformDto? _existingPlatform;

    public SocialPlatformFormDialog(SocialMediaPlatformDto? platform, IPlatformService service, UserSession session)
    {
        InitializeComponent();
        _existingPlatform = platform;
        _service = service;
        _session = session;

        IsWindowDraggable = true;

        TxtTitle.Text = _existingPlatform is not null ? "تعديل بيانات المنصة" : "إضافة منصة جديدة";

        if (_existingPlatform is not null)
        {
            TxtName.Text = _existingPlatform.Name;
            TxtIcon.Text = _existingPlatform.Icon ?? string.Empty;
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageService.Current.ShowWarning("يرجى إدخال اسم المنصة.");
            return;
        }

        var dto = new SocialMediaPlatformDto(
            _existingPlatform?.SocialMediaPlatformId ?? 0,
            TxtName.Text.Trim(),
            string.IsNullOrWhiteSpace(TxtIcon.Text) ? null : TxtIcon.Text.Trim());

        try
        {
            BtnSave.IsEnabled = false;

            Result result;
            if (_existingPlatform is null)
                result = await _service.CreateAsync(dto, _session);
            else
                result = await _service.UpdateAsync(dto, _session);

            if (result.IsSuccess)
            {
                MessageService.Current.ShowSuccess(
                    _existingPlatform is null
                        ? "تمت إضافة المنصة بنجاح."
                        : "تم تحديث المنصة بنجاح.");

                DialogResult = true;
            }
            else
            {
                MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت العملية.");
            }
        }
        catch (ConcurrencyException)
        {
            MessageService.Current.ShowWarning("حدث تعارض في البيانات. يرجى إغلاق النافذة وإعادة المحاولة.");
        }
        catch (Exception)
        {
            MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حفظ البيانات.");
        }
        finally
        {
            BtnSave.IsEnabled = true;
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
