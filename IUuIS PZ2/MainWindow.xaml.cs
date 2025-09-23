// IUuIS_PZ2/MainWindow.xaml.cs
using System.ComponentModel;
using System.Windows;
using IUuIS_PZ2.ViewModels;

namespace IUuIS_PZ2
{
    public partial class MainWindow : Window
    {
        private MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;

            _vm.PropertyChanged += Vm_PropertyChanged;
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.ConsoleVisible) && _vm.ConsoleVisible)
            {
                // Fokus na input čim se konzola otvori
                Dispatcher.BeginInvoke(() => ConsoleInputBox.Focus());
            }
        }
    }
}