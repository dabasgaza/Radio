using DataAccess.Common;
using DataAccess.Services;
using Domain.Models;
using Radio.Messaging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Users
{
    public partial class AuditLogsView : UserControl
    {
        private readonly IAuditLogService _auditLogService;
        private readonly UserSession _session;

        public class ChangeComparisonItem
        {
            public string FieldName { get; set; } = string.Empty;
            public string OldValue { get; set; } = string.Empty;
            public string NewValue { get; set; } = string.Empty;
        }

        public AuditLogsView(IAuditLogService auditLogService, UserSession session)
        {
            InitializeComponent();
            _auditLogService = auditLogService;
            _session = session;
            
            Loaded += AuditLogsView_Loaded;
        }

        private async void AuditLogsView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
            await LoadLogsAsync();
        }

        private async Task LoadUsersAsync()
        {
            var result = await _auditLogService.GetAuditUsersAsync();
            if (result.IsSuccess && result.Value != null)
            {
                var usersList = new List<User> { new User { UserId = 0, FullName = "الكل" } };
                usersList.AddRange(result.Value);
                ComboUsers.ItemsSource = usersList;
                ComboUsers.SelectedIndex = 0;
            }
        }

        private async Task LoadLogsAsync()
        {
            int? userId = null;
            if (ComboUsers.SelectedValue is int id && id > 0)
            {
                userId = id;
            }

            string? action = null;
            if (ComboActions.SelectedItem is ComboBoxItem item && !string.IsNullOrEmpty(item.Tag?.ToString()))
            {
                action = item.Tag.ToString();
            }

            DateTime? fromDate = DpFrom.SelectedDate;
            DateTime? toDate = DpTo.SelectedDate;

            var result = await _auditLogService.GetFilteredAuditLogsAsync(
                tableName: null,
                userId: userId,
                action: action,
                fromDate: fromDate,
                toDate: toDate);

            if (result.IsSuccess)
            {
                GridLogs.ItemsSource = result.Value;
            }
            else
            {
                NotificationManager.Show(NotificationType.Error, "خطأ", $"فشل تحميل العمليات: {result.ErrorMessage}");
            }
        }

        private async void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            await LoadLogsAsync();
        }

        private async void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            ComboUsers.SelectedIndex = 0;
            ComboActions.SelectedIndex = 0;
            DpFrom.SelectedDate = null;
            DpTo.SelectedDate = null;
            await LoadLogsAsync();
        }

        private void GridLogs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridLogs.SelectedItem is AuditLogDto log)
            {
                TxtPlaceholder.Visibility = Visibility.Collapsed;
                GridDetailsContent.Visibility = Visibility.Visible;

                // Bind Meta Info
                TxtDetailUser.Text = $"المستخدم: {log.UserFullName} ({log.Username})";
                TxtDetailTable.Text = $"الجدول المتأثر: {log.TableName} (معرف: {log.RecordId})";
                TxtDetailAction.Text = $"نوع الإجراء: {TranslateAction(log.Action)}";
                TxtDetailDate.Text = $"التاريخ: {log.ChangedAt.ToLocalTime():dd-MM-yyyy HH:mm:ss}";

                // Bind Changes
                var comparisonList = ParseChanges(log.OldValues, log.NewValues, log.Action);
                LvChanges.ItemsSource = comparisonList;
            }
            else
            {
                TxtPlaceholder.Visibility = Visibility.Visible;
                GridDetailsContent.Visibility = Visibility.Collapsed;
            }
        }

        private string TranslateAction(string action)
        {
            return action switch
            {
                "ADDED" => "إضافة",
                "MODIFIED" => "تعديل",
                "SOFT_DELETED" => "حذف منطقي",
                "DELETED" => "حذف نهائي",
                _ => action
            };
        }

        private List<ChangeComparisonItem> ParseChanges(string? oldJson, string? newJson, string action)
        {
            var list = new List<ChangeComparisonItem>();

            try
            {
                var oldDict = !string.IsNullOrEmpty(oldJson) 
                    ? (JsonSerializer.Deserialize<Dictionary<string, object>>(oldJson) ?? new Dictionary<string, object>())
                    : new Dictionary<string, object>();

                var newDict = !string.IsNullOrEmpty(newJson) 
                    ? (JsonSerializer.Deserialize<Dictionary<string, object>>(newJson) ?? new Dictionary<string, object>())
                    : new Dictionary<string, object>();

                if (action == "ADDED")
                {
                    foreach (var kp in newDict)
                    {
                        list.Add(new ChangeComparisonItem
                        {
                            FieldName = kp.Key,
                            OldValue = "(قيمة فارغة)",
                            NewValue = kp.Value?.ToString() ?? "Null"
                        });
                    }
                }
                else if (action == "SOFT_DELETED" || action == "DELETED")
                {
                    var dictToUse = oldDict.Count > 0 ? oldDict : newDict;
                    foreach (var kp in dictToUse)
                    {
                        list.Add(new ChangeComparisonItem
                        {
                            FieldName = kp.Key,
                            OldValue = kp.Value?.ToString() ?? "Null",
                            NewValue = "(محذوف)"
                        });
                    }
                }
                else // MODIFIED
                {
                    // Find keys in either old or new dictionaries
                    var allKeys = new HashSet<string>(oldDict.Keys);
                    foreach (var k in newDict.Keys) allKeys.Add(k);

                    foreach (var key in allKeys)
                    {
                        oldDict.TryGetValue(key, out var oldVal);
                        newDict.TryGetValue(key, out var newVal);

                        string oldStr = oldVal?.ToString() ?? "Null";
                        string newStr = newVal?.ToString() ?? "Null";

                        if (oldStr != newStr)
                        {
                            list.Add(new ChangeComparisonItem
                            {
                                FieldName = key,
                                OldValue = oldStr,
                                NewValue = newStr
                            });
                        }
                    }
                }
            }
            catch
            {
                list.Add(new ChangeComparisonItem
                {
                    FieldName = "خطأ",
                    OldValue = "تعذر تحليل التغييرات",
                    NewValue = "الملف غير صالح"
                });
            }

            return list;
        }
    }
}
