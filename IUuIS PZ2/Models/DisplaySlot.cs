using IUuIS_PZ2.Utils;

namespace IUuIS_PZ2.Models
{
    public class DisplaySlot : ObservableObject
    {
        public int Index { get; set; }   // 1..12

        private DerEntity? _occupant;
        public DerEntity? Occupant
        {
            get => _occupant;
            set => Set(ref _occupant, value);
        }
    }
}