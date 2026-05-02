using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace Radio.Views.Programs
{
    /// <summary>
    /// شاشة إدارة البرامج — تعرض قائمة البرامج النشطة مع إمكانية الإضافة والتعديل والحذف والبحث.
    /// </summary>
    public partial class ProgramsView : UserControl
    {
        private readonly IProgramService _programService;
        private readonly UserSession _session;
        private List<ProgramDto> _allPrograms = [];

        public ProgramsView(IProgramService programService, UserSession session)
        {
            InitializeComponent();
            _programService = programService;
            _session = session;

            BtnAddProgram.Visibility = _session.HasPermission(AppPermissions.ProgramManage)
                ? Visibility.Visible
                : Visibility.Collapsed;

            // ✅ Loaded بدلاً من Fire-and-Forget
            Loaded += async (_, _) => await LoadDataAsync();
        }

        #region Data Loading

        /// <summary>
        /// تحميل جميع البرامج النشطة وربطها بالـ DataGrid.
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                _allPrograms = (await _programService.GetAllActiveAsync()).ToList();
                DgPrograms.ItemsSource = _allPrograms;
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل البرامج.");
            }
        }

        #endregion

        #region Search

        /// <summary>
        /// البحث في قائمة البرامج حسب اسم البرنامج.
        /// </summary>
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            string keyword = textBox.Text.Trim();

            // ✅ StringComparison بدلاً من ToLower()
            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allPrograms
                : _allPrograms.Where(p =>
                    p.ProgramName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            DgPrograms.ItemsSource = filtered.ToList();
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// فتح نافذة إضافة برنامج جديد.
        /// </summary>
        private async void BtnAddProgram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var view = new ProgramFormControl(null, _programService, _session);
                var result = await DialogHost.Show(view);
                if (result is true)
                    await LoadDataAsync();
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء فتح نافذة إضافة البرنامج.");
            }
        }

        /// <summary>
        /// فتح نافذة تعديل برنامج موجود.
        /// </summary>
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ProgramDto prog)
                return;

            try
            {
                var view = new ProgramFormControl(prog, _programService, _session);
                var result = await DialogHost.Show(view);
                if (result is true)
                    await LoadDataAsync();
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء فتح نافذة تعديل البرنامج.");
            }
        }

        /// <summary>
        /// حذف برنامج بعد تأكيد المستخدم.
        /// </summary>
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ProgramDto prog)
                return;

            bool isConfirmed = await MessageService.Current.ShowConfirmationAsync(
                $"هل أنت متأكد من حذف البرنامج: {prog.ProgramName}؟\nلا يمكن التراجع عن هذا الإجراء.",
                "تأكيد الحذف");

            if (!isConfirmed)
                return;

            try
            {
                var result = await _programService.SoftDeleteAsync(prog.ProgramId, _session);

                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    MessageService.Current.ShowSuccess($"تم حذف البرنامج «{prog.ProgramName}» بنجاح.");
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت عملية الحذف.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حذف البرنامج.");
            }
        }

        #endregion
    }
}