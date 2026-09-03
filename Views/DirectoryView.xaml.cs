using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AppParaUniversidad.Views;

public partial class DirectoryView : UserControl
{
    public DirectoryView()
    {
        InitializeComponent();

        Loaded += DirectoryView_Loaded;
        Unloaded += DirectoryView_Unloaded;
    }

    private void DirectoryView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.DirectoryViewModel viewModel)
        {
            viewModel.ConfirmDeleteAllRequested -= ConfirmDeleteAllAsync;
            viewModel.ConfirmDeleteAllRequested += ConfirmDeleteAllAsync;
        }
    }

    private void DirectoryView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.DirectoryViewModel viewModel)
        {
            viewModel.ConfirmDeleteAllRequested -= ConfirmDeleteAllAsync;
        }
    }

    private Task<bool> ConfirmDeleteAllAsync()
    {
        var dialog = new ConfirmationDialog
        {
            Owner = Window.GetWindow(this)
        };

        var result = dialog.ShowDialog();

        return Task.FromResult(result == true);
    }
}
