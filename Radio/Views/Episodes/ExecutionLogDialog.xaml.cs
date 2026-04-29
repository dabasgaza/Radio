using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Text.RegularExpressions;
using System.Windows;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// نافذة تسجيل تنفيذ حلقة — تجمع بيانات المدة والملاحظات والمشاكل وتحفظها عبر ExecutionService.
    /// </summary>
    public partial class ExecutionLogDialog
    {
        // ✅ RegexOptions.Compiled — محسّن مرة واحدة، أفضل أداء
        private static readonly Regex NumericOnlyRegex = new(@"^[0-9.]$", RegexOptions.Compiled);

        private readonly int _episodeId;
        private readonly IExecutionService _executionService;
        private readonly UserSession _session;

        public ExecutionLogDialog(int episodeId, IExecutionService executionService, UserSession session)
        {
            InitializeComponent();
            _episodeId = episodeId;
            _executionService = executionService;
            _session = session;

            IsWindowDraggable = true;

            // ✅ Regex محسّن مسبقاً — لا يُنشئ كائن جديد عند كل ضغطة
            TxtDuration.PreviewTextInput += (s, e) =>
            {
                e.Handled = !NumericOnlyRegex.IsMatch(e.Text);
            };
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtDuration.Text, out int duration))
            {
                MessageService.Current.ShowWarning("يرجى إدخال مدة الحلقة بشكل صحيح (أرقام فقط).");
                return;
            }

            var log = new ExecutionLogDto
            {
                EpisodeId = _episodeId,
                DurationMinutes = duration,
                ExecutionNotes = TxtNotes.Text.Trim(),
                IssuesEncountered = TxtIssues.Text.Trim()
            };

            try
            {
                BtnSave.IsEnabled = false;

                var result = await _executionService.LogExecutionAsync(log, _session);

                if (result.IsSuccess)
                {
                    MessageService.Current.ShowSuccess("تم تسجيل تنفيذ الحلقة بنجاح.");
                    DialogResult = true;   // ✅ يُغلق النافذة تلقائياً — لا حاجة لـ Close()
                }
                else
                {
                    MessageService.Current.ShowError(result.ErrorMessage ?? "فشلت عملية التسجيل.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تسجيل التنفيذ.");
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

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

    }
}