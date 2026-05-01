using ClosedXML.Excel;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Reports
{
    /// <summary>
    /// لوحة التقارير — إحصائيات، جدول اليوم، أداء البرامج، الضيوف، الملغاة، وتقرير الفترة.
    /// </summary>
    public partial class ReportsView : UserControl
    {
        private readonly IReportsService _reportsService;

        // نحتفظ بنتائج آخر بحث لاستخدامها في التصدير
        private List<DateRangeEpisodeDto> _lastSearchResults = [];

        public ReportsView(IReportsService reportsService)
        {
            InitializeComponent();
            _reportsService = reportsService;

            DpFrom.SelectedDate = DateTime.Today.AddDays(-6);
            DpTo.SelectedDate   = DateTime.Today;

            Loaded += async (_, _) => await LoadDashboardDataAsync();
        }

        #region Data Loading

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // ── KPIs ──
                var stats = await _reportsService.GetEpisodeStatusStatsAsync();

                int planned   = stats.GetValueOrDefault("Planned",   0);
                int executed  = stats.GetValueOrDefault("Executed",  0);
                int published = stats.GetValueOrDefault("Published", 0);
                int cancelled = stats.GetValueOrDefault("Cancelled", 0);
                int total     = planned + executed + published + cancelled;

                TxtPlannedCount.Text   = planned.ToString();
                TxtExecutedCount.Text  = executed.ToString();
                TxtPublishedCount.Text = published.ToString();
                TxtCancelledCount.Text = cancelled.ToString();

                // نسب مئوية وشرائط التقدم
                if (total > 0)
                {
                    double pPlanned   = planned   * 100.0 / total;
                    double pExecuted  = executed  * 100.0 / total;
                    double pPublished = published * 100.0 / total;
                    double pCancelled = cancelled * 100.0 / total;

                    PbPlanned.Value   = pPlanned;
                    PbExecuted.Value  = pExecuted;
                    PbPublished.Value = pPublished;
                    PbCancelled.Value = pCancelled;

                    TxtPlannedPct.Text   = $"{pPlanned:F0}%";
                    TxtExecutedPct.Text  = $"{pExecuted:F0}%";
                    TxtPublishedPct.Text = $"{pPublished:F0}%";
                    TxtCancelledPct.Text = $"{pCancelled:F0}%";
                }

                // ── جدول اليوم ──
                DgToday.ItemsSource = await _reportsService.GetTodayEpisodesAsync();

                // ── إحصائيات البرامج ──
                DgProgramStats.ItemsSource = await _reportsService.GetMostActiveProgramsAsync();

                // ── الضيوف الأكثر ظهوراً ──
                DgTopGuests.ItemsSource = await _reportsService.GetTopGuestsAsync();

                // ── الحلقات الملغاة ──
                DgCancelled.ItemsSource = await _reportsService.GetCancelledEpisodesAsync();

                // ── تقرير الفترة الافتراضية ──
                await LoadDateRangeReportAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل لوحة التقارير.");
            }
        }

        private async Task LoadDateRangeReportAsync()
        {
            if (DpFrom.SelectedDate is not DateTime from || DpTo.SelectedDate is not DateTime to)
            {
                MessageService.Current.ShowWarning("يرجى تحديد تاريخ البداية والنهاية.");
                return;
            }

            if (from > to)
            {
                MessageService.Current.ShowWarning("تاريخ البداية يجب أن يكون قبل تاريخ النهاية.");
                return;
            }

            try
            {
                BtnSearch.IsEnabled = false;
                BtnExport.IsEnabled = false;

                _lastSearchResults = await _reportsService.GetEpisodesByDateRangeAsync(from, to);
                DgDateRange.ItemsSource = _lastSearchResults;

                var days = (to - from).Days + 1;
                TxtSearchSummary.Text       = $"{_lastSearchResults.Count} حلقة خلال {days} يوم";
                PnlSearchSummary.Visibility = Visibility.Visible;

                // نفعّل زر التصدير فقط إذا في نتائج
                BtnExport.IsEnabled = _lastSearchResults.Count > 0;
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ أثناء تحميل تقرير الفترة الزمنية.");
            }
            finally
            {
                BtnSearch.IsEnabled = true;
            }
        }

        #endregion

        #region Event Handlers

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
            => await LoadDateRangeReportAsync();

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            DpFrom.SelectedDate         = DateTime.Today.AddDays(-6);
            DpTo.SelectedDate           = DateTime.Today;
            DgDateRange.ItemsSource     = null;
            PnlSearchSummary.Visibility = Visibility.Collapsed;
            BtnExport.IsEnabled         = false;
            _lastSearchResults          = [];
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (_lastSearchResults.Count == 0) return;

            var dialog = new SaveFileDialog
            {
                Title            = "حفظ تقرير Excel",
                Filter           = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName         = $"تقرير_الحلقات_{DateTime.Today:yyyy-MM-dd}.xlsx",
                DefaultExt       = ".xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                BtnExport.IsEnabled = false;

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("تقرير الحلقات");

                // ── إعداد الاتجاه RTL ──
                ws.RightToLeft = true;

                // ── العنوان الرئيسي ──
                ws.Cell(1, 1).Value = "تقرير الحلقات الإذاعية";
                ws.Range(1, 1, 1, 6).Merge();
                ws.Cell(1, 1).Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(16)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1A237E"))
                    .Font.SetFontColor(XLColor.White);
                ws.Row(1).Height = 30;

                // ── الفترة الزمنية ──
                ws.Cell(2, 1).Value = $"الفترة: {DpFrom.SelectedDate:yyyy/MM/dd}  —  {DpTo.SelectedDate:yyyy/MM/dd}";
                ws.Range(2, 1, 2, 6).Merge();
                ws.Cell(2, 1).Style
                    .Font.SetFontSize(11)
                    .Font.SetItalic(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#E8EAF6"));
                ws.Row(2).Height = 20;

                // ── رؤوس الأعمدة ──
                var headers = new[] { "التاريخ والوقت", "البرنامج", "عنوان الحلقة", "الضيوف", "الحالة" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style
                        .Font.SetBold(true)
                        .Font.SetFontSize(12)
                        .Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#303F9F"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetOutsideBorderColor(XLColor.FromHtml("#9FA8DA"));
                }
                ws.Row(4).Height = 24;

                // ── البيانات ──
                for (int i = 0; i < _lastSearchResults.Count; i++)
                {
                    var row  = _lastSearchResults[i];
                    int rowN = i + 5;
                    bool isAlt = i % 2 == 1;

                    ws.Cell(rowN, 1).Value = row.ScheduledExecutionTime?.ToString("yyyy/MM/dd  hh:mm tt") ?? "—";
                    ws.Cell(rowN, 2).Value = row.ProgramName;
                    ws.Cell(rowN, 3).Value = row.EpisodeName;
                    ws.Cell(rowN, 4).Value = row.GuestsDisplay;
                    ws.Cell(rowN, 5).Value = row.StatusText;

                    // تلوين خلية الحالة
                    var statusColor = row.StatusText switch
                    {
                        "منفّذة"           => XLColor.FromHtml("#E8F5E9"),
                        "منشورة"           => XLColor.FromHtml("#E0F2F1"),
                        "مجدولة"           => XLColor.FromHtml("#FFF3E0"),
                        "ملغاة"            => XLColor.FromHtml("#FFEBEE"),
                        "منشورة على الموقع" => XLColor.FromHtml("#E3F2FD"),
                        _                  => XLColor.NoColor
                    };
                    ws.Cell(rowN, 5).Style.Fill.SetBackgroundColor(statusColor);
                    ws.Cell(rowN, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    // تلوين الصفوف المتناوبة
                    var rowRange = ws.Range(rowN, 1, rowN, 5);
                    if (isAlt)
                        rowRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F4F6FB"));

                    rowRange.Style
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetOutsideBorderColor(XLColor.FromHtml("#E0E0E0"))
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                    ws.Row(rowN).Height = 20;
                }

                // ── صف الإجمالي ──
                int totalRow = _lastSearchResults.Count + 5;
                ws.Cell(totalRow, 1).Value = $"الإجمالي: {_lastSearchResults.Count} حلقة";
                ws.Range(totalRow, 1, totalRow, 5).Merge();
                ws.Cell(totalRow, 1).Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(11)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#C5CAE9"));
                ws.Row(totalRow).Height = 22;

                // ── ضبط عرض الأعمدة ──
                ws.Column(1).Width = 22;
                ws.Column(2).Width = 25;
                ws.Column(3).Width = 35;
                ws.Column(4).Width = 45;
                ws.Column(5).Width = 18;

                wb.SaveAs(dialog.FileName);
                MessageService.Current.ShowSuccess($"تم تصدير التقرير بنجاح إلى:\n{dialog.FileName}");
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"فشل تصدير الملف: {ex.Message}");
            }
            finally
            {
                BtnExport.IsEnabled = _lastSearchResults.Count > 0;
            }
        }

        #endregion
    }
}
