using IUuIS_PZ2.Models;
using IUuIS_PZ2.Utils;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace IUuIS_PZ2.ViewModels
{
    public class DisplayViewModel : ObservableObject
    {
        // Izvor (entiteti iz EntitiesVM)
        public ObservableCollection<DerEntity> AllEntities { get; }

        // DVE PRAVE LISTE za levo drvo (bez CollectionView/Filter)
        public ObservableCollection<DerEntity> SolarList { get; } = new();
        public ObservableCollection<DerEntity> WindList { get; } = new();

        // Slotovi i veze
        public ObservableCollection<DisplaySlot> Slots { get; } = new();
        public ObservableCollection<Link> Links { get; } = new();

        // Connect mod
        private bool _connectMode;
        public bool ConnectMode { get => _connectMode; set => Set(ref _connectMode, value); }

        private int? _firstConnectId;

        public DisplayViewModel(EntitiesViewModel entitiesVM)
        {
            AllEntities = entitiesVM.Entities;

            // 12 slotova (3×4)
            for (int i = 1; i <= 12; i++)
                Slots.Add(new DisplaySlot { Index = i });

            // Popuni inicijalno liste
            foreach (var e in AllEntities)
            {
                Attach(e);
                AddToProperListSorted(e);
            }

            // Pracenje promena kolekcije
            AllEntities.CollectionChanged += AllEntities_CollectionChanged;
        }

        private void AllEntities_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                SolarList.Clear();
                WindList.Clear();
                foreach (var it in AllEntities)
                {
                    Attach(it);
                    AddToProperListSorted(it);
                }
                return;
            }

            if (e.NewItems != null)
            {
                foreach (var it in e.NewItems.OfType<DerEntity>())
                {
                    Attach(it);
                    AddToProperListSorted(it);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var it in e.OldItems.OfType<DerEntity>())
                {
                    Detach(it);
                    RemoveFromLists(it);
                }
            }
        }

        private void Attach(DerEntity e) => e.PropertyChanged += Entity_PropertyChanged;
        private void Detach(DerEntity e) => e.PropertyChanged -= Entity_PropertyChanged;

        private void Entity_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not DerEntity ent) return;

            if (e.PropertyName == nameof(DerEntity.Type))
            {
                // prebacivanje između lista
                RemoveFromLists(ent);
                AddToProperListSorted(ent);
            }
            else if (e.PropertyName == nameof(DerEntity.Name))
            {
                // re-sort unutar iste liste
                if (ent.Type == DerType.SolarniPanel) ResortInList(SolarList, ent);
                else ResortInList(WindList, ent);
            }
        }

        private void AddToProperListSorted(DerEntity e)
        {
            var list = e.Type == DerType.SolarniPanel ? SolarList : WindList;
            InsertSorted(list, e);
        }

        private void RemoveFromLists(DerEntity e)
        {
            var i = SolarList.IndexOf(e);
            if (i >= 0) SolarList.RemoveAt(i);
            i = WindList.IndexOf(e);
            if (i >= 0) WindList.RemoveAt(i);
        }

        private void InsertSorted(ObservableCollection<DerEntity> list, DerEntity e)
        {
            // case-insensitive sort po Name
            int idx = 0;
            while (idx < list.Count &&
                   string.Compare(list[idx].Name, e.Name, StringComparison.OrdinalIgnoreCase) < 0)
                idx++;
            list.Insert(idx, e);
        }

        private void ResortInList(ObservableCollection<DerEntity> list, DerEntity e)
        {
            var oldIndex = list.IndexOf(e);
            if (oldIndex < 0) return;
            list.RemoveAt(oldIndex);
            InsertSorted(list, e);
        }

        public void ToggleConnect() => ConnectMode = !ConnectMode;

        public void AddToSlot(int slotIndex, DerEntity entity)
        {
            var slot = Slots.First(s => s.Index == slotIndex);
            if (slot.Occupant != null) return;

            // ukloni iz starog slota (ako postoji)
            var old = Slots.FirstOrDefault(s => s.Occupant?.Id == entity.Id);
            if (old != null) old.Occupant = null;

            slot.Occupant = entity;
            OnPropertyChanged(nameof(Slots));
        }

        public void RemoveFromSlot(int slotIndex)
        {
            var slot = Slots.First(s => s.Index == slotIndex);
            var removed = slot.Occupant;
            slot.Occupant = null;

            if (removed != null)
            {
                var toRemove = Links.Where(l => l.SourceId == removed.Id || l.TargetId == removed.Id).ToList();
                foreach (var l in toRemove) Links.Remove(l);
            }
            OnPropertyChanged(nameof(Slots));
        }

        public void ConnectClick(int entityId)
        {
            if (!ConnectMode) return;
            if (_firstConnectId == null) { _firstConnectId = entityId; return; }
            if (_firstConnectId == entityId) { _firstConnectId = null; return; }

            if (!Links.Any(l => (l.SourceId == _firstConnectId && l.TargetId == entityId) ||
                                (l.SourceId == entityId && l.TargetId == _firstConnectId)))
            {
                Links.Add(new Link { SourceId = _firstConnectId.Value, TargetId = entityId });
            }
            _firstConnectId = null;
        }
    }
}