using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Radio.Messaging;
using Radio.Services;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Programs
{
    /// <summary>
    /// شاشة إدارة البرامج — تعرض قائمة البرامج النشطة مع إمكانية الإضافة والتعديل والحذف والبحث.
    /// </summary>
    public partial class ProgramsView : UserControl
    {
        private readonly IProgramService _programService;
        private readonly UserSession _session;
        private readonly DialogHelper _dialogHelper;
        private List<ProgramDto> _allPrograms = [];
        private DataGrid? _dgPrograms;
        private DataGrid DgPrograms => _dgPrograms ??= FindByTag<DataGrid>(SkeletonGrid, "DgPrograms");

        private static T? FindByTag<T>(DependencyObject parent, object tag) where T : DependencyObject
        {
            if (parent is T t && parent is FrameworkElement fe && fe.Tag?.Equals(tag) == true)
                return t;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var result = FindByTag<T>(System.Windows.Media.VisualTreeHelper.GetChild(parent, i), tag);
                if (result is not null) return result;
            }
            return null;
        }

        public ProgramsView(IProgramService programService, UserSession session, DialogHelper dialogHelper)
        {
            InitializeComponent();
            _programService = programService;
            _session = session;
            _dialogHelper = dialogHelper;

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
                SkeletonGrid.IsLoading = true;
                _allPrograms = (await _programService.GetAllActiveAsync()).ToList();
                DgPrograms.ItemsSource = _allPrograms;

                // تحديث كروت الإحصائيات
                TxtTotal.Text = _allPrograms.Count.ToString();
                TxtCategories.Text = _allPrograms.Select(p => p.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().Count().ToString();
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل البرامج.");
            }
            finally
            {
                SkeletonGrid.IsLoading = false;
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
            var dialog = new ProgramFormControl(null, _programService, _session);
            if (await _dialogHelper.ShowDialogAsync(dialog) == true)
            {
                MessageService.Current.ShowSuccess(Messages.Actioned("إضافة", "البرنامج"));
                await LoadDataAsync();
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ProgramDto prog)
                return;

            var dialog = new ProgramFormControl(prog, _programService, _session);
            if (await _dialogHelper.ShowDialogAsync(dialog) == true)
            {
                MessageService.Current.ShowSuccess(Messages.Updated("البرنامج", prog.ProgramName));
                await LoadDataAsync();
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
                    MessageService.Current.ShowSuccess(Messages.Deleted("البرنامج", prog.ProgramName));
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت عملية الحذف.");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حذف البرنامج.");
            }
        }

        #endregion
    }
}