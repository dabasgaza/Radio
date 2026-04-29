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
    /// شاشة إدارة التغطيات الميدانية — تعرض قائمة التغطيات مع إمكانية
    /// الإضافة والتعديل والحذف والبحث والتصفية حسب الموقع والتاريخ.
    /// </summary>
    public partial class CoverageView : UserControl
    {
        private readonly ICoverageService _coverageService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;
        private List<CoverageDto> _allCoverages = [];

        public CoverageView(ICoverageService coverageService, UserSession session, IServiceProvider serviceProvider)
        {
            _coverageService = coverageService;
            _session = session;
            _serviceProvider = serviceProvider;

            InitializeComponent();

            BtnAdd.Visibility = _session.HasPermission(AppPermissions.CoordinationManage)
                ? Visibility.Visible
                : Visibility.Collapsed;

            Loaded += async (_, _) => await LoadDataAsync();
        }

        #region Data Loading

        /// <summary>
        /// تحميل جميع التغطيات وربطها بالـ DataGrid.
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                _allCoverages = (await _coverageService.GetAllAsync()).ToList();
                ApplyFilters();
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل التغطيات.");
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// فتح نافذة إضافة تغطية جديدة.
        /// </summary>
        private async void BtnAddCoverage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CoverageFormDialog(
                _coverageService,
                _serviceProvider.GetRequiredService<ICorrespondentService>(),
                _serviceProvider.GetRequiredService<IGuestService>(),
                _session);

            if (dialog.ShowDialog() == true)
                await LoadDataAsync();
        }

        /// <summary>
        /// فتح نافذة تعديل تغطية موجودة.
        /// </summary>
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not CoverageDto dto)
                return;

            var dialog = new CoverageFormDialog(
                _coverageService,
                _serviceProvider.GetRequiredService<ICorrespondentService>(),
                _serviceProvider.GetRequiredService<IGuestService>(),
                _session, dto);

            if (dialog.ShowDialog() == true)
                await LoadDataAsync();
        }

        /// <summary>
        /// حذف تغطية بعد تأكيد المستخدم.
        /// </summary>
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not CoverageDto dto)
                return;

            bool isConfirmed = await MessageService.Current.ShowConfirmationAsync(
                $"هل أنت متأكد من حذف التغطية الخاصة بـ: {dto.CorrespondentName}؟",
                "تأكيد الحذف");

            if (!isConfirmed)
                return;

            try
            {
                var result = await _coverageService.DeleteAsync(dto.CoverageId, _session);
                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    MessageService.Current.ShowSuccess($"تم حذف تغطية «{dto.CorrespondentName}» بنجاح.");
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشل الحذف.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حذف التغطية.");
            }
        }

        #endregion

        #region Search & Filtering

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CboLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DpFromDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DpToDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// مسح جميع الفلاتر وإعادة عرض البيانات كاملة.
        /// </summary>
        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Clear();
            CboLocation.SelectedIndex = -1;
            DpFromDate.SelectedDate = null;
            DpToDate.SelectedDate = null;
        }

        /// <summary>
        /// تطبيق جميع الفلاتر المفعّلة على البيانات المعروضة.
        /// </summary>
        private void ApplyFilters()
        {
            if (_allCoverages.Count == 0)
            {
                DgCoverages.ItemsSource = _allCoverages;
                return;
            }

            string keyword = TxtSearch.Text.Trim();
            var filtered = _allCoverages.AsEnumerable();

            // ✅ بحث نصي — اسم المراسل أو الموضوع أو الموقع
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(c =>
                    (c.CorrespondentName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Topic?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Location?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // ✅ تصفية حسب الموقع
            if (CboLocation.SelectedItem is string selectedLocation
                && !string.IsNullOrWhiteSpace(selectedLocation))
            {
                filtered = filtered.Where(c =>
                    c.Location?.Equals(selectedLocation, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            // ✅ تصفية حسب نطاق تواريخ — من/إلى (DpFromDate و DpToDate)
            if (DpFromDate.SelectedDate is DateTime fromDate)
            {
                filtered = filtered.Where(c =>
                    c.ScheduledTime >= fromDate);
            }

            if (DpToDate.SelectedDate is DateTime toDate)
            {
                filtered = filtered.Where(c =>
                    c.ScheduledTime <= toDate.AddDays(1).AddTicks(-1));
            }

            DgCoverages.ItemsSource = filtered.ToList();
        }

        #endregion
    }
}