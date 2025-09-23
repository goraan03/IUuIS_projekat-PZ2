// IUuIS_PZ2/ViewModels/EntitiesViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
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

        // Selection (za Edit/Delete)
        private DerEntity? _selectedEntity;
        public DerEntity? SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                if (Set(ref _selectedEntity, value))
                {
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // P1
        private bool _searchByName = true;
        public bool SearchByName { get => _searchByName; set { if (Set(ref _searchByName, value)) EntitiesView.Refresh(); } }
        private bool _searchByType;
        public bool SearchByType { get => _searchByType; set { if (Set(ref _searchByType, value)) EntitiesView.Refresh(); } }
        private string _searchText = "";
        public string SearchText { get => _searchText; set { if (Set(ref _searchText, value)) EntitiesView.Refresh(); } }

        // Toolbar
        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RestartSimCommand { get; }
        public RelayCommand ApplySearchCommand { get; }
        public RelayCommand ResetSearchCommand { get; }
        public ICommand UndoCommand => _undo.UndoCommand;

        // Add panel (validacija: ID>0 i Name!=empty)
        private int _newId; public int NewId { get => _newId; set { if (Set(ref _newId, value)) AddNewCommand.RaiseCanExecuteChanged(); } }
        private string _newName = ""; public string NewName { get => _newName; set { if (Set(ref _newName, value)) AddNewCommand.RaiseCanExecuteChanged(); } }
        private DerType _newType = DerType.SolarniPanel; public DerType NewType { get => _newType; set => Set(ref _newType, value); }
        private double _newValue; public double NewValue { get => _newValue; set => Set(ref _newValue, value); }
        public RelayCommand AddNewCommand { get; }

        // Edit panel (IsEditing: prikazuje Edit panel umesto Add)
        private bool _isEditing; public bool IsEditing { get => _isEditing; set => Set(ref _isEditing, value); }
        private DerEntity? _editing;

        private int _editId; public int EditId { get => _editId; set => Set(ref _editId, value); } // read-only u UI
        private string _editName = ""; public string EditName { get => _editName; set { if (Set(ref _editName, value)) SaveEditCommand.RaiseCanExecuteChanged(); } }
        private DerType _editType; public DerType EditType { get => _editType; set => Set(ref _editType, value); }
        private double _editValue; public double EditValue { get => _editValue; set => Set(ref _editValue, value); }

        public RelayCommand SaveEditCommand { get; }
        public RelayCommand CancelEditCommand { get; }

        // (opciono) za auto-refresh grafa
        public event Action<int>? MeasurementArrived;

        public EntitiesViewModel(UndoManager undo, ILogService log)
        {
            _undo = undo;
            _log = log;

            // inicialni T4 primeri
            Entities.Add(new DerEntity { Id = 12, Name = "Solar-NS-01", Type = DerType.SolarniPanel, LastValue = 3.4, IsValid = true });
            Entities.Add(new DerEntity { Id = 27, Name = "Wind-IB-07", Type = DerType.Vetrogenerator, LastValue = 5.7, IsValid = false });

            EntitiesView = CollectionViewSource.GetDefaultView(Entities);
            EntitiesView.Filter = FilterEntities;

            AddCommand = new RelayCommand(_ => BeginAdd());
            EditCommand = new RelayCommand(_ => BeginEdit(), _ => SelectedEntity != null);
            DeleteCommand = new RelayCommand(_ => DeleteEntity(), _ => SelectedEntity != null);
            RestartSimCommand = new RelayCommand(_ => { _sim.Stop(); _sim.Start(); });

            ApplySearchCommand = new RelayCommand(_ => EntitiesView.Refresh());
            ResetSearchCommand = new RelayCommand(_ => { SearchText = ""; EntitiesView.Refresh(); });

            AddNewCommand = new RelayCommand(_ => AddFromForm(), _ => CanAddFromForm());
            SaveEditCommand = new RelayCommand(_ => SaveEdit(), _ => _editing != null && !string.IsNullOrWhiteSpace(EditName));
            CancelEditCommand = new RelayCommand(_ => CancelEdit());

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
            var valid = m.value >= 1 && m.value <= 5;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var oldVal = ent.LastValue;
                var oldValid = ent.IsValid;
                ent.LastValue = m.value;
                ent.IsValid = valid;
                _undo.Push(() => { ent.LastValue = oldVal; ent.IsValid = oldValid; });
            });

            _log.AppendMeasurement(DateTime.UtcNow, ent.Id, m.value, valid);
            MeasurementArrived?.Invoke(ent.Id);
        }

        // Add
        private void BeginAdd()
        {
            IsEditing = false; // pokaži Add panel
        }

        private bool CanAddFromForm() => NewId > 0 && !string.IsNullOrWhiteSpace(NewName);

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
            _log.AppendMeasurement(DateTime.UtcNow, e.Id, e.LastValue, e.IsValid);
            _sim.Stop(); _sim.Start();

            // reset Add forme
            NewId = 0; NewName = ""; NewType = DerType.SolarniPanel; NewValue = 0;
        }

        // Edit
        private void BeginEdit()
        {
            if (SelectedEntity == null) return;
            _editing = SelectedEntity;

            EditId = _editing.Id;
            EditName = _editing.Name;
            EditType = _editing.Type;
            EditValue = _editing.LastValue;

            IsEditing = true; // sakrij Add, prikaži Edit
            SaveEditCommand.RaiseCanExecuteChanged();
        }

        private void SaveEdit()
        {
            if (_editing == null) return;

            var e = _editing;
            var oldName = e.Name;
            var oldType = e.Type;
            var oldVal = e.LastValue;
            var oldValid = e.IsValid;

            e.Name = EditName;
            e.Type = EditType;
            e.LastValue = EditValue;
            e.IsValid = EditValue >= 1 && EditValue <= 5;

            if (oldVal != EditValue || oldValid != e.IsValid)
                _log.AppendMeasurement(DateTime.UtcNow, e.Id, e.LastValue, e.IsValid);

            _undo.Push(() => { e.Name = oldName; e.Type = oldType; e.LastValue = oldVal; e.IsValid = oldValid; });

            _editing = null;
            IsEditing = false; // vrati Add panel
        }

        private void CancelEdit()
        {
            _editing = null;
            IsEditing = false; // vrati Add panel
        }

        // Delete
        private void DeleteEntity()
        {
            if (SelectedEntity == null) return;
            if (MessageBox.Show($"Delete {SelectedEntity.Name}?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            var e = SelectedEntity;
            var index = Entities.IndexOf(e);
            Entities.Remove(e);
            _undo.Push(() => Entities.Insert(index, e));
            _sim.Stop(); _sim.Start();
        }

        private int NextId() => Entities.Count == 0 ? 1 : Entities.Max(x => x.Id) + 1;
    }
}