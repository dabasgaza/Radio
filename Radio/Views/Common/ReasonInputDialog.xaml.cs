using DataAccess.Services.Messaging;
using System.Windows;

namespace Radio.Views.Common;

public partial class ReasonInputDialog
{
    public string? Reason { get; private set; }

    public ReasonInputDialog(string title, string prompt)
    {
        InitializeComponent();
        Title = title;
        TxtPrompt.Text = prompt;
        BtnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(TxtReason.Text))
            {
                MessageService.Current.ShowWarning("يجب إدخال السبب.");
                return;
            }
            Reason = TxtReason.Text.Trim();
            DialogResult = true;
        };
    }
}
