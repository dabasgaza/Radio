using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;

namespace Radio.Views.Employees
{
    /// <summary>
    /// نافذة إضافة/تعديل موظف — تتولى جمع البيانات والتحقق منها ثم إرسالها لـ EmployeeService.
    /// </summary>
    public partial class EmployeeFormDialog
    {
        private readonly IEmployeeService _employeeService;
        private readonly UserSession _session;
        private readonly int _employeeId;

        public EmployeeFormDialog(IEmployeeService employeeService, UserSession session, int employeeId)
        {
            InitializeComponent();
            _employeeService = employeeService;
            _session = session;
            _employeeId = employeeId;

            IsWindowDraggable = true;
            Title = _employeeId == 0 ? "إضافة موظف جديد" : "تعديل بيانات موظف";

            Loaded += async (_, _) => await LoadRolesAsync();
        }

        private async Task LoadRolesAsync()
        {
            var roles = await _employeeService.GetAllRolesAsync();
            CmbStaffRole.ItemsSource = roles;

            if (_employeeId != 0)
            {
                var employees = await _employeeService.GetAllActiveAsync();
                var emp = employees.FirstOrDefault(e => e.EmployeeId == _employeeId);
                if (emp != null)
                {
                    TxtFullName.Text = emp.FullName;
                    CmbStaffRole.SelectedValue = emp.StaffRoleId;
                    TxtNotes.Text = emp.Notes;
                }
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtFullName.Text))
            {
                MessageService.Current.ShowWarning("الاسم الكامل مطلوب.");
                return;
            }

            var dto = new EmployeeDto(
                _employeeId,
                TxtFullName.Text.Trim(),
                CmbStaffRole.SelectedValue as int?,
                null,
                TxtNotes.Text.Trim());

            try
            {
                BtnSave.IsEnabled = false;

                Result result;
                if (_employeeId == 0)
                    result = await _employeeService.CreateAsync(dto, _session);
                else
                    result = await _employeeService.UpdateAsync(dto, _session);

                if (result.IsSuccess)
                {
                    MessageService.Current.ShowSuccess(
                        _employeeId == 0 ? "تمت إضافة الموظف بنجاح." : "تم تعديل بيانات الموظف بنجاح.");
                    DialogResult = true;
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت العملية.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حفظ بيانات الموظف.");
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}