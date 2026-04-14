namespace Radio.Views.Common
{
    /// <summary>
    /// Interaction logic for ConcurrencyDialog.xaml
    /// </summary>
    public partial class ConcurrencyDialog
    {
        private Dictionary<string, object?> databaseValues;

        public ConcurrencyDialog(Dictionary<string, object?> databaseValues)
        {
            InitializeComponent();
            this.databaseValues = databaseValues;
            ItemsDiff.ItemsSource = databaseValues;
        }
    }
}
