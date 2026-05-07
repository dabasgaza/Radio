using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Employees
{
    /// <summary>
    /// شاشة إدارة الموظفين — عرض وإضافة وتعديل وحذف الموظفين.
    /// </summary>
    public partial class EmployeesView : UserControl
    {
        private readonly IEmployeeService _employeeService;
        private readonly UserSession _session;
        private List<EmployeeDto> _allEmployees = [];

        public EmployeesView(IEmployeeService employeeService, UserSession session)
        {
            InitializeComponent();
            _employeeService = employeeService;
            _session = session;

            Loaded += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _allEmployees = await _employeeService.GetAllActiveAsync();
                DgEmployees.ItemsSource = _allEmployees;
                UpdateStats();
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ أثناء تحميل الموظفين.");
            }
        }

        private void UpdateStats()
        {
            TxtTotalEmployees.Text = _allEmployees.Count.ToString();
            TxtActiveEmployees.Text = _allEmployees.Count.ToString(); // Assuming GetAllActiveAsync returns active ones
            TxtOccupiedRoles.Text = _allEmployees.Select(e => e.StaffRoleId).Distinct().Count().ToString();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            var keyword = textBox.Text.Trim();

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allEmployees
                : _allEmployees.Where(emp =>
                    emp.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (emp.StaffRoleName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));

            DgEmployees.ItemsSource = filtered.ToList();
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EmployeeFormDialog(_employeeService, _session, employeeId: 0);
            if (dialog.ShowDialog() == true)
                await LoadDataAsync();
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not EmployeeDto emp) return;

            var dialog = new EmployeeFormDialog(_employeeService, _session, emp.EmployeeId);
            if (dialog.ShowDialog() == true)
                await LoadDataAsync();
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not EmployeeDto emp) return;

            var confirmed = await MessageService.Current.ShowConfirmationAsync(
                $"هل أنت متأكد من حذف الموظف: {emp.FullName}؟", "تأكيد الحذف");

            if (!confirmed) return;

            var result = await _employeeService.SoftDeleteAsync(emp.EmployeeId, _session);
            if (result.IsSuccess)
            {
                await LoadDataAsync();
                MessageService.Current.ShowSuccess("تم حذف الموظف بنجاح.");
            }
            else
            {
                MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشل الحذف.");
            }
        }
    }
}