using System.Windows;
using System.Windows.Controls;
using AppParaUniversidad.ViewModels;

namespace AppParaUniversidad.Views
{
    public partial class SendView : UserControl
    {
        private PreviewWindow? _previewWindow;

        public SendView()
        {
            InitializeComponent();
        }

        private void OnOpenPreviewClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SendViewModel vm) return;
            vm.PreviewCommand.Execute(null);

            if (_previewWindow is null || !_previewWindow.IsLoaded)
            {
                _previewWindow = new PreviewWindow
                {
                    Owner = Window.GetWindow(this),
                    DataContext = vm
                };
                _previewWindow.Show();
            }
            else
            {
                _previewWindow.Activate();
            }
        }
    }
}
