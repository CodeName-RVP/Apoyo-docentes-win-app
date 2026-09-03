using System.Windows;

namespace AppParaUniversidad.Views;

public partial class ConfirmationDialog : Window
{
    public bool Confirmed { get; private set; }

    public ConfirmationDialog()
    {
        InitializeComponent();
        ConfirmationTextBox.TextChanged += ConfirmationTextBox_TextChanged;
    }

    private void ConfirmationTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ConfirmButton.IsEnabled = ConfirmationTextBox.Text == "ELIMINAR TODOS";
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
    }
}