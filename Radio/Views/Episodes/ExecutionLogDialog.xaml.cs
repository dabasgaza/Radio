using DataAccess.Services;
using DataAccess.Services.Messaging;
using Domain.Models;
using System.Windows;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// Interaction logic for ExecutionLogDialog.xaml
    /// </summary>
    public partial class ExecutionLogDialog
    {
        private readonly int _episodeId;
        private readonly IExecutionService _executionService;
        private readonly UserSession _session;

        public ExecutionLogDialog(int episodeId, IExecutionService executionService, UserSession session)
        {
            InitializeComponent();
            _episodeId = episodeId;
            _executionService = executionService;
            _session = session;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. التحقق من إدخال مدة الحلقة بشكل صحيح
            if (!int.TryParse(TxtDuration.Text, out int duration))
            {
                MessageService.Current.ShowWarning("يرجى إدخال مدة الحلقة بشكل صحيح (أرقام فقط)");
                return;
            }

            // 2. تجهيز كائن السجل
            var log = new ExecutionLog
            {
                EpisodeId = _episodeId,
                DurationMinutes = duration,
                ExecutionNotes = TxtNotes.Text.Trim(),
                IssuesEncountered = TxtIssues.Text.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                BtnSave.IsEnabled = false; // تعطيل الزر لتجنب النقرات المتعددة

                // 3. استدعاء خدمة التنفيذ (التي تقوم بحفظ السجل وتحديث حالة الحلقة في Transaction واحد)
                await _executionService.LogExecutionAsync(log, _session);

                this.DialogResult = true; // إغلاق النافذة بنجاح
                this.Close();
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError("حدث خطأ أثناء تسجيل التنفيذ: " + ex.Message);
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

    }
}
