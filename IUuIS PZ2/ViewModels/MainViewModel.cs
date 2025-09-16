using IUuIS_PZ2.Services;
using IUuIS_PZ2.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IUuIS_PZ2.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public UndoManager Undo { get; } = new();
        public EntitiesViewModel EntitiesVM { get; }
        public GraphViewModel GraphVM { get; }

        private object _currentViewModel;
        public object CurrentViewModel { get => _currentViewModel; set => Set(ref _currentViewModel, value); }

        public RelayCommand NavigateEntitiesCommand { get; }
        public RelayCommand NavigateDisplayCommand { get; } // placeholder view
        public RelayCommand NavigateGraphCommand { get; }

        private string _status = "1 Entities • 2 Display • 3 Graph • Ctrl+Z Undo • Enter Apply • Esc Reset";
        public string StatusText { get => _status; set => Set(ref _status, value); }

        public MainViewModel()
        {
            var log = new LogService();
            EntitiesVM = new EntitiesViewModel(Undo, log);
            GraphVM = new GraphViewModel(log, EntitiesVM);

            _currentViewModel = EntitiesVM;

            NavigateEntitiesCommand = new RelayCommand(_ => CurrentViewModel = EntitiesVM);
            NavigateDisplayCommand = new RelayCommand(_ => CurrentViewModel = new PlaceholderDisplayViewModel());
            NavigateGraphCommand = new RelayCommand(_ => CurrentViewModel = GraphVM);
        }
    }

    // minimalni VM za Network Display – samo da navigacija radi
    public class PlaceholderDisplayViewModel { public string Title => "Network Display (out of scope)"; }
}
}
