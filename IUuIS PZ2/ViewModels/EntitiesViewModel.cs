using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using IUuIS_PZ2.Interface;
using IUuIS_PZ2.Models;
using IUuIS_PZ2.Services;
using IUuIS_PZ2.Utils;

namespace IUuIS_PZ2.ViewModels
{
    public class EntitiesViewModel : ObservableObject
    {
        public ObservableCollection<DerEntity> Entities { get; } = new();
        public ICollectionView EntitiesView { get; }

        private readonly ILogService _log;
        private readonly MeasurementSimulator _sim;
        private readonly UndoManager _undo;

        // P1
        private bool _searchByName = true;
        public bool SearchByName { get => _searchByName; set { if (Set(ref _searchByName, value)) EntitiesView.Refresh(); } }
        private bool _searchByType;
        public bool SearchByType { get => _searchByType; set { if (Set(ref _searchByType, value)) EntitiesView.Refresh(); } }
        private string _searchText = "";
        public string SearchText { get => _searchText; set { if (Set(ref _searchText, value)) EntitiesView.Refresh(); } }

        // Toolbar komande
        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RestartSimCommand { get; }
        public RelayCommand ApplySearchCommand { get; }
        public RelayCommand ResetSearchCommand { get; }
        public ICommand UndoCommand => _undo.UndoCommand;

        // Add panel (kao u wireframe-u)
        private int _newId; public int NewId { get => _newId; set => Set(ref _newId, value); }
        private string _newName = ""; public string NewName { get => _newName; set => Set(ref _newName, value); }
        private DerType _newType = DerType.SolarniPanel; public DerType NewType { get => _newType; set => Set(ref _newType, value); }
        private double _newValue; public double NewValue { get => _newValue; set => Set(ref _newValue, value); }
        public RelayCommand AddNewCommand { get; }

        public EntitiesViewModel(UndoManager undo, ILogService log)
        {
            _undo = undo;
            _log = log;

            // Početni primeri (T4)
            Entities.Add(new DerEntity { Id = 12, Name = "Solar-NS-01", Type = DerType.SolarniPanel, LastValue = 3.4, IsValid = true });
            Entities.Add(new DerEntity { Id = 27, Name = "Wind-IB-07", Type = DerType.Vetrogenerator, LastValue = 5.7, IsValid = false });

            EntitiesView = CollectionViewSource.GetDefaultView(Entities);
            EntitiesView.Filter = FilterEntities;

            AddCommand = new RelayCommand(_ => AddEntity());
            EditCommand = new RelayCommand(e => EditEntity(e as DerEntity), e => e is DerEntity);
            DeleteCommand = new RelayCommand(e => DeleteEntity(e as DerEntity), e => e is DerEntity);
            RestartSimCommand = new RelayCommand(_ => { _sim.Stop(); _sim.Start(); });

            ApplySearchCommand = new RelayCommand(_ => EntitiesView.Refresh());
            ResetSearchCommand = new RelayCommand(_ => { SearchText = ""; EntitiesView.Refresh(); });

            AddNewCommand = new RelayCommand(_ => AddFromForm(), _ => CanAddFromForm());

            _sim = new MeasurementSimulator(() => Entities.ToList());
            _sim.MeasurementArrived += OnMeasurement;
            _sim.Start();
        }

        private bool FilterEntities(object obj)
        {
            if (obj is not DerEntity e) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            var t = SearchText.Trim().ToLowerInvariant();

            if (SearchByName) return e.Name.ToLowerInvariant().Contains(t);
            if (SearchByType) return e.Type.ToString().ToLowerInvariant().Contains(t);
            return true;
        }

        private void OnMeasurement(object? sender, (int entityId, double value) m)
        {
            var ent = Entities.FirstOrDefault(x => x.Id == m.entityId);
            if (ent == null) return;
            var valid = m.value >= 1 && m.value <= 5; // T4 validacija

            App.Current.Dispatcher.Invoke(() =>
            {
                var oldVal = ent.LastValue;
                var oldValid = ent.IsValid;
                ent.LastValue = m.value;
                ent.IsValid = valid;
                _undo.Push(() => { ent.LastValue = oldVal; ent.IsValid = oldValid; });
            });

            _log.AppendMeasurement(DateTime.UtcNow, ent.Id, m.value, valid);
        }

        private void AddEntity()
        {
            var e = new DerEntity
            {
                Id = NextId(),
                Name = "New DER",
                Type = DerType.SolarniPanel,
                LastValue = 0,
                IsValid = false
            };
            Entities.Add(e);
            _undo.Push(() => Entities.Remove(e));
        }

        private void EditEntity(DerEntity? e)
        {
            if (e == null) return;
            var oldName = e.Name; var oldType = e.Type;
            e.Name = e.Name + " (edited)";
            e.Type = e.Type == DerType.SolarniPanel ? DerType.Vetrogenerator : DerType.SolarniPanel;
            _undo.Push(() => { e.Name = oldName; e.Type = oldType; });
        }

        private void DeleteEntity(DerEntity? e)
        {
            if (e == null) return;
            var index = Entities.IndexOf(e);
            Entities.Remove(e);
            _undo.Push(() => Entities.Insert(index, e));
        }

        private bool CanAddFromForm()
            => NewId > 0 && !string.IsNullOrWhiteSpace(NewName);

        private void AddFromForm()
        {
            var e = new DerEntity
            {
                Id = NewId,
                Name = NewName,
                Type = NewType,
                LastValue = NewValue,
                IsValid = NewValue >= 1 && NewValue <= 5
            };
            Entities.Add(e);
            _undo.Push(() => Entities.Remove(e));

            // reset forme
            NewId = 0; NewName = ""; NewType = DerType.SolarniPanel; NewValue = 0;
        }

        private int NextId() => Entities.Count == 0 ? 1 : Entities.Max(x => x.Id) + 1;
    }
}