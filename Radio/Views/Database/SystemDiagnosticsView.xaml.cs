using DataAccess.Common;
using DataAccess.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Database
{
    public partial class SystemDiagnosticsView : UserControl
    {
        private readonly ISystemDiagnosticsService _diagnosticsService;
        private readonly UserSession _session;

        public SystemDiagnosticsView(ISystemDiagnosticsService diagnosticsService, UserSession session)
        {
            InitializeComponent();
            _diagnosticsService = diagnosticsService;
            _session = session;

            Loaded += SystemDiagnosticsView_Loaded;
        }

        private async void SystemDiagnosticsView_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshDataAsync();
        }

        private async Task RefreshDataAsync()
        {
            // 1. Get Summary
            var summaryResult = await _diagnosticsService.GetSummaryAsync();
            if (summaryResult.IsSuccess && summaryResult.Value != null)
            {
                var val = summaryResult.Value;
                TxtTotalLogs.Text = val.TotalLogs.ToString();
                TxtTotalErrors.Text = val.TotalErrors.ToString();
                TxtTotalQueries.Text = val.TotalQueries.ToString();
                TxtSlowQueries.Text = val.SlowQueriesCount.ToString();

                TxtAvgQueryTime.Text = $"{val.AverageQueryTimeMs:F1} ms";
                ProgressQueryTime.Value = Math.Min(val.AverageQueryTimeMs, 100);

                double slowRatio = val.TotalQueries > 0 ? (double)val.SlowQueriesCount / val.TotalQueries * 100 : 0;
                TxtSlowQueryRatio.Text = $"{slowRatio:F1}%";
                ProgressSlowRatio.Value = Math.Min(slowRatio, 100);

                double stability = val.TotalLogs > 0 ? (double)(val.TotalLogs - val.TotalErrors) / val.TotalLogs * 100 : 100;
                TxtStabilityRate.Text = $"{stability:F1}%";
                ProgressStability.Value = Math.Min(stability, 100);
            }

            // 2. Load Events Log
            await LoadLogsAsync();

            // 3. Load SQL Performance
            var sqlResult = await _diagnosticsService.GetSqlPerformanceLogsAsync();
            if (sqlResult.IsSuccess)
            {
                GridSqlPerformance.ItemsSource = sqlResult.Value;
            }
        }

        private async Task LoadLogsAsync()
        {
            string? level = null;
            if (ComboLogLevels.SelectedItem is ComboBoxItem item && !string.IsNullOrEmpty(item.Tag?.ToString()))
            {
                level = item.Tag.ToString();
            }

            string? search = TxtSearchLogs.Text;

            var result = await _diagnosticsService.GetLogsAsync(level, search);
            if (result.IsSuccess)
            {
                GridLogs.ItemsSource = result.Value;
            }
        }

        private async void ComboLogLevels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridLogs != null) // Avoid calling before InitializeComponent completes
            {
                await LoadLogsAsync();
            }
        }

        private async void TxtSearchLogs_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadLogsAsync();
        }

        private async void BtnResetLogsFilters_Click(object sender, RoutedEventArgs e)
        {
            ComboLogLevels.SelectedIndex = 0;
            TxtSearchLogs.Text = string.Empty;
            await LoadLogsAsync();
        }

        private void GridLogs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridLogs.SelectedItem is DiagnosticLogDto log)
            {
                TxtLogPlaceholder.Visibility = Visibility.Collapsed;
                PanelLogDetail.Visibility = Visibility.Visible;

                TxtDetailContext.Text = log.SourceContext ?? "غير محدد";
                TxtDetailMessage.Text = log.Message;

                if (!string.IsNullOrEmpty(log.Sql))
                {
                    PanelSqlSection.Visibility = Visibility.Visible;
                    TxtDetailSql.Text = log.Sql;
                    TxtDetailDuration.Text = $"{log.DurationMs:F1} ms";
                }
                else
                {
                    PanelSqlSection.Visibility = Visibility.Collapsed;
                }

                if (!string.IsNullOrEmpty(log.Exception))
                {
                    PanelExceptionSection.Visibility = Visibility.Visible;
                    TxtDetailException.Text = log.Exception;
                }
                else
                {
                    PanelExceptionSection.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                TxtLogPlaceholder.Visibility = Visibility.Visible;
                PanelLogDetail.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnRefreshAll_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDataAsync();
        }
    }
}
