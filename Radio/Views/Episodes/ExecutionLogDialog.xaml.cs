using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Text.RegularExpressions;
using System.Windows;
using Radio.Messaging;


namespace Radio.Views.Episodes
{
    /// <summary>
    /// نافذة تسجيل/تعديل تنفيذ حلقة — تدعم وضعين:
    ///   1. إنشاء جديد: تُنشئ سجل تنفيذ وتحوّل الحلقة إلى "منفّذة"
    ///   2. تعديل موجود: تُحدّث بيانات السجل دون تغيير حالة الحلقة
    /// </summary>
    public partial class ExecutionLogDialog
    {
        // ✅ RegexOptions.Compiled — محسّن مرة واحدة، أفضل أداء
        private static readonly Regex NumericOnlyRegex = new(@"^[0-9]$", RegexOptions.Compiled);
        private readonly int _episodeId;
        private readonly IExecutionService _executionService;
        private readonly UserSession _session;

        /// <summary>
        /// معرّف السجل الموجود — null يعني وضع إنشاء جديد
        /// </summary>
        private readonly int? _existingLogId;

        /// <summary>
        /// هل نحن في وضع التعديل؟
        /// </summary>
        private bool IsEditMode => _existingLogId.HasValue;

        /// <summary>
        /// وضع إنشاء جديد — يُنشئ سجل تنفيذ ويحوّل الحلقة إلى "منفّذة"
        /// </summary>
        public ExecutionLogDialog(int episodeId, IExecutionService executionService, UserSession session)
            : this(episodeId, executionService, session, null)
        {
        }

        /// <summary>
        /// وضع التعديل — يُحدّث سجل تنفيذ موجود دون تغيير حالة الحلقة
        /// إذا مُرّر existingLog = null يعمل كوضع إنشاء جديد
        /// </summary>
        public ExecutionLogDialog(int episodeId, IExecutionService executionService, UserSession session,
            ExecutionLogDto? existingLog)
        {
            InitializeComponent();
            _episodeId = episodeId;
            _executionService = executionService;
            _session = session;
            _existingLogId = existingLog?.ExecutionLogId;

            IsWindowDraggable = true;

            // ✅ Regex محسّن مسبقاً — لا يُنشئ كائن جديد عند كل ضغطة
            TxtDuration.PreviewTextInput += (s, e) =>
            {
                e.Handled = !NumericOnlyRegex.IsMatch(e.Text);
            };

            if (existingLog is not null)
            {
                PopulateFields(existingLog);
                SwitchToEditMode();
            }

        }


        /// <summary>
        /// تعبئة حقول النموذج من بيانات السجل الموجود
        /// </summary>
        private void PopulateFields(ExecutionLogDto log)
        {
            TxtDuration.Text = log.DurationMinutes > 0 ? log.DurationMinutes.ToString() : string.Empty;
            TxtNotes.Text = log.ExecutionNotes ?? string.Empty;
            TxtIssues.Text = log.IssuesEncountered ?? string.Empty;
        }

        /// <summary>
        /// تحويل مظهر النافذة إلى وضع التعديل:
        /// - تغيير العنوان من "إتمام تنفيذ" إلى "تعديل سجل التنفيذ"
        /// - تغيير نص الزر من "تأكيد التنفيذ" إلى "حفظ التعديلات"
        /// - إخفاء رسالة "سيتم تحويل حالة الحلقة" لأن التعديل لا يغيّر الحالة
        /// </summary>
        private void SwitchToEditMode()
        {
            Title = "تعديل سجل التنفيذ";
            BtnSave.Content = "حفظ التعديلات";
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtDuration.Text, out int duration) || duration <= 0 || duration > 1440)
            {
                MessageService.Current.ShowWarning("يرجى إدخال مدة صحيحة بين 1 و 1440 دقيقة (أرقام صحيحة فقط).");
                return;
            }
            try
            {
                BtnSave.IsEnabled = false;

                if (IsEditMode)
                {
                    // ═══ وضع التعديل: تحديث السجل الموجود دون تغيير حالة الحلقة ═══
                    var dto = new ExecutionLogDto
                    {
                        ExecutionLogId = _existingLogId!.Value,
                        EpisodeId = _episodeId,
                        DurationMinutes = duration,
                        ExecutionNotes = TxtNotes.Text.Trim(),
                        IssuesEncountered = TxtIssues.Text.Trim()
                    };

                    var result = await _executionService.UpdateExecutionLogAsync(dto, _session);

                    if (result.IsSuccess)
                    {
                        MessageService.Current.ShowSuccess("تم تعديل سجل التنفيذ بنجاح.");
                        DialogResult = true;
                    }
                    else
                    {
                        MessageService.Current.ShowError(result.ErrorMessage ?? "فشل تعديل السجل.");
                    }
                }
                else
                {
                    // ═══ وضع الإنشاء: تسجيل تنفيذ جديد وتحويل الحلقة إلى "منفّذة" ═══
                    var log = new ExecutionLogDto
                    {
                        EpisodeId = _episodeId,
                        DurationMinutes = duration,
                        ExecutionNotes = TxtNotes.Text.Trim(),
                        IssuesEncountered = TxtIssues.Text.Trim()
                    };

                    var result = await _executionService.LogExecutionAsync(log, _session);

                    if (result.IsSuccess)
                    {
                        MessageService.Current.ShowSuccess("تم تسجيل تنفيذ الحلقة بنجاح.");
                        DialogResult = true;
                    }
                    else
                    {
                        MessageService.Current.ShowError(result.ErrorMessage ?? "فشلت عملية التسجيل.");
                    }
                }
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

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

    }
}