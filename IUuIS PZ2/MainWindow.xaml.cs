// IUuIS_PZ2/MainWindow.xaml.cs
using System.Windows;
using IUuIS_PZ2.ViewModels;

namespace IUuIS_PZ2
{
    public partial class MainWindow : Window
    {
        public MainViewModel VM { get; } = new();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = VM;
        }
    }
}