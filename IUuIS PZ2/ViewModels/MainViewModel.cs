using IUuIS_PZ2.Services;
using IUuIS_PZ2.Utils;

namespace IUuIS_PZ2.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public UndoManager Undo { get; } = new();
        public EntitiesViewModel EntitiesVM { get; }
        public GraphViewModel GraphVM { get; }
        public PlaceholderDisplayViewModel DisplayVM { get; } = new();

        private object _currentViewModel;
        public object CurrentViewModel { get => _currentViewModel; set => Set(ref _currentViewModel, value); }

        private AppView _currentView = AppView.Entities;
        public AppView CurrentView { get => _currentView; set => Set(ref _currentView, value); }

        public RelayCommand NavigateEntitiesCommand { get; }
        public RelayCommand NavigateDisplayCommand { get; }
        public RelayCommand NavigateGraphCommand { get; }

        private string _status = "1 Entities • 2 Display • 3 Graph • Ctrl+Z Undo • Enter Apply • Esc Reset";
        public string StatusText { get => _status; set => Set(ref _status, value); }

        public MainViewModel()
        {
            var log = new LogService();
            EntitiesVM = new EntitiesViewModel(Undo, log);
            GraphVM = new GraphViewModel(log, EntitiesVM);

            _currentViewModel = EntitiesVM;
            _currentView = AppView.Entities;

            NavigateEntitiesCommand = new RelayCommand(_ => { CurrentViewModel = EntitiesVM; CurrentView = AppView.Entities; });
            NavigateDisplayCommand = new RelayCommand(_ => { CurrentViewModel = DisplayVM; CurrentView = AppView.Display; });
            NavigateGraphCommand = new RelayCommand(_ => { CurrentViewModel = GraphVM; CurrentView = AppView.Graph; });
        }
    }
}