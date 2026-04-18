using BroadcastWorkflow.Services;
using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Correspondents
{
    /// <summary>
    /// Interaction logic for CoveragesView.xaml
    /// </summary>
    public partial class CoverageView : UserControl
    {
        private readonly ICoverageService _coverageService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;

        public CoverageView(ICoverageService coverageService, UserSession session, IServiceProvider serviceProvider)
        {
            _coverageService = coverageService;
            _session = session;
            _serviceProvider = serviceProvider;

            InitializeComponent();

            BtnAdd.Visibility = _session.HasPermission(AppPermissions.CoordinationManage)
                            ? Visibility.Visible : Visibility.Collapsed;

            _ = LoadDataAsync();

        }

        private async Task LoadDataAsync()
        {
            try
            {
                var data = await _coverageService.GetAllAsync();
                DgCoverages.ItemsSource = data;
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ في تحميل التغطيات: {ex.Message}");
            }
        }

        private async void BtnAddCoverage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CoverageFormDialog(
                _coverageService,
                _serviceProvider.GetRequiredService<ICorrespondentService>(),
                _serviceProvider.GetRequiredService<IGuestService>(),
                _session, null);

            if (dialog.ShowDialog() == true)
                await LoadDataAsync();
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CoverageDto dto)
            {
                var dialog = new CoverageFormDialog(
                    _coverageService,
                    _serviceProvider.GetRequiredService<ICorrespondentService>(),
                    _serviceProvider.GetRequiredService<IGuestService>(),
                    _session, dto);

                if (dialog.ShowDialog() == true)
                    await LoadDataAsync();
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CoverageDto dto)
            {
                bool confirmed = await MessageService.Current.ShowConfirmationAsync(
                    $"هل أنت متأكد من حذف التغطية الخاصة بـ: {dto.CorrespondentName}؟", "تأكيد الحذف");

                if (confirmed)
                {
                    try
                    {
                        await _coverageService.DeleteAsync(dto.CoverageId, _session);
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageService.Current.ShowError(ex.Message);
                    }
                }
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void CboLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleDateFilter_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DpFromDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DpToDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
