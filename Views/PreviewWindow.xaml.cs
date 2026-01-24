using System.ComponentModel;
using System.Windows;

namespace AppParaUniversidad.Views
{
    public partial class PreviewWindow : Window
    {
        public PreviewWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldVm)
            {
                oldVm.PropertyChanged -= OnVmPropertyChanged;
            }
            if (e.NewValue is INotifyPropertyChanged newVm)
            {
                newVm.PropertyChanged += OnVmPropertyChanged;
                UpdatePreview(newVm);
            }
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PreviewHtml" && sender is object vm)
            {
                UpdatePreview(vm);
            }
        }

        private void UpdatePreview(object vm)
        {
            var prop = vm.GetType().GetProperty("PreviewHtml");
            var html = prop?.GetValue(vm) as string ?? "<html><body><p>Sin preview.</p></body></html>";
            PreviewBrowser.NavigateToString(html);
        }
    }
}
