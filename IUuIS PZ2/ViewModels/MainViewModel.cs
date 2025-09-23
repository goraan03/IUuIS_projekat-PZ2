// IUuIS_PZ2/ViewModels/MainViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using IUuIS_PZ2.Services;
using IUuIS_PZ2.Utils;

namespace IUuIS_PZ2.ViewModels
{
    public enum AppView { Entities, Display, Graph }

    public class MainViewModel : ObservableObject
    {
        public UndoManager Undo { get; } = new();
        public EntitiesViewModel EntitiesVM { get; }
        public DisplayViewModel DisplayVM { get; }
        public GraphViewModel GraphVM { get; }

        private object _currentViewModel;
        public object CurrentViewModel { get => _currentViewModel; set => Set(ref _currentViewModel, value); }

        private AppView _currentView = AppView.Entities;
        public AppView CurrentView { get => _currentView; set => Set(ref _currentView, value); }

        public RelayCommand NavigateEntitiesCommand { get; }
        public RelayCommand NavigateDisplayCommand { get; }
        public RelayCommand NavigateGraphCommand { get; }

        private string _status = "1 Entities • 2 Display • 3 Graph • Ctrl+Z Undo • Ctrl+K Console • Enter Apply • Esc Reset";
        public string StatusText { get => _status; set => Set(ref _status, value); }

        // Console (CMD stil)
        private bool _consoleVisible;
        public bool ConsoleVisible { get => _consoleVisible; set => Set(ref _consoleVisible, value); }

        private string _consoleInput = "";
        public string ConsoleInput { get => _consoleInput; set => Set(ref _consoleInput, value); }

        public ObservableCollection<string> ConsoleHistory { get; } = new();

        public RelayCommand ToggleConsoleCommand { get; }
        public RelayCommand ExecuteConsoleCommand { get; }

        public MainViewModel()
        {
            var log = new LogService();
            EntitiesVM = new EntitiesViewModel(Undo, log);
            DisplayVM = new DisplayViewModel(EntitiesVM);
            GraphVM = new GraphViewModel(log, EntitiesVM);

            _currentViewModel = EntitiesVM;
            _currentView = AppView.Entities;

            NavigateEntitiesCommand = new RelayCommand(_ => { CurrentViewModel = EntitiesVM; CurrentView = AppView.Entities; });
            NavigateDisplayCommand = new RelayCommand(_ => { CurrentViewModel = DisplayVM; CurrentView = AppView.Display; });
            NavigateGraphCommand = new RelayCommand(_ => { CurrentViewModel = GraphVM; CurrentView = AppView.Graph; });

            ToggleConsoleCommand = new RelayCommand(_ => ConsoleVisible = !ConsoleVisible);
            ExecuteConsoleCommand = new RelayCommand(_ => ExecuteConsole());
        }

        private void LogToConsole(string msg) => ConsoleHistory.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");

        private void ExecuteConsole()
        {
            var raw = ConsoleInput?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(raw)) return;

            LogToConsole($"> {raw}");
            var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();

            try
            {
                switch (cmd)
                {
                    case "help":
                        LogToConsole("nav entities|display|graph; search name <txt>|type <txt>; clear; add id= name= type= value=; edit id= [name=] [type=] [value=]; del id=; restart; undo");
                        break;

                    case "nav":
                        if (parts.Length < 2) { LogToConsole("nav entities|display|graph"); break; }
                        var where = parts[1].ToLowerInvariant();
                        if (where.StartsWith("ent")) { CurrentViewModel = EntitiesVM; CurrentView = AppView.Entities; }
                        else if (where.StartsWith("dis")) { CurrentViewModel = DisplayVM; CurrentView = AppView.Display; }
                        else if (where.StartsWith("gra")) { CurrentViewModel = GraphVM; CurrentView = AppView.Graph; }
                        else LogToConsole("Unknown view");
                        break;

                    case "search":
                        if (parts.Length < 3) { LogToConsole("search name|type <text>"); break; }
                        var mode = parts[1].ToLowerInvariant();
                        var text = string.Join(' ', parts.Skip(2));
                        EntitiesVM.SearchByName = mode == "name";
                        EntitiesVM.SearchByType = mode == "type";
                        EntitiesVM.SearchText = text;
                        EntitiesVM.ApplySearchCommand.Execute(null);
                        LogToConsole($"Filter {mode} = '{text}'");
                        break;

                    case "clear":
                        EntitiesVM.SearchText = "";
                        EntitiesVM.ApplySearchCommand.Execute(null);
                        LogToConsole("Filter cleared");
                        break;

                    case "add":
                        // add id=101 name=Solar-X type=solar value=3.2
                        var a = ParseArgs(parts.Skip(1));
                        var id = int.Parse(a["id"]);
                        var name = a["name"];
                        var type = a["type"].ToLowerInvariant().Contains("vet") ? Models.DerType.Vetrogenerator : Models.DerType.SolarniPanel;
                        var value = double.Parse(a["value"], CultureInfo.InvariantCulture);

                        var e = new Models.DerEntity { Id = id, Name = name, Type = type, LastValue = value, IsValid = value >= 1 && value <= 5 };
                        EntitiesVM.Entities.Add(e);
                        LogToConsole($"Added {id}:{name}");
                        break;

                    case "edit":
                        // edit id=101 [name=...] [type=...] [value=...]
                        var ed = ParseArgs(parts.Skip(1));
                        var eid = int.Parse(ed["id"]);
                        var ent = EntitiesVM.Entities.FirstOrDefault(x => x.Id == eid);
                        if (ent == null) { LogToConsole("Not found"); break; }
                        if (ed.TryGetValue("name", out var nn)) ent.Name = nn;
                        if (ed.TryGetValue("type", out var tt)) ent.Type = tt.ToLowerInvariant().Contains("vet") ? Models.DerType.Vetrogenerator : Models.DerType.SolarniPanel;
                        if (ed.TryGetValue("value", out var vv))
                        {
                            var v = double.Parse(vv, CultureInfo.InvariantCulture);
                            ent.LastValue = v; ent.IsValid = v >= 1 && v <= 5;
                        }
                        LogToConsole($"Edited {eid}");
                        break;

                    case "del":
                    case "delete":
                        var d = ParseArgs(parts.Skip(1));
                        var did = int.Parse(d["id"]);
                        var del = EntitiesVM.Entities.FirstOrDefault(x => x.Id == did);
                        if (del == null) { LogToConsole("Not found"); break; }
                        EntitiesVM.Entities.Remove(del);
                        LogToConsole($"Deleted {did}");
                        break;

                    case "restart":
                        EntitiesVM.RestartSimCommand.Execute(null);
                        LogToConsole("Simulator restarted");
                        break;

                    case "undo":
                        Undo.Undo();
                        LogToConsole("Undo");
                        break;

                    default:
                        LogToConsole("Unknown command. Type 'help'.");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogToConsole($"Error: {ex.Message}");
            }
            finally
            {
                ConsoleInput = "";
            }
        }

        private static System.Collections.Generic.Dictionary<string, string> ParseArgs(System.Collections.Generic.IEnumerable<string> parts)
        {
            var dict = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in parts)
            {
                var kv = p.Split('=', 2);
                if (kv.Length == 2) dict[kv[0]] = kv[1].Trim('"');
            }
            return dict;
        }
    }
}