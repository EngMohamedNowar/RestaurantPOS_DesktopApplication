// Views/OpenShiftDialog.xaml.cs
using System.Windows;

namespace PizzaPOS.Views
{
    public partial class OpenShiftDialog : Window
    {
        public double OpeningCash { get; private set; }

        public OpenShiftDialog()
        {
            InitializeComponent();
            CashBox.GotFocus += (_, _) => CashBox.SelectAll();
        }

        void Confirm_Click(object s, RoutedEventArgs e)
        {
            OpeningCash = double.TryParse(CashBox.Text, out var v) ? v : 0;
            DialogResult = true;
        }

        void Cancel_Click(object s, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}