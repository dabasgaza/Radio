using DataAccess.Services.Messaging;
using System;
using System.Diagnostics;
using System.Windows;

namespace Radio.Forms
{
    public partial class SystemErrorWindow : MahApps.Metro.Controls.MetroWindow
    {
        private readonly Exception _exception;

        public SystemErrorWindow(Exception exception)
        {
            InitializeComponent();
            _exception = exception;
            
            // Format exception details
            TxtExceptionDetails.Text = exception != null 
                ? $"{exception.GetType().FullName}: {exception.Message}\n\nStack Trace:\n{exception.StackTrace}\n\nInner Exception:\n{exception.InnerException?.Message}\n{exception.InnerException?.StackTrace}"
                : "لا تتوفر تفاصيل إضافية.";
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(TxtExceptionDetails.Text);
                // ✨ استخدام نظام الإشعارات المركزي بدلاً من MessageBox التقليدية
                MessageService.Current.ShowSuccess("تم نسخ تفاصيل الخطأ إلى الحافظة بنجاح.", "تم النسخ");
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"فشل النسخ إلى الحافظة: {ex.Message}", "خطأ");
            }
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var processPath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(processPath))
                {
                    Process.Start(new ProcessStartInfo(processPath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"فشل إعادة تشغيل التطبيق: {ex.Message}", "خطأ في إعادة التشغيل");
            }
            finally
            {
                Application.Current.Shutdown();
            }
        }
    }
}
